using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.DTOs.Product;
using CORE.DTOs;
using Microsoft.AspNetCore.Http;

namespace Core.Services.IServices;
public interface IProductService
{
    Task<ResponseDto<GetProductDto>> CreateProductAsync(CreateProductDto createProductDto, int? adminId);
    Task<ResponseDto<GetProductDto>> UpdateProductAsync(int productId, UpdateProductDto updateProductDto, int? adminId);
    Task<ResponseDto<object>> DeleteProductAsync(int productId, int? adminId);
    Task<ResponseDto<GetProductDto>> GetProductByIdAsync(int productId);
    Task<ResponseDto<PagedResultDto<GetProductDto>>> GetProductsAsync(ProductFilterDto filter);
    Task<ResponseDto<IEnumerable<string>>> AddProductImagesAsync(int productId, List<IFormFile> images, int? adminId);
    Task<ResponseDto<object>> DeleteProductImageAsync(int productId, int imageId, int? adminId);
}
