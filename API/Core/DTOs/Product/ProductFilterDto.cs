using Data.Models.Enums;
using System.Text.Json.Serialization;

namespace Core.DTOs.Product
{
    public class ProductFilterDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public QualityGrade? QualityGrade { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ProductCategory? Category { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ProductType? Type { get; set; }

        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        public string? SearchTerm { get; set; }

        public string? SortBy { get; set; } // "price_asc", "price_desc", "name", "date"
    }
}
