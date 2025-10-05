using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Data.Models;
using Data.Models.Enums;

namespace Core.DTOs.Product;
public class CreateProductDto
{

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public QualityGrade QualityGrade { get; set; }

    [MaxLength(200)]
    public string Name { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProductCategory Category { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProductType Type { get; set; }

    public int Quantity { get; set; }

    public Size Size { get; set; }

    public int PiecesPerBox { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PricePerSqm { get; set; }
}
