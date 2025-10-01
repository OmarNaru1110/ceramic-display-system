using DATA.Models.Contract;
using Data.Models.Enums;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DATA.Models;

namespace Data.Models
{
    public class AppUser : IdentityUser<int>, ISoftDeleteable
    {

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // ISoftDeleteable implementation
        public bool IsDeleted { get; set; } = false;
        public DateTime? DateDeleted { get; set; }

        // Navigation properties
        public virtual ICollection<Product> Products { get; set; } = new List<Product>(); // user : product 1:m
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>(); // user : order 1:m
        public virtual Cart? Cart { get; set; } // user : cart 1:1
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>(); // user : refresh_token 1:m
    }
}
