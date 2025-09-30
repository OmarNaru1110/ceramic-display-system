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
    public class ReturnRequest : ISoftDeleteable
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public string ReturnReason { get; set; }

        public string? Description { get; set; }

        [Required]
        public ReturnStatus Status { get; set; } = ReturnStatus.Pending;

        [Required]
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        public string? AdminNotes { get; set; }

        // ISoftDeleteable implementation
        public bool IsDeleted { get; set; } = false;
        public DateTime? DateDeleted { get; set; }
        // Navigation properties
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }
        public virtual ICollection<ReturnItem> ReturnItems { get; set; } = new List<ReturnItem>();
    }
}