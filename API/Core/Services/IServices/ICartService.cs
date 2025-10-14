using Core.DTOs.Cart;
using CORE.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services.IServices
{
    public interface ICartService
    {
        Task<ResponseDto<GetCartDto>> GetCartAsync(int? sellerId);
        Task<ResponseDto<GetCartDto>> AddItemToCartAsync(AddCartItemDto dto, int? sellerId);
        Task<ResponseDto<GetCartDto>> UpdateCartItemAsync(int cartItemId, UpdateCartItemDto dto, int? sellerId);
        Task<ResponseDto<object>> RemoveCartItemAsync(int cartItemId, int? sellerId);
        Task<ResponseDto<object>> ClearCartAsync(int? sellerId);
    }
}
