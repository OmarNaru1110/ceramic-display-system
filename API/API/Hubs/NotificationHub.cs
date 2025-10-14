using Core.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API.Hubs
{
    [Authorize] // enforce JWT auth for hub connections
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            // Auto-join Admins group if the connected user has Admin role

            var userRoles = UserHelpers.GetUserRoles(Context.User);
            if (userRoles!= null && userRoles.Contains("Admin"))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
            }

            await base.OnConnectedAsync();
        }

        // Optional explicit joins if needed by clients
        public Task JoinAdminsGroup()
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
        }

        public Task LeaveAdminsGroup()
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, "Admins");
        }
    }
}
