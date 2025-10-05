using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Core.DTOs.Product;
using Core.Services.IServices;
using CORE.Constants;
using CORE.DTOs;
using Data.DataAccess.Repositories.UnitOfWork;
using Data.Models;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Core.Services;
public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProductService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    private string? ValidateCreateProductDto(CreateProductDto dto)
    {
        if (dto.QualityGrade == null)
            return "QualityGrade is required";
        if (string.IsNullOrWhiteSpace(dto.Name))
            return "Name is required";
        if (dto.Name.Length > 200)
            return "Name must be less than 200 characters";
        if (dto.Category == null)
            return "Category is required";
        if (dto.Type == null)
            return "Type is required";
        if (dto.Quantity < 0)
            return "Quantity must be greater than or equal to 0";
        if (dto.PiecesPerBox <= 0)
            return "PiecesPerBox must be greater than 0";
        if (dto.PricePerSqm <= 0)
            return "PricePerSqm must be greater than 0";
        return null;
    }
    public async Task<ResponseDto<GetProductDto>> CreateProductAsync(CreateProductDto createProductDto, int? adminId)
    {
        if (adminId == null)
            return new ResponseDto<GetProductDto>
            {
                StatusCode = StatusCodes.Unauthorized,
                Message = "Admin token is required"
            };
        if (ValidateCreateProductDto(createProductDto) is string errorMsg)
            return new ResponseDto<GetProductDto>
            {
                StatusCode = StatusCodes.BadRequest,
                Message = errorMsg
            };

        var product = _mapper.Map<Product>(createProductDto);
        if (product == null)
        {
            return new ResponseDto<GetProductDto>
            {
                StatusCode = StatusCodes.InternalServerError,
                Message = "Mapping failed"
            };
        }

        await _unitOfWork.Products.AddOrUpdateAsync(product);
        var changes = await _unitOfWork.CommitAsync();
        if (changes == 0)
        {
            return new ResponseDto<GetProductDto>
            {
                StatusCode = StatusCodes.InternalServerError,
                Message = "Failed to add product"
            };
        }

        return new ResponseDto<GetProductDto>
        {
            StatusCode = StatusCodes.Created,
            Message = "Product added successfully",
            Data = _mapper.Map<GetProductDto>(product)
        };
    }

    public async Task<ResponseDto<object>> DeleteProductsAsync(int productId)
    {
        throw new NotImplementedException();
    }

    public async Task<ResponseDto<GetProductDto>> UpdateProductAsync(UpdateProductDto updateProductDto)
    {
        throw new NotImplementedException();
    }
}
