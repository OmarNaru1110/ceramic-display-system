using System.Threading.Tasks;
using Core.DTOs.Order;

namespace Core.Services.IServices
{
    // Abstraction for notifying admins about events (implemented in API via SignalR)
    public interface IAdminNotificationService
    {
        Task NotifyOrderPlacedAsync(GetOrderDto order);
    }
}
