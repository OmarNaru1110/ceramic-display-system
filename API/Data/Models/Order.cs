using DATA.Models.Contract;
using Data.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Models
{
    public class Order : ISoftDeleteable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SellerId { get; set; }

        [Required]
        [MaxLength(200)]
        public string CustomerName { get; set; }

        [Required]
        [MaxLength(100)]
        public string CustomerContact { get; set; }

        [Required]
        [MaxLength(500)]
        public string CustomerAddress { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedDate { get; set; }

        // ISoftDeleteable implementation
        public bool IsDeleted { get; set; } = false;
        public DateTime? DateDeleted { get; set; }

        // Navigation properties
        [ForeignKey("SellerId")]
        public virtual AppUser Seller { get; set; }

        public virtual Invoice? Invoice { get; set; } // order : invoice 1:1
        public virtual ReturnRequest? ReturnRequest { get; set; } // order : return_request 1:1
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>(); // order : order_item 1:m
    }
}