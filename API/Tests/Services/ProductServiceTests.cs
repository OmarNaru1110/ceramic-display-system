using NUnit.Framework;
using Tests.Helpers;
using Core.Services;
using Core.Services.IServices;
using Core.DTOs.Product;
using CORE.DTOs;
using Data.Models;
using DATA.DataAccess.Repositories.IRepositories;
using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using StatusCodes = CORE.Constants.StatusCodes;

namespace Tests.Services
{
    [TestFixture]
    public class ProductServiceTests : TestBase
    {
        private ProductService _productService = null!;
        private Mock<IFileStorageService> _mockFileStorage = null!;
        private Mock<IBaseRepository<Product>> _mockProductRepo = null!;
        private Mock<IBaseRepository<ProductImage>> _mockProductImageRepo = null!;

        [SetUp]
        public override async Task SetUp()
        {
            await base.SetUp();

            _mockFileStorage = new Mock<IFileStorageService>();
            _mockProductRepo = new Mock<IBaseRepository<Product>>();
            _mockProductImageRepo = new Mock<IBaseRepository<ProductImage>>();

            MockUnitOfWork.Setup(u => u.Products).Returns(_mockProductRepo.Object);
            MockUnitOfWork.Setup(u => u.ProductImages).Returns(_mockProductImageRepo.Object);

            _productService = new ProductService(MockUnitOfWork.Object, MockMapper.Object, _mockFileStorage.Object);
        }

        #region CreateProductAsync
        [Test]
        public async Task CreateProductAsync_Unauthorized_WhenAdminIdIsNull()
        {
            var dto = ValidCreateDto();
            var result = await _productService.CreateProductAsync(dto, null);
            result.StatusCode.Should().Be(StatusCodes.Unauthorized);
        }

        [Test]
        public async Task CreateProductAsync_BadRequest_WhenNameIsMissing()
        {
            var dto = ValidCreateDto();
            dto.Name = null!;
            var result = await _productService.CreateProductAsync(dto, 1);
            result.StatusCode.Should().Be(StatusCodes.BadRequest);
            result.Message.Should().Be("Name is required");
        }

        [Test]
        public async Task CreateProductAsync_BadRequest_WhenNameTooLong()
        {
            var dto = ValidCreateDto();
            dto.Name = new string('a', 201);
            var result = await _productService.CreateProductAsync(dto, 1);
            result.StatusCode.Should().Be(StatusCodes.BadRequest);
            result.Message.Should().Be("Name must be less than 200 characters");
        }

        [Test]
        public async Task CreateProductAsync_BadRequest_WhenQuantityNegative()
        {
            var dto = ValidCreateDto();
            dto.Quantity = -1;
            var result = await _productService.CreateProductAsync(dto, 1);
            result.StatusCode.Should().Be(StatusCodes.BadRequest);
            result.Message.Should().Be("Quantity cannot be negative");
        }

        [Test]
        public async Task CreateProductAsync_BadRequest_WhenPiecesPerBoxNotPositive()
        {
            var dto = ValidCreateDto();
            dto.PiecesPerBox = 0;
            var result = await _productService.CreateProductAsync(dto, 1);
            result.StatusCode.Should().Be(StatusCodes.BadRequest);
            result.Message.Should().Be("PiecesPerBox must be greater than 0");
        }

        [Test]
        public async Task CreateProductAsync_BadRequest_WhenPricePerSqmNotPositive()
        {
            var dto = ValidCreateDto();
            dto.PricePerSqm = 0;
            var result = await _productService.CreateProductAsync(dto, 1);
            result.StatusCode.Should().Be(StatusCodes.BadRequest);
            result.Message.Should().Be("PricePerSqm must be greater than 0");
        }

        [Test]
        public async Task CreateProductAsync_BadRequest_WhenSizeNull()
        {
            var dto = ValidCreateDto();
            dto.Size = null!;
            var result = await _productService.CreateProductAsync(dto, 1);
            result.StatusCode.Should().Be(StatusCodes.BadRequest);
            result.Message.Should().Be("Size is required");
        }

