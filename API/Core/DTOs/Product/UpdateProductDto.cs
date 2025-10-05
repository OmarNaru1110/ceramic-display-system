using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Data.Models;
using Data.Models.Enums;

namespace Core.DTOs.Product;
public class UpdateProductDto
{
    public int Id { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public QualityGrade QualityGrade { get; set; }

    public string Name { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProductCategory Category { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProductType Type { get; set; }
    public int Quantity { get; set; }

    public Size Size { get; set; }

    public int PiecesPerBox { get; set; }

    public decimal PricePerSqm { get; set; }

    public int AdminId { get; set; } // user : product 1:m
    public DateTime CreatedDate { get; set; }
}
