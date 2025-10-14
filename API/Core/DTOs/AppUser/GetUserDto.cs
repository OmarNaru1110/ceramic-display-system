using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CORE.DTOs.AppUser
{
    public class GetUserDto
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public DateTime CreatedDate { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
    }

    public class UserFilterDto
    {
        // Optional role filter: Admin, Seller, null for all
        public string? Role { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SearchTerm { get; set; }
    }

    public class UpdateUserProfileDto
    {
        [MaxLength(256)]
        public string UserName { get; set; }
        [EmailAddress]
        public string Email { get; set; }
        [Phone]
        public string PhoneNumber { get; set; }
    }

    public class UpdatePasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; }
        [Required]
        public string NewPassword { get; set; }
        [Required, DataType(DataType.Password), Compare("NewPassword")]
        public string ConfirmPassword { get; set; }
    }
}
