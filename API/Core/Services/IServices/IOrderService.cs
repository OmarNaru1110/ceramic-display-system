using Core.DTOs.Order;
using CORE.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services.IServices
{
    public interface IOrderService
    {
        Task<ResponseDto<GetOrderDto>> CreateOrderAsync(CreateOrderDto dto, int? sellerId);
        Task<ResponseDto<GetOrderDto>> UpdateOrderAsync(int orderId, UpdateOrderDto dto, int? sellerId);
        Task<ResponseDto<object>> CancelOrderAsync(int orderId, int? sellerId);
        Task<ResponseDto<GetOrderDto>> GetOrderByIdAsync(int orderId, int? userId, string[]? roles);
        Task<ResponseDto<PagedResultDto<GetOrderDto>>> GetOrdersAsync(OrderFilterDto filter, int? userId, string[]? roles);
        Task<ResponseDto<GetOrderDto>> ConfirmOrderAsync(int orderId, int? adminId);
        Task<ResponseDto<GetOrderDto>> RejectOrderAsync(int orderId, int? adminId, string? reason);
        Task<ResponseDto<GetOrderDto>> CompleteOrderAsync(int orderId, int? sellerId);
    }
}
