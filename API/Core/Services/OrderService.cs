using AutoMapper;
using Core.DTOs.Order;
using Core.Services.IServices;
using CORE.Constants;
using CORE.DTOs;
using Data.DataAccess.Repositories.UnitOfWork;
using Data.Models;
using Data.Models.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Helpers;

namespace Core.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<OrderService> _logger;
        private readonly IInvoiceService _invoiceService;
        private readonly IAdminNotificationService _adminNotificationService;

        public OrderService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<OrderService> logger, IInvoiceService invoiceService, IAdminNotificationService adminNotificationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _invoiceService = invoiceService;
            _adminNotificationService = adminNotificationService;
        }

        public async Task<ResponseDto<GetOrderDto>> CreateOrderAsync(CreateOrderDto dto, int? sellerId)
        {
            if (sellerId == null || sellerId <= 0)
                return new ResponseDto<GetOrderDto> { StatusCode = StatusCodes.Unauthorized, Message = "Invalid seller ID" };

            // Pre-validate items outside of transaction
            var pendingItems = new List<(int ProductId, int Quantity)>();
            decimal totalAmountPreCalc = 0;
            foreach (var itemDto in dto.Items)
            {
                var product = await _unitOfWork.Products.GetAsync(itemDto.ProductId);
                if (product == null || product.IsDeleted)
                {
                    return new ResponseDto<GetOrderDto> { StatusCode = StatusCodes.NotFound, Message = $"Product {itemDto.ProductId} not found" };
                }
                if (product.Quantity < itemDto.Quantity)
                {
                    return new ResponseDto<GetOrderDto>
                    {
                        StatusCode = StatusCodes.BadRequest,
                        Message = $"Insufficient stock for product '{product.Name}'. Available: {product.Quantity}, Requested: {itemDto.Quantity}"
                    };
                }
                totalAmountPreCalc += itemDto.Quantity * product.PricePerSqm;
                pendingItems.Add((itemDto.ProductId, itemDto.Quantity));
            }

            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var order = new Order
                    {
                        SellerId = sellerId.Value,
                        CustomerName = dto.CustomerName,
                        CustomerContact = dto.CustomerContact,
                        CustomerAddress = dto.CustomerAddress,
                        Notes = dto.Notes,
                        Status = OrderStatus.Pending,
                        TotalAmount = totalAmountPreCalc
                    };

                    await _unitOfWork.Orders.AddOrUpdateAsync(order);
                    await _unitOfWork.CommitAsync(); // to get order.Id

                    foreach (var (productId, quantity) in pendingItems)
                    {
                        var product = await _unitOfWork.Products.GetAsync(productId);
                        if (product == null || product.IsDeleted)
                            throw new InvalidOperationException($"Product {productId} not found");

                        if (product.Quantity < quantity)
                            throw new InvalidOperationException($"Insufficient stock for product '{product.Name}'. Available: {product.Quantity}, Requested: {quantity}");

                        product.Quantity -= quantity;
                        await _unitOfWork.Products.AddOrUpdateAsync(product);

                        var orderItem = new OrderItem
                        {
                            OrderId = order.Id,
                            ProductId = productId,
                            Quantity = quantity,
                            PricePerSqm = product.PricePerSqm,
                            TotalPrice = quantity * product.PricePerSqm
                        };
                        await _unitOfWork.OrderItems.AddOrUpdateAsync(orderItem);
                    }

                    await _unitOfWork.CommitAsync();

                    // Load full order details for notification
                    var response = await GetOrderByIdAsync(order.Id, sellerId, new[] { "Seller" });

                    if (response.StatusCode == StatusCodes.OK && response.Data != null)
                    {
                        try
                        {
                            await _adminNotificationService.NotifyOrderPlacedAsync(response.Data);
                        }
                        catch (Exception notifyEx)
                        {
                            _logger.LogWarning(notifyEx, "Failed to notify admins for order {OrderId}", order.Id);
                        }
                    }

                    return response;
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "CreateOrder validation failed during transaction");
                return new ResponseDto<GetOrderDto> { StatusCode = StatusCodes.BadRequest, Message = ex.Message };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return new ResponseDto<GetOrderDto> { StatusCode = StatusCodes.InternalServerError, Message = "Error creating order" };
            }
        }

        public async Task<ResponseDto<GetOrderDto>> UpdateOrderAsync(int orderId, UpdateOrderDto dto, int? sellerId)
        {
            if (sellerId == null || sellerId <= 0)
                return new ResponseDto<GetOrderDto> { StatusCode = StatusCodes.Unauthorized, Message = "Invalid seller ID" };

            var order = await _unitOfWork.Orders.FindAsync(o => o.Id == orderId, new[] { "OrderItems" });
            if (order == null)
                return new ResponseDto<GetOrderDto> { StatusCode = StatusCodes.NotFound, Message = "Order not found" };

            if (order.SellerId != sellerId.Value)
                return new ResponseDto<GetOrderDto> { StatusCode = StatusCodes.Forbidden, Message = "Unauthorized access" };

            if (order.Status != OrderStatus.Pending)
                return new ResponseDto<GetOrderDto> { StatusCode = StatusCodes.BadRequest, Message = "Only pending orders can be updated" };

            // Update order metadata
            if (!string.IsNullOrWhiteSpace(dto.CustomerName)) order.CustomerName = dto.CustomerName;
            if (!string.IsNullOrWhiteSpace(dto.CustomerContact)) order.CustomerContact = dto.CustomerContact;
            if (!string.IsNullOrWhiteSpace(dto.CustomerAddress)) order.CustomerAddress = dto.CustomerAddress;
            if (dto.Notes != null) order.Notes = dto.Notes;

            // Handle item quantity changes with stock adjustments
            if (dto.Items != null && dto.Items.Any())
            {
                decimal totalAmount = 0;

                // Create a dictionary of old items for easy lookup
                Dictionary<int, OrderItem> oldItems;
                try
                {
                    oldItems = order.OrderItems.ToDictionary(i => i.ProductId, i => i);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Duplicate product IDs found in existing order items for order {OrderId}", orderId);
                    return new ResponseDto<GetOrderDto> { StatusCode = StatusCodes.InternalServerError, Message = "Data inconsistency detected in order items" };
                }

                // Track new items to add
                var newOrderItems = new List<OrderItem>();

                foreach (var itemDto in dto.Items)
                {
                    var product = await _unitOfWork.Products.GetAsync(itemDto.ProductId);
                    if (product == null || product.IsDeleted)
                    {
                        return new ResponseDto<GetOrderDto> { 
                            StatusCode = StatusCodes.NotFound, 
                            Message = $"Product {itemDto.ProductId} not found" 
                        };
                    }

                    if (oldItems.TryGetValue(itemDto.ProductId, out var oldItem))
                    {
                        // Item exists, check if quantity changed
                        int quantityDifference = itemDto.Quantity - oldItem.Quantity;

                        if (quantityDifference != 0)
                        {
                            // Validate sufficient stock for increase
                            if (quantityDifference > 0 && product.Quantity < quantityDifference)
                            {
                                return new ResponseDto<GetOrderDto> { 
                                    StatusCode = StatusCodes.BadRequest, 
                                    Message = $"Insufficient stock for product '{product.Name}'. Available: {product.Quantity}, Additional needed: {quantityDifference}" 
                                };
                            }

                            // Adjust stock: negative difference = restore stock, positive = deduct more
                            product.Quantity -= quantityDifference;
                            await _unitOfWork.Products.AddOrUpdateAsync(product);

                            // Update order item
                            oldItem.Quantity = itemDto.Quantity;
                            oldItem.TotalPrice = itemDto.Quantity * product.PricePerSqm;
                            await _unitOfWork.OrderItems.AddOrUpdateAsync(oldItem);
                        }

                        totalAmount += oldItem.TotalPrice;
                        oldItems.Remove(itemDto.ProductId); // Mark as processed
                    }
                    else
                    {
                        // New item added to order
                        if (product.Quantity < itemDto.Quantity)
                        {
                            return new ResponseDto<GetOrderDto> { 
                                StatusCode = StatusCodes.BadRequest, 
                                Message = $"Insufficient stock for product '{product.Name}'. Available: {product.Quantity}, Requested: {itemDto.Quantity}" 
                            };
                        }

                        // Deduct stock
                        product.Quantity -= itemDto.Quantity;
                        await _unitOfWork.Products.AddOrUpdateAsync(product);

                        // Create new order item
                        var newItem = new OrderItem
                        {
                            OrderId = orderId,
                            ProductId = itemDto.ProductId,
                            Quantity = itemDto.Quantity,
                            PricePerSqm = product.PricePerSqm,
                            TotalPrice = itemDto.Quantity * product.PricePerSqm
                        };
                        newOrderItems.Add(newItem);
                        totalAmount += newItem.TotalPrice;
                    }
                }

                // Handle removed items (restore stock)
                foreach (var removedItem in oldItems.Values)
                {
                    var product = await _unitOfWork.Products.GetAsync(removedItem.ProductId);
                    if (product != null && !product.IsDeleted)
                    {
                        product.Quantity += removedItem.Quantity;
                        await _unitOfWork.Products.AddOrUpdateAsync(product);
                    }
                    _unitOfWork.OrderItems.Delete(removedItem);
                }

                // Add new items to database
                foreach (var newItem in newOrderItems)
                {
                    await _unitOfWork.OrderItems.AddOrUpdateAsync(newItem);
                }

                // Update order total amount
                order.TotalAmount = totalAmount;
            }

            order.UpdatedDate = DateTime.UtcNow;
            await _unitOfWork.Orders.AddOrUpdateAsync(order);
            await _unitOfWork.CommitAsync();

            return await GetOrderByIdAsync(orderId, sellerId, new[] { "Seller" });
        }

        public async Task<ResponseDto<object>> CancelOrderAsync(int orderId, int? sellerId)
        {
            if (sellerId == null || sellerId <= 0)
                return new ResponseDto<object> { StatusCode = StatusCodes.Unauthorized, Message = "Invalid seller ID" };

            // Load order with items to restore stock
            var order = await _unitOfWork.Orders.FindAsync(o => o.Id == orderId, new[] { "OrderItems" });
            if (order == null)
                return new ResponseDto<object> { StatusCode = StatusCodes.NotFound, Message = "Order not found" };

            if (order.SellerId != sellerId.Value)
                return new ResponseDto<object> { StatusCode = StatusCodes.Forbidden, Message = "Unauthorized access" };

            if (order.Status == OrderStatus.Confirmed || order.Status == OrderStatus.Completed)
                return new ResponseDto<object> { StatusCode = StatusCodes.BadRequest, Message = "Cannot cancel confirmed or completed orders" };

            // Restore stock for each order item
            foreach (var orderItem in order.OrderItems)
            {
                var product = await _unitOfWork.Products.GetAsync(orderItem.ProductId);
                if (product != null && !product.IsDeleted)
                {
                    product.Quantity += orderItem.Quantity;
                    await _unitOfWork.Products.AddOrUpdateAsync(product);
                }
            }

            // Mark order as cancelled instead of deleting
            order.Status = OrderStatus.Cancelled;
            await _unitOfWork.Orders.AddOrUpdateAsync(order);
            await _unitOfWork.CommitAsync();

            return new ResponseDto<object> { StatusCode = StatusCodes.OK, Message = "Order cancelled successfully and stock restored" };
        }

        public async Task<ResponseDto<GetOrderDto>> GetOrderByIdAsync(int orderId, int? userId, string[]? roles)
        {
            var order = await _unitOfWork.Orders.FindAsync(o => o.Id == orderId, new[] { "Seller", "OrderItems", "OrderItems.Product", "Invoice" });
            if (order == null)
                return new ResponseDto<GetOrderDto> { StatusCode = StatusCodes.NotFound, Message = "Order not found" };

            var isAdmin = roles?.Contains("Admin") == true;
            if (!isAdmin && order.SellerId != userId)
                return new ResponseDto<GetOrderDto> { StatusCode = StatusCodes.Forbidden, Message = "Unauthorized access" };

            var orderDto = MapOrderToDto(order);
            return new ResponseDto<GetOrderDto> { StatusCode = StatusCodes.OK, Data = orderDto };
        }

        public async Task<ResponseDto<PagedResultDto<GetOrderDto>>> GetOrdersAsync(OrderFilterDto filter, int? userId, string[]? roles)
        {
            var isAdmin = roles?.Contains("Admin") == true;

            System.Linq.Expressions.Expression<Func<Order, bool>> criteria = o => true;
            
            if (!isAdmin)
                criteria = o => o.SellerId == userId.Value;
            else if (filter.SellerId.HasValue)
                criteria = o => o.SellerId == filter.SellerId.Value;

            if (filter.Status.HasValue)
            {
                var status = filter.Status.Value;
                criteria = criteria.AndAlso(o => o.Status == status);
            }

            if (filter.FromDate.HasValue)
                criteria = criteria.AndAlso(o => o.OrderDate >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                criteria = criteria.AndAlso(o => o.OrderDate <= filter.ToDate.Value);

            var totalCount = await _unitOfWork.Orders.CountAsync(criteria);
            var orders = await _unitOfWork.Orders.FindAsync(
                criteria,
                filter.PageNumber,
                filter.PageSize,
                new[] { "Seller", "OrderItems", "OrderItems.Product", "Invoice" },
                o => o.OrderDate,
                DATA.Constants.Enums.OrderBy.Descending
            );

            var orderDtos = orders.Select(MapOrderToDto).ToList();

            return new ResponseDto<PagedResultDto<GetOrderDto>>
            {
                StatusCode = StatusCodes.OK,
                Data = new PagedResultDto<GetOrderDto>
                {
                    Items = orderDtos,
                    TotalCount = totalCount,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize
                }
            };
        }

        public async Task<ResponseDto<GetOrderDto>> ConfirmOrderAsync(int orderId, int? adminId)
        {
            if (adminId == null || adminId <= 0)
                return new ResponseDto<GetOrderDto> { StatusCode = StatusCodes.Unauthorized, Message = "Invalid admin ID" };

            var order = await _unitOfWork.Orders.FindAsync(o => o.Id == orderId, new[] { "OrderItems" });
            if (order == null)
                return new ResponseDto<GetOrderDto> { StatusCode = StatusCodes.NotFound, Message = "Order not found" };

            if (order.Status != OrderStatus.Pending)
                return new ResponseDto<GetOrderDto> { StatusCode = StatusCodes.BadRequest, Message = "Only pending orders can be confirmed" };

            order.Status = OrderStatus.Confirmed;
            order.UpdatedDate = DateTime.UtcNow;
            await _unitOfWork.Orders.AddOrUpdateAsync(order);
            var changes = await _unitOfWork.CommitAsync();
            if(changes == 0)
                return new ResponseDto<GetOrderDto> { StatusCode = StatusCodes.InternalServerError, Message = "Failed to confirm order" };
            // Generate invoice automatically
            await _invoiceService.GenerateInvoiceAsync(orderId);

            return await GetOrderByIdAsync(orderId, adminId, new[] { "Admin" });
        }

        public async Task<ResponseDto<GetOrderDto>> RejectOrderAsync(int orderId, int? adminId, string? reason)
        {
            if (adminId == null || adminId <= 0)
                return new ResponseDto<GetOrderDto> { StatusCode = StatusCodes.Unauthorized, Message = "Invalid admin ID" };

            // Load order with items to restore stock on rejection
            var order = await _unitOfWork.Orders.FindAsync(o => o.Id == orderId, new[] { "OrderItems" });
            if (order == null)
                return new ResponseDto<GetOrderDto> { StatusCode = StatusCodes.NotFound, Message = "Order not found" };

            if (order.Status != OrderStatus.Pending)
                return new ResponseDto<GetOrderDto> { StatusCode = StatusCodes.BadRequest, Message = "Only pending orders can be rejected" };

            // Restore stock for each order item
            foreach (var orderItem in order.OrderItems)
            {
                var product = await _unitOfWork.Products.GetAsync(orderItem.ProductId);
                if (product != null && !product.IsDeleted)
                {
                    product.Quantity += orderItem.Quantity;
                    await _unitOfWork.Products.AddOrUpdateAsync(product);
                }
            }

            order.Status = OrderStatus.Rejected;
            order.UpdatedDate = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(reason))
                order.Notes = (order.Notes ?? "") + $"\nRejection reason: {reason}";

            await _unitOfWork.Orders.AddOrUpdateAsync(order);
            await _unitOfWork.CommitAsync();

            return await GetOrderByIdAsync(orderId, adminId, new[] { "Admin" });
        }

        public async Task<ResponseDto<GetOrderDto>> CompleteOrderAsync(int orderId, int? sellerId)
        {
            if (sellerId == null || sellerId <= 0)
                return new ResponseDto<GetOrderDto> { StatusCode = StatusCodes.Unauthorized, Message = "Invalid seller ID" };

            var order = await _unitOfWork.Orders.FindAsync(o => o.Id == orderId, new[] { "OrderItems" });
            if (order == null)
                return new ResponseDto<GetOrderDto> { StatusCode = StatusCodes.NotFound, Message = "Order not found" };

            if (order.SellerId != sellerId.Value)
                return new ResponseDto<GetOrderDto> { StatusCode = StatusCodes.Forbidden, Message = "Unauthorized access" };

            if (order.Status != OrderStatus.Confirmed)
                return new ResponseDto<GetOrderDto> { StatusCode = StatusCodes.BadRequest, Message = "Only confirmed orders can be completed" };

            order.Status = OrderStatus.Completed;
            order.UpdatedDate = DateTime.UtcNow;
            await _unitOfWork.Orders.AddOrUpdateAsync(order);
            var changes = await _unitOfWork.CommitAsync();
            if (changes == 0)
                return new ResponseDto<GetOrderDto> { StatusCode = StatusCodes.InternalServerError, Message = "Failed to complete order" };

            return await GetOrderByIdAsync(orderId, sellerId, new[] { "Seller" });
        }

        private GetOrderDto MapOrderToDto(Order order)
        {
            return new GetOrderDto
            {
                Id = order.Id,
                SellerId = order.SellerId,
                SellerName = order.Seller?.UserName ?? "N/A",
                CustomerName = order.CustomerName,
                CustomerContact = order.CustomerContact,
                CustomerAddress = order.CustomerAddress,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                Notes = order.Notes,
                OrderDate = order.OrderDate,
                UpdatedDate = order.UpdatedDate,
                Items = order.OrderItems.Select(oi => new GetOrderItemDto
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name ?? "N/A",
                    Quantity = oi.Quantity,
                    PricePerSqm = oi.PricePerSqm,
                    TotalPrice = oi.TotalPrice
                }).ToList(),
                Invoice = order.Invoice != null ? new GetInvoiceInfoDto
                {
                    Id = order.Invoice.Id,
                    TotalAmount = order.Invoice.TotalAmount,
                    InvoiceDate = order.Invoice.InvoiceDate,
                    PdfUrl = order.Invoice.PdfUrl
                } : null
            };
        }
    }
}