        [Test]
        public async Task CreateProductAsync_BadRequest_WhenSizeDimensionsInvalid()
        {
            var dto = ValidCreateDto();
            dto.Size = new Size { Length = 0, Width = -1 };
            var result = await _productService.CreateProductAsync(dto, 1);
            result.StatusCode.Should().Be(StatusCodes.BadRequest);
            result.Message.Should().Be("Size dimensions must be greater than 0");
        }

        [Test]
        public async Task CreateProductAsync_InternalError_WhenMappingReturnsNull()
        {
            var dto = ValidCreateDto();
            MockMapper.Setup(m => m.Map<Product>(dto)).Returns((Product)null!);

            var result = await _productService.CreateProductAsync(dto, 1);
            result.StatusCode.Should().Be(StatusCodes.InternalServerError);
            result.Message.Should().Be("Mapping failed");
        }

        [Test]
        public async Task CreateProductAsync_InternalError_WhenCommitFails()
        {
            var dto = ValidCreateDto();
            var product = new Product { Id = 1, Name = dto.Name };
            MockMapper.Setup(m => m.Map<Product>(dto)).Returns(product);
            _mockProductRepo.Setup(r => r.AddOrUpdateAsync(product)).ReturnsAsync(product);
            MockUnitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(0);

            var result = await _productService.CreateProductAsync(dto, 1);
            result.StatusCode.Should().Be(StatusCodes.InternalServerError);
            result.Message.Should().Be("Failed to add product");
        }

        [Test]
        public async Task CreateProductAsync_Created_OnSuccess()
        {
            var dto = ValidCreateDto();
            var product = new Product { Id = 1, Name = dto.Name };
            var outDto = new GetProductDto { Id = 1, Name = dto.Name };

            MockMapper.Setup(m => m.Map<Product>(dto)).Returns(product);
            _mockProductRepo.Setup(r => r.AddOrUpdateAsync(product)).ReturnsAsync(product);
            MockUnitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            MockMapper.Setup(m => m.Map<GetProductDto>(product)).Returns(outDto);

            var result = await _productService.CreateProductAsync(dto, 1);
            result.StatusCode.Should().Be(StatusCodes.Created);
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(1);
        }
        #endregion

        #region DeleteProductAsync
        [Test]
        public async Task DeleteProductAsync_Unauthorized_WhenAdminNull()
        {
            var result = await _productService.DeleteProductAsync(1, null);
            result.StatusCode.Should().Be(StatusCodes.Unauthorized);
        }

        [Test]
        public async Task DeleteProductAsync_NotFound_WhenMissing()
        {
            _mockProductRepo.Setup(r => r.GetAsync(1)).ReturnsAsync((Product?)null);
            var result = await _productService.DeleteProductAsync(1, 1);
            result.StatusCode.Should().Be(StatusCodes.NotFound);
        }

        [Test]
        public async Task DeleteProductAsync_InternalError_WhenCommitFails()
        {
            var prod = new Product { Id = 1 };
            _mockProductRepo.Setup(r => r.GetAsync(1)).ReturnsAsync(prod);
            MockUnitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(0);
            var result = await _productService.DeleteProductAsync(1, 1);
            result.StatusCode.Should().Be(StatusCodes.InternalServerError);
        }

        [Test]
        public async Task DeleteProductAsync_Success()
        {
            var prod = new Product { Id = 1 };
            _mockProductRepo.Setup(r => r.GetAsync(1)).ReturnsAsync(prod);
            MockUnitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            var result = await _productService.DeleteProductAsync(1, 1);
            result.StatusCode.Should().Be(StatusCodes.OK);
        }
        #endregion

