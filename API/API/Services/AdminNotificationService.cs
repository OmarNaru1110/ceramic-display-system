using System.Threading.Tasks;
using API.Hubs;
using Core.Services.IServices;
using Core.DTOs.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.Services
{
    // SignalR implementation of admin notifications
    [Authorize]
    public class AdminNotificationService : IAdminNotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public AdminNotificationService(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyOrderPlacedAsync(GetOrderDto order)
        {
            // Broadcast to Admins group and also allow client-side filtering by role
            await _hubContext.Clients.Group("Admins").SendAsync("OrderPlaced", order);
        }
    }
}
