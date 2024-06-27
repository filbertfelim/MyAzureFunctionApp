using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MyAzureFunctionApp.Controllers;
using MyAzureFunctionApp.Models;
using MyAzureFunctionApp.Models.DTOs;
using MyAzureFunctionApp.Services;
using MyAzureFunctionApp.Validators;
using Xunit;

namespace MyAzureFunctionApp.Tests
{
    public class CategoriesFunctionTests
    {
        private readonly string ValidToken = Environment.GetEnvironmentVariable("VALID_TOKEN");
        private readonly Mock<ICategoryService> _mockCategoryService;
        private readonly Mock<ILogger<CategoriesFunction>> _mockLogger;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly IValidator<CategoryDto> _validator;
        private readonly CategoriesFunction _categoriesFunction;

        public CategoriesFunctionTests()
        {
            _mockCategoryService = new Mock<ICategoryService>();
            _mockLogger = new Mock<ILogger<CategoriesFunction>>();
            _mockMapper = new Mock<IMapper>();
            _mockConfiguration = new Mock<IConfiguration>();
            _validator = new CategoryDtoValidator();

            // Setup the mock configuration to return a valid audience value
            _mockConfiguration.Setup(config => config["AzureAd:ClientId"]).Returns(Environment.GetEnvironmentVariable("AZURE_AD_CLIENT_ID"));

            _categoriesFunction = new CategoriesFunction(_mockCategoryService.Object, _validator, _mockMapper.Object, _mockConfiguration.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetCategories_ReturnsOkObjectResult_WithCategories()
        {
            // Arrange
            var categories = new List<Category>
            {
                new Category { CategoryId = 1, Name = "Category One" },
                new Category { CategoryId = 2, Name = "Category Two" }
            };

            _mockCategoryService.Setup(service => service.GetAllAsync())
                .ReturnsAsync(categories);

            var context = new DefaultHttpContext();
            var request = context.Request;

            // Add valid authorization header
            request.Headers["Authorization"] = $"Bearer {ValidToken}";

            // Act
            var result = await _categoriesFunction.GetCategories(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;

            Assert.NotNull(response);
            var messageProp = response.GetType().GetProperty("Message");
            var dataProp = response.GetType().GetProperty("Data");

            Assert.NotNull(messageProp);
            Assert.NotNull(dataProp);

            var message = messageProp.GetValue(response)?.ToString();
            var data = dataProp.GetValue(response) as IEnumerable<Category>;

            Assert.Equal("Categories retrieved successfully.", message);
            Assert.Equal(categories, data);
        }

        [Fact]
        public async Task GetCategories_ReturnsUnauthorized_WhenAuthorizationHeaderIsMissing()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;

            // Act
            var result = await _categoriesFunction.GetCategories(request);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
            var response = jsonResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Missing or invalid Authorization header", message);
        }

        [Fact]
        public async Task GetCategories_ReturnsUnauthorized_WhenTokenIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = "Bearer invalid-token";

            // Act
            var result = await _categoriesFunction.GetCategories(request);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
            var response = jsonResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Invalid token", message);
        }

        [Fact]
        public async Task GetCategoryById_ReturnsOkObjectResult_WithCategory()
        {
            // Arrange
            var category = new Category { CategoryId = 1, Name = "Category One" };

            _mockCategoryService.Setup(service => service.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(category);

            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = $"Bearer {ValidToken}";

            // Act
            var result = await _categoriesFunction.GetCategoryById(request, "1");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;

            Assert.NotNull(response);
            var messageProp = response.GetType().GetProperty("Message");
            var dataProp = response.GetType().GetProperty("Data");

            Assert.NotNull(messageProp);
            Assert.NotNull(dataProp);

            var message = messageProp.GetValue(response)?.ToString();
            var data = dataProp.GetValue(response) as Category;

            Assert.Equal("Category retrieved successfully.", message);
            Assert.Equal(category, data);
        }

        [Fact]
        public async Task GetCategoryById_ReturnsUnauthorized_WhenAuthorizationHeaderIsMissing()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;

            // Act
            var result = await _categoriesFunction.GetCategoryById(request, "1");

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
            var response = jsonResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Missing or invalid Authorization header", message);
        }

        [Fact]
        public async Task GetCategoryById_ReturnsUnauthorized_WhenTokenIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = "Bearer invalid-token";

            // Act
            var result = await _categoriesFunction.GetCategoryById(request, "1");

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
            var response = jsonResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Invalid token", message);
        }

