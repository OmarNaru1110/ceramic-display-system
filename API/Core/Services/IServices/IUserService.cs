using System.Collections.Generic;
using CORE.DTOs;
using CORE.DTOs.AppUser;
using System.Threading.Tasks;

namespace CORE.Services.IServices
{
    public interface IUserService
    {
        Task<ResponseDto<object>> DeleteUserAsync(int userId);
        Task<ResponseDto<PagedResultDto<GetUserDto>>> GetUsersAsync(UserFilterDto filter);
        Task<ResponseDto<UserProfileDto>> GetProfileAsync(int userId);
        Task<ResponseDto<UserProfileDto>> UpdateProfileAsync(int userId, UpdateUserProfileDto dto);
        Task<ResponseDto<object>> UpdatePasswordAsync(int userId, UpdatePasswordDto dto);
    }
}
