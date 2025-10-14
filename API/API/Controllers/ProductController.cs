using Core.DTOs.Product;
using Core.Helpers;
using Core.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
[Route("api/[controller]")]
[ApiController]
public class ProductController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateAsync(CreateProductDto dto)
    {
        var adminId = UserHelpers.GetUserId(User);
        var result = await _productService.CreateProductAsync(dto, adminId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateAsync(int id, UpdateProductDto dto)
    {
        var adminId = UserHelpers.GetUserId(User);
        var result = await _productService.UpdateProductAsync(id, dto, adminId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var adminId = UserHelpers.GetUserId(User);
        var result = await _productService.DeleteProductAsync(id, adminId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetByIdAsync(int id)
    {
        var result = await _productService.GetProductByIdAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync([FromQuery] ProductFilterDto filter)
    {
        var result = await _productService.GetProductsAsync(filter);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{id}/images")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AddImagesAsync(int id, [FromForm] List<IFormFile> images)
    {
        var adminId = UserHelpers.GetUserId(User);
        var result = await _productService.AddProductImagesAsync(id, images, adminId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{productId}/images/{imageId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteImageAsync(int productId, int imageId)
    {
        var adminId = UserHelpers.GetUserId(User);
        var result = await _productService.DeleteProductImageAsync(productId, imageId, adminId);
        return StatusCode(result.StatusCode, result);
    }
}