        [Fact]
        public async Task GetCategoryById_ReturnsBadRequest_WhenIdIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = $"Bearer {ValidToken}";

            // Act
            var result = await _categoriesFunction.GetCategoryById(request, "invalid");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Invalid ID format.", message);
        }

        [Fact]
        public async Task GetCategoryById_ReturnsNotFound_WhenCategoryDoesNotExist()
        {
            // Arrange
            _mockCategoryService.Setup(service => service.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Category)null);

            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = $"Bearer {ValidToken}";

            // Act
            var result = await _categoriesFunction.GetCategoryById(request, "1");

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = notFoundResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Category with ID 1 not found.", message);
        }

        [Fact]
        public async Task CreateCategory_ReturnsUnauthorized_WhenAuthorizationHeaderIsMissing()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;

            // Act
            var result = await _categoriesFunction.CreateCategory(request);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
            var response = jsonResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Missing or invalid Authorization header", message);
        }

        [Fact]
        public async Task CreateCategory_ReturnsUnauthorized_WhenTokenIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = "Bearer invalid-token";

            // Act
            var result = await _categoriesFunction.CreateCategory(request);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
            var response = jsonResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Invalid token", message);
        }

        [Fact]
        public async Task CreateCategory_ReturnsBadRequest_WhenRequestBodyIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = $"Bearer {ValidToken}";
            var json = "invalid-json";
            var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            request.Body = memoryStream;

            // Act
            var result = await _categoriesFunction.CreateCategory(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Invalid request structure.", message);
        }

        [Fact]
        public async Task CreateCategory_ReturnsBadRequest_WhenValidationFails()
        {
            // Arrange
            var categoryDto = new CategoryDto { Name = "" };
            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = $"Bearer {ValidToken}";
            var json = JsonSerializer.Serialize(categoryDto);
            var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            request.Body = memoryStream;

            // Act
            var result = await _categoriesFunction.CreateCategory(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var errorsProp = response.GetType().GetProperty("Errors");

            Assert.NotNull(messageProp);
            Assert.NotNull(errorsProp);

            var message = messageProp.GetValue(response)?.ToString();
            var errors = errorsProp.GetValue(response) as IEnumerable<object>;

            Assert.Equal("Validation failed.", message);
            Assert.NotNull(errors);
            Assert.NotEmpty(errors);
        }

        [Fact]
        public async Task CreateCategory_ReturnsCreatedResult_WithCategory()
        {
            // Arrange
            var categoryDto = new CategoryDto { Name = "Category One" };
            var category = new Category { CategoryId = 1, Name = categoryDto.Name };

            _mockCategoryService.Setup(service => service.AddAsync(It.IsAny<CategoryDto>()))
                .ReturnsAsync((category, null));

            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = $"Bearer {ValidToken}";
            var json = JsonSerializer.Serialize(categoryDto);
            var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            request.Body = memoryStream;

            // Act
            var result = await _categoriesFunction.CreateCategory(request);

            // Assert
            var createdResult = Assert.IsType<CreatedResult>(result);
            var response = createdResult.Value;

            Assert.NotNull(response);
            var messageProp = response.GetType().GetProperty("Message");
            var dataProp = response.GetType().GetProperty("Data");

            Assert.NotNull(messageProp);
            Assert.NotNull(dataProp);

            var message = messageProp.GetValue(response)?.ToString();
            var data = dataProp.GetValue(response) as Category;

            Assert.Equal("Category created successfully.", message);
            Assert.Equal(category, data);
        }

        [Fact]
        public async Task UpdateCategory_ReturnsUnauthorized_WhenAuthorizationHeaderIsMissing()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            var id = "1";
            request.Body = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(new CategoryDto { Name = "Updated Category" }));

            // Act
            var result = await _categoriesFunction.UpdateCategory(request, id);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
            var response = jsonResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Missing or invalid Authorization header", message);
        }

        [Fact]
        public async Task UpdateCategory_ReturnsUnauthorized_WhenTokenIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            var id = "1";
            request.Headers["Authorization"] = "Bearer invalid-token";
            request.Body = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(new CategoryDto { Name = "Updated Category" }));

            // Act
            var result = await _categoriesFunction.UpdateCategory(request, id);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
            var response = jsonResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Invalid token", message);
        }

        [Fact]
        public async Task UpdateCategory_ReturnsBadRequest_WhenIdIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            var id = "invalid-id";
            request.Headers["Authorization"] = $"Bearer {ValidToken}";
            request.Body = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(new CategoryDto { Name = "Updated Category" }));

            // Act
            var result = await _categoriesFunction.UpdateCategory(request, id);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Invalid ID format.", message);
        }

        [Fact]
        public async Task UpdateCategory_ReturnsBadRequest_WhenRequestBodyIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            var id = "1";
            request.Headers["Authorization"] = $"Bearer {ValidToken}";
            request.Body = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes("Invalid body"));

            // Act
            var result = await _categoriesFunction.UpdateCategory(request, id);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Invalid request structure.", message);
        }

        [Fact]
        public async Task UpdateCategory_ReturnsBadRequest_WhenValidationFails()
        {
            // Arrange
            var categoryDto = new CategoryDto { Name = "" };
            var id = "1";
            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = $"Bearer {ValidToken}";
            var json = JsonSerializer.Serialize(categoryDto);
            var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            request.Body = memoryStream;

            // Act
            var result = await _categoriesFunction.UpdateCategory(request, id);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var errorsProp = response.GetType().GetProperty("Errors");

            Assert.NotNull(messageProp);
            Assert.NotNull(errorsProp);

            var message = messageProp.GetValue(response)?.ToString();
            var errors = errorsProp.GetValue(response) as IEnumerable<object>;

            Assert.Equal("Validation failed.", message);
            Assert.NotNull(errors);
            Assert.NotEmpty(errors);
        }

        [Fact]
        public async Task UpdateCategory_ReturnsOkObjectResult_WhenSuccessful()
        {
            // Arrange
            var categoryId = 1;
            var updatedCategory = new Category { CategoryId = categoryId, Name = "Updated Category" };

            _mockCategoryService.Setup(service => service.UpdateAsync(categoryId, It.IsAny<CategoryDto>()))
                .ReturnsAsync((updatedCategory, null));

            var context = new DefaultHttpContext();
            var request = context.Request;
            var id = categoryId.ToString();
            request.Headers["Authorization"] = $"Bearer {ValidToken}";
            request.Body = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(new CategoryDto { Name = "Updated Category" }));

            // Act
            var result = await _categoriesFunction.UpdateCategory(request, id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;

            Assert.NotNull(response);
            var messageProp = response.GetType().GetProperty("Message");
            var dataProp = response.GetType().GetProperty("Data");

            Assert.NotNull(messageProp);
            Assert.NotNull(dataProp);

            var message = messageProp.GetValue(response)?.ToString();
            var data = dataProp.GetValue(response) as Category;

            Assert.Equal("Category updated successfully.", message);
            Assert.Equal(updatedCategory, data);
        }

        [Fact]
        public async Task DeleteCategory_ReturnsUnauthorized_WhenAuthorizationHeaderIsMissing()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;

            // Act
            var result = await _categoriesFunction.DeleteCategory(request, "1");

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
            var response = jsonResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();
            Assert.Equal("Missing or invalid Authorization header", message);
        }

        [Fact]
        public async Task DeleteCategory_ReturnsUnauthorized_WhenTokenIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = "Bearer invalid-token";

            // Act
            var result = await _categoriesFunction.DeleteCategory(request, "1");

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
            var response = jsonResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Invalid token", message);
        }

        [Fact]
        public async Task DeleteCategory_ReturnsBadRequest_WhenIdIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = $"Bearer {ValidToken}";

            // Act
            var result = await _categoriesFunction.DeleteCategory(request, "invalid");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Invalid ID format.", message);
        }

        [Fact]
        public async Task DeleteCategory_ReturnsNotFound_WhenCategoryDoesNotExist()
        {
            // Arrange
            _mockCategoryService.Setup(service => service.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Category)null);

            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = $"Bearer {ValidToken}";

            // Act
            var result = await _categoriesFunction.DeleteCategory(request, "1");

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = notFoundResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Category with ID 1 not found.", message);
        }

        [Fact]
        public async Task DeleteCategory_ReturnsOkObjectResult_WithSuccessMessage()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = $"Bearer {ValidToken}";

            _mockCategoryService.Setup(service => service.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new Category { CategoryId = 1, Name = "Category One" });

            // Act
            var result = await _categoriesFunction.DeleteCategory(request, "1");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;

            Assert.NotNull(response);
            var messageProp = response.GetType().GetProperty("Message");

            Assert.NotNull(messageProp);

            var message = messageProp.GetValue(response)?.ToString();

            Assert.Equal("Category deleted successfully.", message);
        }
    }
}