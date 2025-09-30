using DATA.Models.Contract;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Models
{
    public class Cart : ISoftDeleteable
    {
        public int Id { get; set; }

        [Required]
        public int SellerId { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // ISoftDeleteable implementation
        public bool IsDeleted { get; set; } = false;
        public DateTime? DateDeleted { get; set; }

        // Navigation properties
        public virtual AppUser? AppUser { get; set; } // appuser : cart 1:1
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>(); // cart : cart_item 1:m
    }
}