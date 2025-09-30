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
    public class Invoice : ISoftDeleteable
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

        public string? PdfUrl { get; set; }

        // ISoftDeleteable implementation
        public bool IsDeleted { get; set; } = false;
        public DateTime? DateDeleted { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; } // order : invoice 1:1
    }
}