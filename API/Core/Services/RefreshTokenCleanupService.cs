using CORE.Services.IServices;
using Data.Models;
using DATA.DataAccess.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CORE.Services
{
    public class RefreshTokenCleanupService : IRefreshTokenCleanupService
    {
        private readonly ILogger<RefreshTokenCleanupService> _logger;
        private readonly AppDbContext _context;

        public RefreshTokenCleanupService(
            ILogger<RefreshTokenCleanupService> logger,
            AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<int> DeleteInactiveRefreshTokensAsync()
        {
            try
            {
                _logger.LogInformation("Starting refresh token cleanup process");

                var currentTime = DateTime.UtcNow;
                
                // Get all users with their refresh tokens in a single efficient query
                // We only load the necessary data to minimize memory usage
                var usersWithInactiveTokens = await _context.Users
                    .Where(u => u.RefreshTokens.Any(rt => 
                        rt.ExpiresOn < currentTime || rt.RevokedOn != null))
                    .Select(u => new
                    {
                        User = u,
                        InactiveTokens = u.RefreshTokens
                            .Where(rt => rt.ExpiresOn < currentTime || rt.RevokedOn != null)
                            .ToList()
                    })
                    .ToListAsync();

                if (!usersWithInactiveTokens.Any())
                {
                    _logger.LogInformation("No inactive refresh tokens found to delete");
                    return 0;
                }

                int totalTokensDeleted = 0;

                foreach (var userWithTokens in usersWithInactiveTokens)
                {
                    var user = userWithTokens.User;
                    var inactiveTokens = userWithTokens.InactiveTokens;

                    _logger.LogDebug(
                        "User {UserId} ({Username}) has {Count} inactive tokens to remove",
                        user.Id,
                        user.UserName,
                        inactiveTokens.Count);

                    foreach (var token in inactiveTokens)
                    {
                        user.RefreshTokens.Remove(token);
                        totalTokensDeleted++;
                    }
                }

                // Save all changes in a single transaction
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Successfully deleted {Count} inactive refresh tokens from {UserCount} users",
                    totalTokensDeleted,
                    usersWithInactiveTokens.Count);

                return totalTokensDeleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cleaning up inactive refresh tokens");
                throw;
            }
        }
    }
}