        #region UpdateProductAsync
        [Test]
        public async Task UpdateProductAsync_Unauthorized_WhenAdminNull()
        {
            var result = await _productService.UpdateProductAsync(1, new UpdateProductDto(), null);
            result.StatusCode.Should().Be(StatusCodes.Unauthorized);
        }

        [Test]
        public async Task UpdateProductAsync_NotFound()
        {
            _mockProductRepo.Setup(r => r.GetAsync(1)).ReturnsAsync((Product?)null);
            var result = await _productService.UpdateProductAsync(1, new UpdateProductDto(), 1);
            result.StatusCode.Should().Be(StatusCodes.NotFound);
        }

        [Test]
        public async Task UpdateProductAsync_InternalError_WhenCommitFails()
        {
            var prod = new Product { Id = 1, Name = "old" };
            _mockProductRepo.Setup(r => r.GetAsync(1)).ReturnsAsync(prod);
            MockUnitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(0);
            var dto = new UpdateProductDto { Name = "new-name" };
            var result = await _productService.UpdateProductAsync(1, dto, 1);
            result.StatusCode.Should().Be(StatusCodes.InternalServerError);
        }

        [Test]
        public async Task UpdateProductAsync_UpdatesOnlyProvidedFields_AndSuccess()
        {
            var prod = new Product
            {
                Id = 1,
                Name = "old",
                Quantity = 100,
                Size = new Size { Length = 10, Width = 10 },
                PiecesPerBox = 5,
                PricePerSqm = 20
            };
            _mockProductRepo.Setup(r => r.GetAsync(1)).ReturnsAsync(prod);
            MockUnitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            var dto = new UpdateProductDto { Name = "updated", Quantity = null, PricePerSqm = 99 };

            var result = await _productService.UpdateProductAsync(1, dto, 1);

            prod.Name.Should().Be("updated");
            prod.Quantity.Should().Be(100); // unchanged
            prod.PricePerSqm.Should().Be(99);
            result.StatusCode.Should().Be(StatusCodes.OK);
        }
        #endregion

        #region GetProductByIdAsync
        [Test]
        public async Task GetProductByIdAsync_NotFound()
        {
            _mockProductRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync((Product?)null);
            var result = await _productService.GetProductByIdAsync(5);
            result.StatusCode.Should().Be(StatusCodes.NotFound);
        }

        [Test]
        public async Task GetProductByIdAsync_Success()
        {
            var prod = new Product { Id = 5, Name = "p" };
            var dto = new GetProductDto { Id = 5, Name = "p" };

            _mockProductRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(prod);
            MockMapper.Setup(m => m.Map<GetProductDto>(prod)).Returns(dto);

            var result = await _productService.GetProductByIdAsync(5);
            result.StatusCode.Should().Be(StatusCodes.OK);
            result.Data!.Id.Should().Be(5);
        }
        #endregion

        #region GetProductsAsync
        [Test]
        public async Task GetProductsAsync_ReturnsPagedData()
        {
            var filter = new ProductFilterDto
            {
                PageNumber = 1,
                PageSize = 10,
                SearchTerm = "tile",
                SortBy = "price_desc"
            };

            var products = new List<Product>
            {
                new Product{ Id = 1, Name = "tile-1"},
                new Product{ Id = 2, Name = "tile-2"}
            } as IEnumerable<Product>;

            _mockProductRepo.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<bool>())).ReturnsAsync(2);
            _mockProductRepo.Setup(r => r.FindAsync(
                It.IsAny<Expression<Func<Product, bool>>>(),
                filter.PageNumber,
                filter.PageSize,
                It.IsAny<string[]>(),
                It.IsAny<Expression<Func<Product, object>>>(),
                It.IsAny<DATA.Constants.Enums.OrderBy>(),
                It.IsAny<bool>()))
                .ReturnsAsync(products);

            MockMapper.Setup(m => m.Map<IEnumerable<GetProductDto>>(products))
                .Returns(products.Select(p => new GetProductDto { Id = p.Id, Name = p.Name }));

