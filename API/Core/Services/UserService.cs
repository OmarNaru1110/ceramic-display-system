using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CORE.Constants;
using CORE.DTOs;
using CORE.DTOs.AppUser;
using CORE.Services.IServices;
using Data.DataAccess.Repositories.UnitOfWork;
using Data.Models;
using DATA.Constants.Enums;
using Microsoft.AspNetCore.Identity;

namespace CORE.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;
        private readonly IMapper _mapper;

        public UserService(IUnitOfWork unitOfWork, UserManager<AppUser> userManager, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task<ResponseDto<object>> DeleteUserAsync(int userId)
        {
            var user = await _unitOfWork.AppUsers.GetAsync(userId);
            if (user == null)
                return new ResponseDto<object> { StatusCode = StatusCodes.NotFound, Message = "User not found" };

            // Use UserManager to ensure identity-related cleanup
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new ResponseDto<object> { StatusCode = StatusCodes.InternalServerError, Message = $"Failed to delete user: {errors}" };
            }

            return new ResponseDto<object> { StatusCode = StatusCodes.OK, Message = "User deleted successfully" };
        }

        public async Task<ResponseDto<PagedResultDto<GetUserDto>>> GetUsersAsync(UserFilterDto filter)
        {
            // Base query with pagination
            var pageNumber = filter?.PageNumber > 0 ? filter.PageNumber : 1;
            var pageSize = filter?.PageSize > 0 ? filter.PageSize : 20;

            // We need to filter by role, but roles are in AspNetUserRoles.
            // Fetch users and then attach roles.
            var criteria = PredicateBuilder.True<AppUser>();

            if (!string.IsNullOrWhiteSpace(filter?.SearchTerm))
            {
                var term = filter.SearchTerm.ToLower();
                criteria = criteria.And(u => (u.UserName != null && u.UserName.ToLower().Contains(term))
                                        || (u.Email != null && u.Email.ToLower().Contains(term)));
            }

            var totalCount = await _unitOfWork.AppUsers.CountAsync(criteria);
            var users = await _unitOfWork.AppUsers.FindAsync(
                criteria,
                pageNumber,
                pageSize,
                includes: null,
                sortingExpression: u => u.CreatedDate,
                sortingDirection: OrderBy.Descending
            );

            var list = new List<GetUserDto>();
            foreach (var u in users)
            {
                var dto = _mapper.Map<GetUserDto>(u);
                dto.Roles = (await _userManager.GetRolesAsync(u)).ToList();
                list.Add(dto);
            }

            if (!string.IsNullOrWhiteSpace(filter?.Role))
            {
                var roleFilter = filter.Role.Trim();
                list = list.Where(x => x.Roles.Any(r => string.Equals(r, roleFilter, StringComparison.OrdinalIgnoreCase))).ToList();
            }

            return new ResponseDto<PagedResultDto<GetUserDto>>
            {
                StatusCode = StatusCodes.OK,
                Data = new PagedResultDto<GetUserDto>
                {
                    Items = list,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                }
            };
        }

        public async Task<ResponseDto<UserProfileDto>> GetProfileAsync(int userId)
        {
            var user = await _unitOfWork.AppUsers.GetAsync(userId);
            if (user == null)
                return new ResponseDto<UserProfileDto> { StatusCode = StatusCodes.NotFound, Message = "User not found" };

            return new ResponseDto<UserProfileDto>
            {
                StatusCode = StatusCodes.OK,
                Data = _mapper.Map<UserProfileDto>(user)
            };
        }

        public async Task<ResponseDto<UserProfileDto>> UpdateProfileAsync(int userId, UpdateUserProfileDto dto)
        {
            var user = await _unitOfWork.AppUsers.GetAsync(userId);
            if (user == null)
                return new ResponseDto<UserProfileDto> { StatusCode = StatusCodes.NotFound, Message = "User not found" };

            // Apply changes if provided
            if (!string.IsNullOrWhiteSpace(dto.UserName)) user.UserName = dto.UserName;
            if (!string.IsNullOrWhiteSpace(dto.Email)) user.Email = dto.Email;
            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber)) user.PhoneNumber = dto.PhoneNumber;

            // Use UserManager to update to respect Identity rules
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new ResponseDto<UserProfileDto> { StatusCode = StatusCodes.BadRequest, Message = $"Failed to update profile: {errors}" };
            }

            return new ResponseDto<UserProfileDto>
            {
                StatusCode = StatusCodes.OK,
                Message = "Profile updated successfully",
                Data = _mapper.Map<UserProfileDto>(user)
            };
        }

        public async Task<ResponseDto<object>> UpdatePasswordAsync(int userId, UpdatePasswordDto dto)
        {
            var user = await _unitOfWork.AppUsers.GetAsync(userId);
            if (user == null)
                return new ResponseDto<object> { StatusCode = StatusCodes.NotFound, Message = "User not found" };

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new ResponseDto<object> { StatusCode = StatusCodes.BadRequest, Message = errors };
            }

            return new ResponseDto<object> { StatusCode = StatusCodes.OK, Message = "Password updated successfully" };
        }
    }

    internal static class PredicateBuilder
    {
        public static System.Linq.Expressions.Expression<Func<T, bool>> True<T>() { return f => true; }
        public static System.Linq.Expressions.Expression<Func<T, bool>> False<T>() { return f => false; }

        public static System.Linq.Expressions.Expression<Func<T, bool>> And<T>(this System.Linq.Expressions.Expression<Func<T, bool>> expr1,
            System.Linq.Expressions.Expression<Func<T, bool>> expr2)
        {
            var invokedExpr = System.Linq.Expressions.Expression.Invoke(expr2, expr1.Parameters.Cast<System.Linq.Expressions.Expression>());
            return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(System.Linq.Expressions.Expression.AndAlso(expr1.Body, invokedExpr), expr1.Parameters);
        }
    }
}
