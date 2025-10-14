using AutoMapper;
using Core.DTOs.Cart;
using Core.Services.IServices;
using CORE.Constants;
using CORE.DTOs;
using Data.DataAccess.Repositories.UnitOfWork;
using Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Services
{
    public class CartService : ICartService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CartService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ResponseDto<GetCartDto>> GetCartAsync(int? sellerId)
        {
            if (sellerId == null || sellerId <= 0)
                return new ResponseDto<GetCartDto> { StatusCode = StatusCodes.Unauthorized, Message = "Invalid seller ID" };

            var cart = await _unitOfWork.Carts.FindAsync(c => c.SellerId == sellerId.Value, new[] { "CartItems", "CartItems.Product" });
            
            if (cart == null)
            {
                // Create cart if doesn't exist
                cart = new Cart { SellerId = sellerId.Value };
                await _unitOfWork.Carts.AddOrUpdateAsync(cart);
                await _unitOfWork.CommitAsync();
            }

            var cartDto = new GetCartDto
            {
                Id = cart.Id,
                SellerId = cart.SellerId,
                CreatedDate = cart.CreatedDate,
                Items = cart.CartItems.Select(ci => new GetCartItemDto
                {
                    Id = ci.Id,
                    ProductId = ci.ProductId,
                    ProductName = ci.Product.Name,
                    Quantity = ci.Quantity,
                    PricePerSqm = ci.Product.PricePerSqm,
                    ItemTotal = ci.Quantity * ci.Product.PricePerSqm,
                    AddedDate = ci.AddedDate
                }).ToList(),
                TotalAmount = cart.CartItems.Sum(ci => ci.Quantity * ci.Product.PricePerSqm)
            };

            return new ResponseDto<GetCartDto> { StatusCode = StatusCodes.OK, Data = cartDto };
        }

        public async Task<ResponseDto<GetCartDto>> AddItemToCartAsync(AddCartItemDto dto, int? sellerId)
        {
            if (sellerId == null || sellerId <= 0)
                return new ResponseDto<GetCartDto> { StatusCode = StatusCodes.Unauthorized, Message = "Invalid seller ID" };

            // Validate quantity
            if (dto.Quantity <= 0)
                return new ResponseDto<GetCartDto> { StatusCode = StatusCodes.BadRequest, Message = "Quantity must be greater than 0" };

            var product = await _unitOfWork.Products.GetAsync(dto.ProductId);
            if (product == null)
                return new ResponseDto<GetCartDto> { StatusCode = StatusCodes.NotFound, Message = "Product not found" };

            // Check if product is deleted
            if (product.IsDeleted)
                return new ResponseDto<GetCartDto> { StatusCode = StatusCodes.NotFound, Message = "Product not found" };

            var cart = await _unitOfWork.Carts.FindAsync(c => c.SellerId == sellerId.Value, new[] { "CartItems" });
            if (cart == null)
            {
                cart = new Cart { SellerId = sellerId.Value };
                await _unitOfWork.Carts.AddOrUpdateAsync(cart);
                await _unitOfWork.CommitAsync();
            }

            var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == dto.ProductId);
            int newQuantity = existingItem != null ? existingItem.Quantity + dto.Quantity : dto.Quantity;

            // Validate against available stock
            if (newQuantity > product.Quantity)
            {
                return new ResponseDto<GetCartDto> 
                { 
                    StatusCode = StatusCodes.BadRequest, 
                    Message = $"Insufficient stock. Available: {product.Quantity}, Requested total: {newQuantity}" 
                };
            }

            if (existingItem != null)
            {
                existingItem.Quantity = newQuantity;
                await _unitOfWork.CartItems.AddOrUpdateAsync(existingItem);
            }
            else
            {
                var cartItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity
                };
                await _unitOfWork.CartItems.AddOrUpdateAsync(cartItem);
            }

            await _unitOfWork.CommitAsync();
            return await GetCartAsync(sellerId);
        }

        public async Task<ResponseDto<GetCartDto>> UpdateCartItemAsync(int cartItemId, UpdateCartItemDto dto, int? sellerId)
        {
            if (sellerId == null || sellerId <= 0)
                return new ResponseDto<GetCartDto> { StatusCode = StatusCodes.Unauthorized, Message = "Invalid seller ID" };

            // Validate quantity
            if (dto.Quantity <= 0)
                return new ResponseDto<GetCartDto> { StatusCode = StatusCodes.BadRequest, Message = "Quantity must be greater than 0" };

            var cartItem = await _unitOfWork.CartItems.FindAsync(ci => ci.Id == cartItemId, new[] { "Cart", "Product" });
            if (cartItem == null)
                return new ResponseDto<GetCartDto> { StatusCode = StatusCodes.NotFound, Message = "Cart item not found" };

            if (cartItem.Cart.SellerId != sellerId.Value)
                return new ResponseDto<GetCartDto> { StatusCode = StatusCodes.Forbidden, Message = "Unauthorized access" };

            // Validate against available stock
            var product = await _unitOfWork.Products.GetAsync(cartItem.ProductId);
            if (product == null || product.IsDeleted)
                return new ResponseDto<GetCartDto> { StatusCode = StatusCodes.NotFound, Message = "Product not found" };

            if (dto.Quantity > product.Quantity)
            {
                return new ResponseDto<GetCartDto> 
                { 
                    StatusCode = StatusCodes.BadRequest, 
                    Message = $"Insufficient stock. Available: {product.Quantity}, Requested: {dto.Quantity}" 
                };
            }

            cartItem.Quantity = dto.Quantity;
            await _unitOfWork.CartItems.AddOrUpdateAsync(cartItem);
            await _unitOfWork.CommitAsync();

            return await GetCartAsync(sellerId);
        }

        public async Task<ResponseDto<object>> RemoveCartItemAsync(int cartItemId, int? sellerId)
        {
            if (sellerId == null || sellerId <= 0)
                return new ResponseDto<object> { StatusCode = StatusCodes.Unauthorized, Message = "Invalid seller ID" };

            var cartItem = await _unitOfWork.CartItems.FindAsync(ci => ci.Id == cartItemId, new[] { "Cart" });
            if (cartItem == null)
                return new ResponseDto<object> { StatusCode = StatusCodes.NotFound, Message = "Cart item not found" };

            if (cartItem.Cart.SellerId != sellerId.Value)
                return new ResponseDto<object> { StatusCode = StatusCodes.Forbidden, Message = "Unauthorized access" };

            _unitOfWork.CartItems.Delete(cartItem);
            await _unitOfWork.CommitAsync();

            return new ResponseDto<object> { StatusCode = StatusCodes.OK, Message = "Item removed successfully" };
        }

        public async Task<ResponseDto<object>> ClearCartAsync(int? sellerId)
        {
            if (sellerId == null || sellerId <= 0)
                return new ResponseDto<object> { StatusCode = StatusCodes.Unauthorized, Message = "Invalid seller ID" };

            var cart = await _unitOfWork.Carts.FindAsync(c => c.SellerId == sellerId.Value, new[] { "CartItems" });
            if (cart == null)
                return new ResponseDto<object> { StatusCode = StatusCodes.NotFound, Message = "Cart not found" };

            foreach (var item in cart.CartItems)
            {
                _unitOfWork.CartItems.Delete(item);
            }

            await _unitOfWork.CommitAsync();
            return new ResponseDto<object> { StatusCode = StatusCodes.OK, Message = "Cart cleared successfully" };
        }
    }
}
