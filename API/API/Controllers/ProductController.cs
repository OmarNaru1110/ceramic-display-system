using Core.DTOs.Product;
using Core.Services.IServices;
using CORE.Helpers;
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
}
