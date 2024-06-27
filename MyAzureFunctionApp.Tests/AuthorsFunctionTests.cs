using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Text.Json;
using AutoMapper;
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
    public class AuthorsFunctionTests
    {
        private readonly string ValidToken = Environment.GetEnvironmentVariable("VALID_TOKEN");
        private readonly Mock<IAuthorService> _mockAuthorService;
        private readonly Mock<ILogger<AuthorsFunction>> _mockLogger;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly AuthorsFunction _authorsFunction;

        public AuthorsFunctionTests()
        {
            _mockAuthorService = new Mock<IAuthorService>();
            _mockLogger = new Mock<ILogger<AuthorsFunction>>();
            _mockMapper = new Mock<IMapper>();
            _mockConfiguration = new Mock<IConfiguration>();

            // Setup the mock configuration to return a valid audience value
            _mockConfiguration.Setup(config => config["AzureAd:ClientId"]).Returns(Environment.GetEnvironmentVariable("AZURE_AD_CLIENT_ID"));

            _authorsFunction = new AuthorsFunction(_mockAuthorService.Object, new AuthorDtoValidator(), _mockMapper.Object, _mockConfiguration.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetAuthors_ReturnsOkObjectResult_WithAuthors()
        {
            // Arrange
            var authors = new List<Author>
            {
                new Author { AuthorId = 1, Name = "Author One", Books = new List<Book>() },
                new Author { AuthorId = 2, Name = "Author Two", Books = new List<Book>() }
            };

            _mockAuthorService.Setup(service => service.GetAllAsync())
                .ReturnsAsync(authors);

            var context = new DefaultHttpContext();
            var request = context.Request;

            // Add valid authorization header
            request.Headers["Authorization"] = $"Bearer {ValidToken}";

            // Act
            var result = await _authorsFunction.GetAuthors(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;

            Assert.NotNull(response);
            var messageProp = response.GetType().GetProperty("Message");
            var dataProp = response.GetType().GetProperty("Data");

            Assert.NotNull(messageProp);
            Assert.NotNull(dataProp);

            var message = messageProp.GetValue(response)?.ToString();
            var data = dataProp.GetValue(response) as IEnumerable<Author>;

            Assert.Equal("Authors retrieved successfully.", message);
            Assert.Equal(authors, data);
        }

        [Fact]
        public async Task GetAuthors_ReturnsUnauthorized_WhenAuthorizationHeaderIsMissing()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;

            // Act
            var result = await _authorsFunction.GetAuthors(request);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
            var response = jsonResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Missing or invalid Authorization header", message);
        }

        [Fact]
        public async Task GetAuthors_ReturnsUnauthorized_WhenTokenIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;

            // Add invalid authorization header
            request.Headers["Authorization"] = "Bearer invalid-token";

            // Act
            var result = await _authorsFunction.GetAuthors(request);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
            var response = jsonResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Invalid token", message);
        }

        [Fact]
        public async Task GetAuthorById_ReturnsOkObjectResult_WithAuthor()
        {
            // Arrange
            var author = new Author { AuthorId = 1, Name = "Author One", Books = new List<Book>() };

            _mockAuthorService.Setup(service => service.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(author);

            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = $"Bearer {ValidToken}";

            // Act
            var result = await _authorsFunction.GetAuthorById(request, "1");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;

            Assert.NotNull(response);
            var messageProp = response.GetType().GetProperty("Message");
            var dataProp = response.GetType().GetProperty("Data");

            Assert.NotNull(messageProp);
            Assert.NotNull(dataProp);

            var message = messageProp.GetValue(response)?.ToString();
            var data = dataProp.GetValue(response) as Author;

            Assert.Equal("Author retrieved successfully.", message);
            Assert.Equal(author, data);
        }

        [Fact]
        public async Task GetAuthorById_ReturnsUnauthorized_WhenAuthorizationHeaderIsMissing()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;

            // Act
            var result = await _authorsFunction.GetAuthorById(request, "1");

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
            var response = jsonResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Missing or invalid Authorization header", message);
        }

        [Fact]
        public async Task GetAuthorById_ReturnsUnauthorized_WhenTokenIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = "Bearer invalid-token";

            // Act
            var result = await _authorsFunction.GetAuthorById(request, "1");

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
            var response = jsonResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Invalid token", message);
        }

        [Fact]
        public async Task GetAuthorById_ReturnsBadRequest_WhenIdIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = $"Bearer {ValidToken}";

            // Act
            var result = await _authorsFunction.GetAuthorById(request, "invalid");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Invalid ID format, ID should be a number.", message);
        }

        [Fact]
        public async Task GetAuthorById_ReturnsNotFound_WhenAuthorDoesNotExist()
        {
            // Arrange
            _mockAuthorService.Setup(service => service.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Author)null);

            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = $"Bearer {ValidToken}";

            // Act
            var result = await _authorsFunction.GetAuthorById(request, "1");

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = notFoundResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Author with ID 1 not found.", message);
        }

        [Fact]
        public async Task CreateAuthor_ReturnsUnauthorized_WhenAuthorizationHeaderIsMissing()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;

            // Act
            var result = await _authorsFunction.CreateAuthor(request);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
            var response = jsonResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Missing or invalid Authorization header", message);
        }

        [Fact]
        public async Task CreateAuthor_ReturnsUnauthorized_WhenTokenIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = "Bearer invalid-token";

            // Act
            var result = await _authorsFunction.CreateAuthor(request);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
            var response = jsonResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Invalid token", message);
        }

        [Fact]
        public async Task CreateAuthor_ReturnsBadRequest_WhenRequestBodyIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = $"Bearer {ValidToken}";
            var json = "invalid-json";
            var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            request.Body = memoryStream;

            // Act
            var result = await _authorsFunction.CreateAuthor(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Invalid request structure.", message);
        }

        [Fact]
        public async Task CreateAuthor_ReturnsBadRequest_WhenValidationFails()
        {
            // Arrange
            var authorDto = new AuthorDto { Name = "" };
            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = $"Bearer {ValidToken}";
            var json = JsonSerializer.Serialize(authorDto);
            var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            request.Body = memoryStream;

            // Act
            var result = await _authorsFunction.CreateAuthor(request);

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
        public async Task CreateAuthor_ReturnsCreatedResult_WithAuthor()
        {
            // Arrange
            var authorDto = new AuthorDto { Name = "Author One" };
            var author = new Author { AuthorId = 1, Name = authorDto.Name, Books = new List<Book>() };

            _mockAuthorService.Setup(service => service.AddAsync(It.IsAny<AuthorDto>()))
                .ReturnsAsync(author);

            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = $"Bearer {ValidToken}";
            var json = JsonSerializer.Serialize(authorDto);
            var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            request.Body = memoryStream;

            // Act
            var result = await _authorsFunction.CreateAuthor(request);

            // Assert
            var createdResult = Assert.IsType<CreatedResult>(result);
            var response = createdResult.Value;

            Assert.NotNull(response);
            var messageProp = response.GetType().GetProperty("Message");
            var dataProp = response.GetType().GetProperty("Data");

            Assert.NotNull(messageProp);
            Assert.NotNull(dataProp);

            var message = messageProp.GetValue(response)?.ToString();
            var data = dataProp.GetValue(response) as Author;

            Assert.Equal("Author created successfully.", message);
            Assert.Equal(author, data);
        }

        [Fact]
        public async Task UpdateAuthor_ReturnsUnauthorized_WhenAuthorizationHeaderIsMissing()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            var id = "1";
            request.Body = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(new AuthorDto { Name = "Updated Author" }));

            // Act
            var result = await _authorsFunction.UpdateAuthor(request, id);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
            var response = jsonResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Missing or invalid Authorization header", message);
        }

        [Fact]
        public async Task UpdateAuthor_ReturnsUnauthorized_WhenTokenIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            var id = "1";
            request.Headers["Authorization"] = "Bearer invalid-token";
            request.Body = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(new AuthorDto { Name = "Updated Author" }));

            // Act
            var result = await _authorsFunction.UpdateAuthor(request, id);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
            var response = jsonResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Invalid token", message);
        }

        [Fact]
        public async Task UpdateAuthor_ReturnsBadRequest_WhenIdIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            var id = "invalid-id";
            request.Headers["Authorization"] = $"Bearer {ValidToken}";
            request.Body = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(new AuthorDto { Name = "Updated Author" }));

            // Act
            var result = await _authorsFunction.UpdateAuthor(request, id);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Invalid ID format, ID should be a number.", message);
        }

        [Fact]
        public async Task UpdateAuthor_ReturnsBadRequest_WhenRequestBodyIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            var id = "1";
            request.Headers["Authorization"] = $"Bearer {ValidToken}";
            request.Body = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes("Invalid body"));

            // Act
            var result = await _authorsFunction.UpdateAuthor(request, id);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Invalid request structure.", message);
        }

        [Fact]
        public async Task UpdateAuthor_ReturnsNotFound_WhenAuthorDoesNotExist()
        {
            // Arrange
            var authorId = 1;
            var authorDto = new AuthorDto { Name = "Updated Author" };

            _mockAuthorService.Setup(service => service.UpdateAsync(authorId, It.IsAny<AuthorDto>()))
                .ReturnsAsync((Author)null);

            var context = new DefaultHttpContext();
            var request = context.Request;
            var id = authorId.ToString();
            request.Headers["Authorization"] = $"Bearer {ValidToken}";
            var json = JsonSerializer.Serialize(authorDto);
            var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            request.Body = memoryStream;

            // Act
            var result = await _authorsFunction.UpdateAuthor(request, id);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = notFoundResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal($"Author with ID {authorId} not found.", message);
        }
        public async Task UpdateAuthor_ReturnsBadRequest_WhenValidationFails()
        {
            // Arrange
            var authorDto = new AuthorDto { Name = "" };
            var id = "1";
            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = $"Bearer {ValidToken}";
            var json = JsonSerializer.Serialize(authorDto);
            var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            request.Body = memoryStream;

            // Act
            var result = await _authorsFunction.UpdateAuthor(request, id);

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
        public async Task UpdateAuthor_ReturnsOkObjectResult_WhenSuccessful()
        {
            // Arrange
            var authorId = 1;
            var updatedAuthor = new Author { AuthorId = authorId, Name = "Updated Author" };

            _mockAuthorService.Setup(service => service.UpdateAsync(authorId, It.IsAny<AuthorDto>()))
                .ReturnsAsync(updatedAuthor);

            var context = new DefaultHttpContext();
            var request = context.Request;
            var id = authorId.ToString();
            request.Headers["Authorization"] = $"Bearer {ValidToken}";
            request.Body = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(new AuthorDto { Name = "Updated Author" }));

            // Act
            var result = await _authorsFunction.UpdateAuthor(request, id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;

            Assert.NotNull(response);
            var messageProp = response.GetType().GetProperty("Message");
            var dataProp = response.GetType().GetProperty("Data");

            Assert.NotNull(messageProp);
            Assert.NotNull(dataProp);

            var message = messageProp.GetValue(response)?.ToString();
            var data = dataProp.GetValue(response) as Author;

            Assert.Equal("Author updated successfully.", message);
            Assert.Equal(updatedAuthor, data);
        }

        [Fact]
        public async Task DeleteAuthor_ReturnsUnauthorized_WhenAuthorizationHeaderIsMissing()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;

            // Act
            var result = await _authorsFunction.DeleteAuthor(request, "1");

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
            var response = jsonResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Missing or invalid Authorization header", message);
        }

        [Fact]
        public async Task DeleteAuthor_ReturnsUnauthorized_WhenTokenIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = "Bearer invalid-token";

            // Act
            var result = await _authorsFunction.DeleteAuthor(request, "1");

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
            var response = jsonResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Invalid token", message);
        }

        [Fact]
        public async Task DeleteAuthor_ReturnsBadRequest_WhenIdIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = $"Bearer {ValidToken}";

            // Act
            var result = await _authorsFunction.DeleteAuthor(request, "invalid-id");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Invalid ID format, ID should be a number.", message);
        }

        [Fact]
        public async Task DeleteAuthor_ReturnsNotFound_WhenAuthorDoesNotExist()
        {
            // Arrange
            _mockAuthorService.Setup(service => service.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Author)null);

            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = $"Bearer {ValidToken}";

            // Act
            var result = await _authorsFunction.DeleteAuthor(request, "1");

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = notFoundResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Author with ID 1 not found.", message);
        }

        [Fact]
        public async Task DeleteAuthor_ReturnsOkObjectResult_WithSuccessMessage()
        {
            // Arrange
            _mockAuthorService.Setup(service => service.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new Author { AuthorId = 1, Name = "Author One" });

            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = $"Bearer {ValidToken}";

            // Act
            var result = await _authorsFunction.DeleteAuthor(request, "1");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;

            Assert.NotNull(response);
            var messageProp = response.GetType().GetProperty("Message");

            Assert.NotNull(messageProp);

            var message = messageProp.GetValue(response)?.ToString();

            Assert.Equal("Author deleted successfully.", message);
        }
    }
}