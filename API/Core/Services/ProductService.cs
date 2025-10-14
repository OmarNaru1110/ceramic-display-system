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
using Core.Helpers;

namespace Core.Services;
public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IFileStorageService _fileStorageService;

    public ProductService(IUnitOfWork unitOfWork, IMapper mapper, IFileStorageService fileStorageService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _fileStorageService = fileStorageService;
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
            return "Quantity cannot be negative";
        if (dto.PiecesPerBox <= 0)
            return "PiecesPerBox must be greater than 0";
        if (dto.PricePerSqm <= 0)
            return "PricePerSqm must be greater than 0";
        if (dto.Size == null)
            return "Size is required";
        if (dto.Size.Length <= 0 || dto.Size.Width <= 0)
            return "Size dimensions must be greater than 0";
        return null;
    }

    private string? ValidateUpdateProductDto(UpdateProductDto dto)
    {
        if (dto == null)
            return "Invalid payload";

        // If name is provided, it must be non-empty and not too long
        if (dto.Name != null)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return "Name is required";
            if (dto.Name.Length > 200)
                return "Name must be less than 200 characters";
        }

        // If quantity provided, it cannot be negative
        if (dto.Quantity.HasValue && dto.Quantity.Value < 0)
            return "Quantity cannot be negative";

        // PiecesPerBox and PricePerSqm must be positive if provided
        if (dto.PiecesPerBox.HasValue && dto.PiecesPerBox.Value <= 0)
            return "PiecesPerBox must be greater than 0";
        if (dto.PricePerSqm.HasValue && dto.PricePerSqm.Value <= 0)
            return "PricePerSqm must be greater than 0";

        // Size if provided must have positive dimensions
        if (dto.Size != null)
        {
            if (dto.Size.Length <= 0 || dto.Size.Width <= 0)
                return "Size dimensions must be greater than 0";
        }

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
        product.AdminId = adminId.Value;


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

    public async Task<ResponseDto<object>> DeleteProductAsync(int productId, int? adminId)
    {
        if (adminId == null)
            return new ResponseDto<object>
            {
                StatusCode = StatusCodes.Unauthorized,
                Message = "Admin token is required"
            };

        var product = await _unitOfWork.Products.GetAsync(productId);
        if (product == null)
            return new ResponseDto<object>
            {
                StatusCode = StatusCodes.NotFound,
                Message = "Product not found"
            };

        _unitOfWork.Products.Delete(product);
        var changes = await _unitOfWork.CommitAsync();

        if (changes == 0)
            return new ResponseDto<object>
            {
                StatusCode = StatusCodes.InternalServerError,
                Message = "Failed to delete product"
            };

        return new ResponseDto<object>
        {
            StatusCode = StatusCodes.OK,
            Message = "Product deleted successfully"
        };
    }

    public async Task<ResponseDto<GetProductDto>> UpdateProductAsync(int productId, UpdateProductDto updateProductDto, int? adminId)
    {
        if (adminId == null)
            return new ResponseDto<GetProductDto>
            {
                StatusCode = StatusCodes.Unauthorized,
                Message = "Admin token is required"
            };

        if (ValidateUpdateProductDto(updateProductDto) is string updateError)
            return new ResponseDto<GetProductDto>
            {
                StatusCode = StatusCodes.BadRequest,
                Message = updateError
            };

        var product = await _unitOfWork.Products.GetAsync(productId);
        if (product == null)
            return new ResponseDto<GetProductDto>
            {
                StatusCode = StatusCodes.NotFound,
                Message = "Product not found"
            };

        // Update only provided fields
        if (!string.IsNullOrWhiteSpace(updateProductDto.Name))
            product.Name = updateProductDto.Name;
        if (updateProductDto.QualityGrade.HasValue)
            product.QualityGrade = updateProductDto.QualityGrade.Value;
        if (updateProductDto.Category.HasValue)
            product.Category = updateProductDto.Category.Value;
        if (updateProductDto.Type.HasValue)
            product.Type = updateProductDto.Type.Value;
        if (updateProductDto.Quantity.HasValue)
            product.Quantity = updateProductDto.Quantity.Value;
        if (updateProductDto.Size != null)
            product.Size = updateProductDto.Size;
        if (updateProductDto.PiecesPerBox.HasValue)
            product.PiecesPerBox = updateProductDto.PiecesPerBox.Value;
        if (updateProductDto.PricePerSqm.HasValue)
            product.PricePerSqm = updateProductDto.PricePerSqm.Value;

        await _unitOfWork.Products.AddOrUpdateAsync(product);
        var changes = await _unitOfWork.CommitAsync();

        if (changes == 0)
            return new ResponseDto<GetProductDto>
            {
                StatusCode = StatusCodes.InternalServerError,
                Message = "Failed to update product"
            };

        return new ResponseDto<GetProductDto>
        {
            StatusCode = StatusCodes.OK,
            Message = "Product updated successfully",
            Data = _mapper.Map<GetProductDto>(product)
        };
    }

    public async Task<ResponseDto<GetProductDto>> GetProductByIdAsync(int productId)
    {
        var product = await _unitOfWork.Products.FindAsync(
            p => p.Id == productId,
            new[] { "ProductImages" }
        );

        if (product == null)
            return new ResponseDto<GetProductDto>
            {
                StatusCode = StatusCodes.NotFound,
                Message = "Product not found"
            };

        return new ResponseDto<GetProductDto>
        {
            StatusCode = StatusCodes.OK,
            Data = _mapper.Map<GetProductDto>(product)
        };
    }

    public async Task<ResponseDto<PagedResultDto<GetProductDto>>> GetProductsAsync(ProductFilterDto filter)
    {
        // Build the filter expression
        System.Linq.Expressions.Expression<Func<Product, bool>> criteria = p => true;

        if (filter.QualityGrade.HasValue)
            criteria = criteria.AndAlso(p => p.QualityGrade == filter.QualityGrade.Value);
        if (filter.Category.HasValue)
            criteria = criteria.AndAlso(p => p.Category == filter.Category.Value);
        if (filter.Type.HasValue)
            criteria = criteria.AndAlso(p => p.Type == filter.Type.Value);
        if (filter.MinPrice.HasValue)
            criteria = criteria.AndAlso(p => p.PricePerSqm >= filter.MinPrice.Value);
        if (filter.MaxPrice.HasValue)
            criteria = criteria.AndAlso(p => p.PricePerSqm <= filter.MaxPrice.Value);
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            criteria = criteria.AndAlso(p => p.Name.Contains(filter.SearchTerm));

        // Determine sorting
        System.Linq.Expressions.Expression<Func<Product, object>> sortExpression = filter.SortBy?.ToLower() switch
        {
            "price_asc" => p => p.PricePerSqm,
            "price_desc" => p => p.PricePerSqm,
            "name" => p => p.Name,
            "date" => p => p.CreatedDate,
            _ => p => p.CreatedDate
        };

        var sortDirection = filter.SortBy?.ToLower() == "price_desc" 
            ? DATA.Constants.Enums.OrderBy.Descending 
            : DATA.Constants.Enums.OrderBy.Ascending;

        var totalCount = await _unitOfWork.Products.CountAsync(criteria);
        var products = await _unitOfWork.Products.FindAsync(
            criteria,
            filter.PageNumber,
            filter.PageSize,
            new[] { "ProductImages" },
            sortExpression,
            sortDirection
        );

        var productDtos = _mapper.Map<IEnumerable<GetProductDto>>(products);

        return new ResponseDto<PagedResultDto<GetProductDto>>
        {
            StatusCode = StatusCodes.OK,
            Data = new PagedResultDto<GetProductDto>
            {
                Items = productDtos,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            }
        };
    }

    public async Task<ResponseDto<IEnumerable<string>>> AddProductImagesAsync(int productId, List<Microsoft.AspNetCore.Http.IFormFile> images, int? adminId)
    {
        if (adminId == null)
            return new ResponseDto<IEnumerable<string>>
            {
                StatusCode = StatusCodes.Unauthorized,
                Message = "Admin token is required"
            };

        var product = await _unitOfWork.Products.GetAsync(productId);
        if (product == null)
            return new ResponseDto<IEnumerable<string>>
            {
                StatusCode = StatusCodes.NotFound,
                Message = "Product not found"
            };

        if (images == null || images.Count == 0)
        {
            return new ResponseDto<IEnumerable<string>>
            {
                StatusCode = StatusCodes.BadRequest,
                Message = "No images provided"
            };
        }

        // Filter out invalid files
        var validImages = images.Where(i => i != null && i.Length > 0).ToList();
        if (validImages.Count == 0)
        {
            return new ResponseDto<IEnumerable<string>>
            {
                StatusCode = StatusCodes.BadRequest,
                Message = "No valid images provided"
            };
        }

        try
        {
            var folder = $"products/{productId}";
            var urls = new List<string>();

            // Determine starting display order
            var startOrder = await _unitOfWork.ProductImages.CountAsync(pi => pi.ProductId == productId);

            foreach (var (file, index) in validImages.Select((f, idx) => (f, idx)))
            {
                var url = await _fileStorageService.UploadFileAsync(file, folder);
                urls.Add(url);

                var productImage = new ProductImage
                {
                    ProductId = productId,
                    ImageURL = url,
                    AltText = product.Name,
                    DisplayOrder = startOrder + index
                };

                await _unitOfWork.ProductImages.AddOrUpdateAsync(productImage);
            }

            var changes = await _unitOfWork.CommitAsync();
            if (changes == 0)
            {
                return new ResponseDto<IEnumerable<string>>
                {
                    StatusCode = StatusCodes.InternalServerError,
                    Message = "Failed to save product images"
                };
            }

            return new ResponseDto<IEnumerable<string>>
            {
                StatusCode = StatusCodes.OK,
                Message = "Images uploaded successfully",
                Data = urls
            };
        }
        catch (Exception ex)
        {
            return new ResponseDto<IEnumerable<string>>
            {
                StatusCode = StatusCodes.InternalServerError,
                Message = $"Image upload failed: {ex.Message}"
            };
        }
    }

    public async Task<ResponseDto<object>> DeleteProductImageAsync(int productId, int imageId, int? adminId)
    {
        if (adminId == null)
            return new ResponseDto<object>
            {
                StatusCode = StatusCodes.Unauthorized,
                Message = "Admin token is required"
            };

        var image = await _unitOfWork.ProductImages.FindAsync(
            i => i.Id == imageId && i.ProductId == productId,
            null
        );

        if (image == null)
            return new ResponseDto<object>
            {
                StatusCode = StatusCodes.NotFound,
                Message = "Image not found"
            };

        var imageUrl = image.ImageURL;
        _unitOfWork.ProductImages.Delete(image);
        var changes = await _unitOfWork.CommitAsync();

        if (changes == 0)
            return new ResponseDto<object>
            {
                StatusCode = StatusCodes.InternalServerError,
                Message = "Failed to delete image"
            };

        // Best-effort delete from storage (ignore result)
        _ = _fileStorageService.DeleteFileAsync(imageUrl);

        return new ResponseDto<object>
        {
            StatusCode = StatusCodes.OK,
            Message = "Image deleted successfully"
        };
    }
}
