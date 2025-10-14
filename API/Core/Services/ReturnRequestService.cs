using AutoMapper;
using Core.DTOs.ReturnRequest;
using Core.Helpers;
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

namespace Core.Services
{
    public class ReturnRequestService : IReturnRequestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ReturnRequestService> _logger;
        private readonly IPdfGeneratorService _pdfGenerator;
        private readonly IFileStorageService _fileStorage;

        public ReturnRequestService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<ReturnRequestService> logger,
            IPdfGeneratorService pdfGenerator,
            IFileStorageService fileStorage)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _pdfGenerator = pdfGenerator;
            _fileStorage = fileStorage;
        }

        public async Task<ResponseDto<GetReturnRequestDto>> CreateReturnRequestAsync(CreateReturnRequestDto dto, int? sellerId)
        {
            if (sellerId == null || sellerId <= 0)
                return new ResponseDto<GetReturnRequestDto> { StatusCode = StatusCodes.Unauthorized, Message = "Invalid seller ID" };

            try
            {
                var order = await _unitOfWork.Orders.FindAsync(o => o.Id == dto.OrderId, new[] { "OrderItems" });
                if (order == null)
                    return new ResponseDto<GetReturnRequestDto> { StatusCode = StatusCodes.NotFound, Message = "Order not found" };

                if (order.SellerId != sellerId.Value)
                    return new ResponseDto<GetReturnRequestDto> { StatusCode = StatusCodes.Forbidden, Message = "Unauthorized access" };

                if (order.Status != OrderStatus.Confirmed && order.Status != OrderStatus.Completed)
                    return new ResponseDto<GetReturnRequestDto> { StatusCode = StatusCodes.BadRequest, Message = "Can only return confirmed or completed orders" };

                // Check if return request already exists
                var existingRequest = await _unitOfWork.ReturnRequests.FindAsync(r => r.OrderId == dto.OrderId, null);
                if (existingRequest != null)
                    return new ResponseDto<GetReturnRequestDto> { StatusCode = StatusCodes.BadRequest, Message = "Return request already exists for this order" };

                // Pre-validate return items before starting transaction
                foreach (var itemDto in dto.Items)
                {
                    var orderItem = order.OrderItems.FirstOrDefault(oi => oi.ProductId == itemDto.ProductId);
                    if (orderItem == null || orderItem.Quantity < itemDto.Quantity)
                    {
                        return new ResponseDto<GetReturnRequestDto> { StatusCode = StatusCodes.BadRequest, Message = $"Invalid return quantity for product {itemDto.ProductId}" };
                    }
                }

                GetReturnRequestDto? createdDto = null;

                await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var returnRequest = new ReturnRequest
                    {
                        OrderId = dto.OrderId,
                        ReturnReason = dto.ReturnReason,
                        Description = dto.Description,
                        Status = ReturnStatus.Pending
                    };

                    await _unitOfWork.ReturnRequests.AddOrUpdateAsync(returnRequest);
                    // Persist to get generated Id if required by repository implementation
                    await _unitOfWork.CommitAsync();

                    foreach (var itemDto in dto.Items)
                    {
                        var returnItem = new ReturnItem
                        {
                            ReturnRequestId = returnRequest.Id,
                            ProductId = itemDto.ProductId,
                            Quantity = itemDto.Quantity,
                            Reason = itemDto.Reason ?? dto.ReturnReason
                        };

                        await _unitOfWork.ReturnItems.AddOrUpdateAsync(returnItem);
                    }

                    await _unitOfWork.CommitAsync();

                    // Map result within the same context to avoid re-query if desired
                    createdDto = (await GetReturnRequestByIdAsync(returnRequest.Id, sellerId, new[] { "Seller" })).Data;
                });

                if (createdDto == null)
                {
                    return new ResponseDto<GetReturnRequestDto> { StatusCode = StatusCodes.InternalServerError, Message = "Error creating return request" };
                }

                return new ResponseDto<GetReturnRequestDto> { StatusCode = StatusCodes.OK, Data = createdDto };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating return request");
                return new ResponseDto<GetReturnRequestDto> { StatusCode = StatusCodes.InternalServerError, Message = "Error creating return request" };
            }
        }

        public async Task<ResponseDto<GetReturnRequestDto>> GetReturnRequestByIdAsync(int id, int? userId, string[]? roles)
        {
            var returnRequest = await _unitOfWork.ReturnRequests.FindAsync(
                r => r.Id == id,
                new[] { "Order", "ReturnItems", "ReturnItems.Product" }
            );

            if (returnRequest == null)
                return new ResponseDto<GetReturnRequestDto> { StatusCode = StatusCodes.NotFound, Message = "Return request not found" };

            var isAdmin = roles?.Contains("Admin") == true;
            if (!isAdmin && returnRequest.Order.SellerId != userId)
                return new ResponseDto<GetReturnRequestDto> { StatusCode = StatusCodes.Forbidden, Message = "Unauthorized access" };

            var returnRequestDto = MapReturnRequestToDto(returnRequest);
            return new ResponseDto<GetReturnRequestDto> { StatusCode = StatusCodes.OK, Data = returnRequestDto };
        }

        public async Task<ResponseDto<PagedResultDto<GetReturnRequestDto>>> GetReturnRequestsAsync(ReturnRequestFilterDto filter, int? userId, string[]? roles)
        {
            var isAdmin = roles?.Contains("Admin") == true;

            System.Linq.Expressions.Expression<Func<ReturnRequest, bool>> criteria = r => true;

            if (!isAdmin)
                criteria = r => r.Order.SellerId == userId.Value;
            else if (filter.SellerId.HasValue)
                criteria = r => r.Order.SellerId == filter.SellerId.Value;

            if (filter.Status.HasValue)
            {
                var status = filter.Status.Value;
                criteria = criteria.AndAlso(r => r.Status == status);
            }

            if (filter.FromDate.HasValue)
                criteria = criteria.AndAlso(r => r.RequestDate >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                criteria = criteria.AndAlso(r => r.RequestDate <= filter.ToDate.Value);

            var totalCount = await _unitOfWork.ReturnRequests.CountAsync(criteria);
            var returnRequests = await _unitOfWork.ReturnRequests.FindAsync(
                criteria,
                filter.PageNumber,
                filter.PageSize,
                new[] { "Order", "ReturnItems", "ReturnItems.Product" },
                r => r.RequestDate,
                DATA.Constants.Enums.OrderBy.Descending
            );

            var returnRequestDtos = returnRequests.Select(MapReturnRequestToDto).ToList();

            return new ResponseDto<PagedResultDto<GetReturnRequestDto>>
            {
                StatusCode = StatusCodes.OK,
                Data = new PagedResultDto<GetReturnRequestDto>
                {
                    Items = returnRequestDtos,
                    TotalCount = totalCount,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize
                }
            };
        }

        public async Task<ResponseDto<GetReturnRequestDto>> AcceptReturnRequestAsync(int id, int? adminId, string? adminNotes)
        {
            if (adminId == null || adminId <= 0)
                return new ResponseDto<GetReturnRequestDto> { StatusCode = StatusCodes.Unauthorized, Message = "Invalid admin ID" };

            var returnRequest = await _unitOfWork.ReturnRequests.FindAsync(
                r => r.Id == id,
                new[] { "Order", "ReturnItems", "ReturnItems.Product" }
            );

            if (returnRequest == null)
                return new ResponseDto<GetReturnRequestDto> { StatusCode = StatusCodes.NotFound, Message = "Return request not found" };

            if (returnRequest.Status != ReturnStatus.Pending)
                return new ResponseDto<GetReturnRequestDto> { StatusCode = StatusCodes.BadRequest, Message = "Only pending requests can be accepted" };

            // Mark accepted and restock returned quantities
            returnRequest.Status = ReturnStatus.Accepted;
            returnRequest.AdminNotes = adminNotes;

            await _unitOfWork.ReturnRequests.AddOrUpdateAsync(returnRequest);

            // Restock inventory for each returned item
            if (returnRequest.ReturnItems != null)
            {
                foreach (var item in returnRequest.ReturnItems)
                {
                    try
                    {
                        var product = item.Product;
                        if (product == null)
                        {
                            product = await _unitOfWork.Products.GetAsync(item.ProductId);
                        }

                        if (product != null)
                        {
                            // Increase available inventory by the returned quantity
                            product.Quantity += item.Quantity;
                            await _unitOfWork.Products.AddOrUpdateAsync(product);
                        }
                        else
                        {
                            _logger.LogWarning("Product not found while restocking for ReturnItemId={ReturnItemId}, ProductId={ProductId}", item.Id, item.ProductId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error while restocking product for ReturnItemId={ReturnItemId}", item.Id);
                    }
                }
            }

            await _unitOfWork.CommitAsync();

            // Generate return PDF (best-effort; do not fail the acceptance if this fails)
            try
            {
                var pdfBytes = await _pdfGenerator.GenerateReturnRequestPdfAsync(
                    returnRequest,
                    returnRequest.Order,
                    returnRequest.ReturnItems.ToList()
                );
                var pdfUrl = await _fileStorage.UploadFileAsync(
                    pdfBytes,
                    $"return-{returnRequest.Id}.pdf",
                    "returns",
                    "application/pdf"
                );

                // Store PDF URL in the dedicated field
                returnRequest.ReturnPdfUrl = pdfUrl;
                await _unitOfWork.ReturnRequests.AddOrUpdateAsync(returnRequest);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating return PDF");
            }

            return await GetReturnRequestByIdAsync(id, adminId, new[] { "Admin" });
        }

        public async Task<ResponseDto<GetReturnRequestDto>> RejectReturnRequestAsync(int id, int? adminId, string? adminNotes)
        {
            if (adminId == null || adminId <= 0)
                return new ResponseDto<GetReturnRequestDto> { StatusCode = StatusCodes.Unauthorized, Message = "Invalid admin ID" };

            var returnRequest = await _unitOfWork.ReturnRequests.GetAsync(id);
            if (returnRequest == null)
                return new ResponseDto<GetReturnRequestDto> { StatusCode = StatusCodes.NotFound, Message = "Return request not found" };

            if (returnRequest.Status != ReturnStatus.Pending)
                return new ResponseDto<GetReturnRequestDto> { StatusCode = StatusCodes.BadRequest, Message = "Only pending requests can be rejected" };

            returnRequest.Status = ReturnStatus.Rejected;
            returnRequest.AdminNotes = adminNotes;

            await _unitOfWork.ReturnRequests.AddOrUpdateAsync(returnRequest);
            await _unitOfWork.CommitAsync();

            return await GetReturnRequestByIdAsync(id, adminId, new[] { "Admin" });
        }

        private GetReturnRequestDto MapReturnRequestToDto(ReturnRequest returnRequest)
        {
            return new GetReturnRequestDto
            {
                Id = returnRequest.Id,
                OrderId = returnRequest.OrderId,
                ReturnReason = returnRequest.ReturnReason,
                Description = returnRequest.Description,
                Status = returnRequest.Status,
                RequestDate = returnRequest.RequestDate,
                AdminNotes = returnRequest.AdminNotes,
                ReturnPdfUrl = returnRequest.ReturnPdfUrl,
                Items = returnRequest.ReturnItems.Select(ri => new GetReturnItemDto
                {
                    Id = ri.Id,
                    ProductId = ri.ProductId,
                    ProductName = ri.Product?.Name ?? "N/A",
                    Quantity = ri.Quantity,
                    Reason = ri.Reason
                }).ToList(),
                Order = new GetOrderInfoDto
                {
                    Id = returnRequest.Order.Id,
                    CustomerName = returnRequest.Order.CustomerName,
                    TotalAmount = returnRequest.Order.TotalAmount,
                    OrderDate = returnRequest.Order.OrderDate
                }
            };
        }
    }
}
