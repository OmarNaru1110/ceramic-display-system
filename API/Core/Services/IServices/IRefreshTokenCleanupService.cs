using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CORE.Services.IServices
{
    public interface IRefreshTokenCleanupService
    {
        /// <summary>
        /// Deletes all inactive refresh tokens (expired or revoked) from the database
        /// </summary>
        /// <returns>The number of tokens deleted</returns>
        Task<int> DeleteInactiveRefreshTokensAsync();
    }
}
