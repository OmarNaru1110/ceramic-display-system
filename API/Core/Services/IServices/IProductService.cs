using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.DTOs.Product;
using CORE.DTOs;

namespace Core.Services.IServices;
public interface IProductService
{
    Task<ResponseDto<GetProductDto>> CreateProductAsync(CreateProductDto createProductDto, int? adminId);
    Task<ResponseDto<GetProductDto>> UpdateProductAsync(UpdateProductDto updateProductDto);
    Task<ResponseDto<object>> DeleteProductsAsync(int productId);
}
