using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs.Product;

public class ProductImageDto
{
    public int Id { get; set; }
    public string ImageURL { get; set; }
    public string? AltText { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedDate { get; set; }
}
