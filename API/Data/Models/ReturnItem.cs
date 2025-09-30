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
    public class ReturnItem : ISoftDeleteable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ReturnRequestId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [MaxLength(500)]
        public string Reason { get; set; }

        // ISoftDeleteable implementation
        public bool IsDeleted { get; set; } = false;
        public DateTime? DateDeleted { get; set; }

        // Navigation properties
        [ForeignKey("ReturnRequestId")]
        public virtual ReturnRequest ReturnRequest { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
    }
}