using DATA.Models.Contract;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Models
{
    [Owned]
    public class Size
    {
        public double Length { get; set; }

        public double Width { get; set; }
    }
}