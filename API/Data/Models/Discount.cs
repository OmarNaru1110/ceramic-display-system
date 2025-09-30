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
    public class Discount : ISoftDeleteable
    {
        public int Id { get; set; }

        [Required]
        [Range(0, 100)]
        public decimal Percentage { get; set; }

        [Required]
        public int DaysRequired { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Required]
        public int ProductId { get; set; }

        // ISoftDeleteable implementation
        public bool IsDeleted { get; set; } = false;
        public DateTime? DateDeleted { get; set; }

    }
}