            var result = await _productService.GetProductsAsync(filter);

            result.StatusCode.Should().Be(StatusCodes.OK);
            result.Data!.Items.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(2);
            result.Data.PageNumber.Should().Be(1);
        }
        #endregion

        #region AddProductImagesAsync
        [Test]
        public async Task AddProductImagesAsync_Unauthorized_WhenAdminNull()
        {
            var result = await _productService.AddProductImagesAsync(1, new List<IFormFile>(), null);
            result.StatusCode.Should().Be(StatusCodes.Unauthorized);
        }

        [Test]
        public async Task AddProductImagesAsync_NotFound_WhenProductMissing()
        {
            _mockProductRepo.Setup(r => r.GetAsync(1)).ReturnsAsync((Product?)null);
            var result = await _productService.AddProductImagesAsync(1, new List<IFormFile>(), 1);
            result.StatusCode.Should().Be(StatusCodes.NotFound);
        }

        [Test]
        public async Task AddProductImagesAsync_BadRequest_WhenNoImages()
        {
            _mockProductRepo.Setup(r => r.GetAsync(1)).ReturnsAsync(new Product{ Id = 1, Name = "p"});
            var result = await _productService.AddProductImagesAsync(1, new List<IFormFile>(), 1);
            result.StatusCode.Should().Be(StatusCodes.BadRequest);
            result.Message.Should().Be("No images provided");
        }

        [Test]
        public async Task AddProductImagesAsync_BadRequest_WhenAllImagesInvalid()
        {
            _mockProductRepo.Setup(r => r.GetAsync(1)).ReturnsAsync(new Product{ Id = 1, Name = "p"});
            var invalid = new Mock<IFormFile>();
            invalid.SetupGet(f => f.Length).Returns(0);
            var result = await _productService.AddProductImagesAsync(1, new List<IFormFile>{ invalid.Object }, 1);
            result.StatusCode.Should().Be(StatusCodes.BadRequest);
            result.Message.Should().Be("No valid images provided");
        }

        [Test]
        public async Task AddProductImagesAsync_InternalError_WhenCommitFails()
        {
            var product = new Product{ Id = 1, Name = "p"};
            _mockProductRepo.Setup(r => r.GetAsync(1)).ReturnsAsync(product);
            _mockProductImageRepo.Setup(r => r.CountAsync(It.IsAny<Expression<Func<ProductImage, bool>>>(), It.IsAny<bool>())).ReturnsAsync(0);

            var formFile = MakeFormFile("image.jpg", 10);
            _mockFileStorage.Setup(s => s.UploadFileAsync(formFile, It.IsAny<string>())).ReturnsAsync("https://example.com/1.jpg");
            MockUnitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(0);

            var result = await _productService.AddProductImagesAsync(1, new List<IFormFile>{ formFile }, 1);
            result.StatusCode.Should().Be(StatusCodes.InternalServerError);
            result.Message.Should().Be("Failed to save product images");
        }

        [Test]
        public async Task AddProductImagesAsync_Success_UploadsAndReturnsUrls()
        {
            var product = new Product{ Id = 1, Name = "p"};
            _mockProductRepo.Setup(r => r.GetAsync(1)).ReturnsAsync(product);
            _mockProductImageRepo.Setup(r => r.CountAsync(It.IsAny<Expression<Func<ProductImage, bool>>>(), It.IsAny<bool>())).ReturnsAsync(1);

            var f1 = MakeFormFile("a.jpg", 10);
            var f2 = MakeFormFile("b.jpg", 20);

            _mockFileStorage.Setup(s => s.UploadFileAsync(f1, It.IsAny<string>())).ReturnsAsync("https://example.com/a.jpg");
            _mockFileStorage.Setup(s => s.UploadFileAsync(f2, It.IsAny<string>())).ReturnsAsync("https://example.com/b.jpg");
            MockUnitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(2);

            var result = await _productService.AddProductImagesAsync(1, new List<IFormFile>{ f1, f2 }, 1);
            result.StatusCode.Should().Be(StatusCodes.OK);
            result.Message.Should().Be("Images uploaded successfully");
            var urls = result.Data;
            urls.Should().BeEquivalentTo(new[]{"https://example.com/a.jpg","https://example.com/b.jpg"});
        }

        [Test]
        public async Task AddProductImagesAsync_InternalError_OnException()
        {
            var product = new Product{ Id = 1, Name = "p"};
            _mockProductRepo.Setup(r => r.GetAsync(1)).ReturnsAsync(product);
            _mockProductImageRepo.Setup(r => r.CountAsync(It.IsAny<Expression<Func<ProductImage, bool>>>(), It.IsAny<bool>())).ReturnsAsync(0);

            var f = MakeFormFile("a.jpg", 10);
            _mockFileStorage.Setup(s => s.UploadFileAsync(f, It.IsAny<string>())).ThrowsAsync(new Exception("boom"));

            var result = await _productService.AddProductImagesAsync(1, new List<IFormFile>{ f }, 1);
            result.StatusCode.Should().Be(StatusCodes.InternalServerError);
            result.Message.Should().Contain("boom");
        }
        #endregion

        #region DeleteProductImageAsync
        [Test]
        public async Task DeleteProductImageAsync_Unauthorized()
        {
            var result = await _productService.DeleteProductImageAsync(1, 1, null);
            result.StatusCode.Should().Be(StatusCodes.Unauthorized);
        }

        [Test]
        public async Task DeleteProductImageAsync_NotFound()
        {
            _mockProductImageRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductImage, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync((ProductImage?)null);
            var result = await _productService.DeleteProductImageAsync(1, 1, 1);
            result.StatusCode.Should().Be(StatusCodes.NotFound);
        }

        [Test]
        public async Task DeleteProductImageAsync_InternalError_WhenCommitFails()
        {
            var img = new ProductImage{ Id = 1, ProductId = 1, ImageURL = "u" };
            _mockProductImageRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductImage, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(img);
            MockUnitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(0);
            var result = await _productService.DeleteProductImageAsync(1, 1, 1);
            result.StatusCode.Should().Be(StatusCodes.InternalServerError);
        }

        [Test]
        public async Task DeleteProductImageAsync_Success_TriggersStorageDeletion()
        {
            var img = new ProductImage{ Id = 1, ProductId = 1, ImageURL = "https://example.com/x.jpg" };
            _mockProductImageRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductImage, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync(img);
            MockUnitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            var result = await _productService.DeleteProductImageAsync(1, 1, 1);
            result.StatusCode.Should().Be(StatusCodes.OK);
            _mockFileStorage.Verify(s => s.DeleteFileAsync("https://example.com/x.jpg"), Times.AtLeastOnce);
        }
        #endregion

        #region Helpers
        private static CreateProductDto ValidCreateDto()
        {
            return new CreateProductDto
            {
                QualityGrade = Data.Models.Enums.QualityGrade.A,
                Name = "Product",
                Category = Data.Models.Enums.ProductCategory.Floor,
                Type = Data.Models.Enums.ProductType.Ceramic,
                Quantity = 10,
                Size = new Size { Length = 10, Width = 10 },
                PiecesPerBox = 1,
                PricePerSqm = 100
            };
        }

        private static IFormFile MakeFormFile(string fileName, long length)
        {
            var mock = new Mock<IFormFile>();
            mock.SetupGet(f => f.FileName).Returns(fileName);
            mock.SetupGet(f => f.Length).Returns(length);
            mock.Setup(f => f.OpenReadStream()).Returns(new System.IO.MemoryStream(new byte[1]));
            mock.SetupGet(f => f.ContentType).Returns("image/jpeg");
            return mock.Object;
        }
        #endregion
    }
}
