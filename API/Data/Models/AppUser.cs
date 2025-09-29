using DATA.Models.Contract;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Models
{
    public class AppUser : IdentityUser<int>, ISoftDeleteable
    {
        public bool IsDeleted { get; set; } = false;
        public DateTime? DateDeleted { get; set; }
    }
}
