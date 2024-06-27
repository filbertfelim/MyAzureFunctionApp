using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
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
    public class BooksFunctionTests
    {
        private readonly string ValidToken = Environment.GetEnvironmentVariable("VALID_TOKEN");
        private readonly Mock<IBookService> _mockBookService;
        private readonly Mock<ILogger<BooksFunction>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly BooksFunction _booksFunction;

        public BooksFunctionTests()
        {
            _mockBookService = new Mock<IBookService>();
            _mockLogger = new Mock<ILogger<BooksFunction>>();
            _mockConfiguration = new Mock<IConfiguration>();

            _mockConfiguration.Setup(config => config["AzureAd:ClientId"]).Returns(Environment.GetEnvironmentVariable("AZURE_AD_CLIENT_ID"));

            _booksFunction = new BooksFunction(_mockBookService.Object, new BookDtoValidator(), _mockConfiguration.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetBooks_ReturnsOkObjectResult_WithBooks()
        {
            // Arrange
            var books = new List<Book>
            {
                new Book { BookId = 1, Title = "Book One" },
                new Book { BookId = 2, Title = "Book Two" }
            };

            _mockBookService.Setup(service => service.GetAllAsync())
                .ReturnsAsync(books);

            var context = new DefaultHttpContext();
            var request = context.Request;

            // Add valid authorization header
            request.Headers["Authorization"] = $"Bearer {ValidToken}";

            // Act
            var result = await _booksFunction.GetBooks(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;

            Assert.NotNull(response);
            var messageProp = response.GetType().GetProperty("Message");
            var dataProp = response.GetType().GetProperty("Data");

            Assert.NotNull(messageProp);
            Assert.NotNull(dataProp);

            var message = messageProp.GetValue(response)?.ToString();
            var data = dataProp.GetValue(response);

            Assert.Equal("Books retrieved successfully.", message);
            Assert.Equal(books, data);
        }

        [Fact]
        public async Task GetBooks_ReturnsUnauthorized_WhenAuthorizationHeaderIsMissing()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;

            // Act
            var result = await _booksFunction.GetBooks(request);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
            var response = jsonResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Missing or invalid Authorization header", message);
        }

        [Fact]
        public async Task GetBooks_ReturnsUnauthorized_WhenTokenIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;

            // Add invalid authorization header
            request.Headers["Authorization"] = "Bearer invalid-token";

            // Act
            var result = await _booksFunction.GetBooks(request);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
            var response = jsonResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Invalid token", message);
        }

        [Fact]
        public async Task GetBookById_ReturnsOkObjectResult_WithBook()
        {
            // Arrange
            var book = new Book { BookId = 1, Title = "Book One" };

            _mockBookService.Setup(service => service.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(book);

            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = $"Bearer {ValidToken}";

            // Act
            var result = await _booksFunction.GetBookById(request, "1");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;

            Assert.NotNull(response);
            var messageProp = response.GetType().GetProperty("Message");
            var dataProp = response.GetType().GetProperty("Data");

            Assert.NotNull(messageProp);
            Assert.NotNull(dataProp);

            var message = messageProp.GetValue(response)?.ToString();
            var data = dataProp.GetValue(response) as Book;

            Assert.Equal("Book retrieved successfully.", message);
            Assert.Equal(book, data);
        }

        [Fact]
        public async Task GetBookById_ReturnsUnauthorized_WhenAuthorizationHeaderIsMissing()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;

            // Act
            var result = await _booksFunction.GetBookById(request, "1");

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
            var response = jsonResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Missing or invalid Authorization header", message);
        }

        [Fact]
        public async Task GetBookById_ReturnsUnauthorized_WhenTokenIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = "Bearer invalid-token";

            // Act
            var result = await _booksFunction.GetBookById(request, "1");

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
            var response = jsonResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Invalid token", message);
        }

        [Fact]
        public async Task GetBookById_ReturnsBadRequest_WhenIdIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = $"Bearer {ValidToken}";

            // Act
            var result = await _booksFunction.GetBookById(request, "invalid");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Invalid ID format.", message);
        }

        [Fact]
        public async Task GetBookById_ReturnsNotFound_WhenBookDoesNotExist()
        {
            // Arrange
            _mockBookService.Setup(service => service.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Book)null);

            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = $"Bearer {ValidToken}";

            // Act
            var result = await _booksFunction.GetBookById(request, "1");

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = notFoundResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Book with ID 1 not found.", message);
        }

        [Fact]
        public async Task CreateBook_ReturnsUnauthorized_WhenAuthorizationHeaderIsMissing()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;

            // Act
            var result = await _booksFunction.CreateBook(request);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
            var response = jsonResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Missing or invalid Authorization header", message);
        }

        [Fact]
        public async Task CreateBook_ReturnsUnauthorized_WhenTokenIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = "Bearer invalid-token";

            // Act
            var result = await _booksFunction.CreateBook(request);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
            var response = jsonResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Invalid token", message);
        }

        [Fact]
        public async Task CreateBook_ReturnsBadRequest_WhenRequestBodyIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = $"Bearer {ValidToken}";
            var json = "invalid-json";
            var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            request.Body = memoryStream;

            // Act
            var result = await _booksFunction.CreateBook(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Invalid request structure.", message);
        }

        [Fact]
        public async Task CreateBook_ReturnsBadRequest_WhenValidationFails()
        {
            // Arrange
            var bookDto = new BookDto { Title = "", AuthorId = 0, CategoryIds = new List<int>() };
            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = $"Bearer {ValidToken}";
            var json = JsonSerializer.Serialize(bookDto);
            var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            request.Body = memoryStream;

            // Act
            var result = await _booksFunction.CreateBook(request);

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
        public async Task CreateBook_ReturnsCreatedResult_WithBook()
        {
            // Arrange
            var bookDto = new BookDto { Title = "Book One", AuthorId = 1, CategoryIds = new List<int> { 1, 2 } };
            var book = new Book { BookId = 1, Title = bookDto.Title };

            _mockBookService.Setup(service => service.AddAsync(It.IsAny<BookDto>()))
                .ReturnsAsync((book, null));

            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = $"Bearer {ValidToken}";
            var json = JsonSerializer.Serialize(bookDto);
            var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            request.Body = memoryStream;

            // Act
            var result = await _booksFunction.CreateBook(request);

            // Assert
            var createdResult = Assert.IsType<CreatedResult>(result);
            var response = createdResult.Value;

            Assert.NotNull(response);
            var messageProp = response.GetType().GetProperty("Message");
            var dataProp = response.GetType().GetProperty("Data");

            Assert.NotNull(messageProp);
            Assert.NotNull(dataProp);

            var message = messageProp.GetValue(response)?.ToString();
            var data = dataProp.GetValue(response) as Book;

            Assert.Equal("Book created successfully.", message);
            Assert.Equal(book, data);
        }

        [Fact]
        public async Task UpdateBook_ReturnsUnauthorized_WhenAuthorizationHeaderIsMissing()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            var id = "1";
            request.Body = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(new BookDto { Title = "Updated Book", AuthorId = 1, CategoryIds = new List<int> { 1, 2 } }));

            // Act
            var result = await _booksFunction.UpdateBook(request, id);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
            var response = jsonResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Missing or invalid Authorization header", message);
        }

        [Fact]
        public async Task UpdateBook_ReturnsUnauthorized_WhenTokenIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            var id = "1";
            request.Headers["Authorization"] = "Bearer invalid-token";
            request.Body = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(new BookDto { Title = "Updated Book", AuthorId = 1, CategoryIds = new List<int> { 1, 2 } }));

            // Act
            var result = await _booksFunction.UpdateBook(request, id);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
            var response = jsonResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Invalid token", message);
        }

        [Fact]
        public async Task UpdateBook_ReturnsBadRequest_WhenIdIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            var id = "invalid-id";
            request.Headers["Authorization"] = $"Bearer {ValidToken}";
            request.Body = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(new BookDto { Title = "Updated Book", AuthorId = 1, CategoryIds = new List<int> { 1, 2 } }));

            // Act
            var result = await _booksFunction.UpdateBook(request, id);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Invalid ID format.", message);
        }

        [Fact]
        public async Task UpdateBook_ReturnsBadRequest_WhenRequestBodyIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            var id = "1";
            request.Headers["Authorization"] = $"Bearer {ValidToken}";
            request.Body = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes("Invalid body"));

            // Act
            var result = await _booksFunction.UpdateBook(request, id);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Invalid request structure.", message);
        }

        [Fact]
        public async Task UpdateBook_ReturnsBadRequest_WhenValidationFails()
        {
            // Arrange
            var bookDto = new BookDto { Title = "", AuthorId = 0, CategoryIds = new List<int>() };
            var context = new DefaultHttpContext();
            var request = context.Request;
            var id = "1";
            request.Headers["Authorization"] = $"Bearer {ValidToken}";
            var json = JsonSerializer.Serialize(bookDto);
            var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            request.Body = memoryStream;

            // Act
            var result = await _booksFunction.UpdateBook(request, id);

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
        public async Task UpdateBook_ReturnsBadRequest_WhenBookNotFound()
        {
            // Arrange
            var bookDto = new BookDto { Title = "Updated Book", AuthorId = 1, CategoryIds = new List<int> { 1, 2 } };
            var context = new DefaultHttpContext();
            var request = context.Request;
            var id = "1";
            request.Headers["Authorization"] = $"Bearer {ValidToken}";
            var json = JsonSerializer.Serialize(bookDto);
            var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            request.Body = memoryStream;

            _mockBookService.Setup(service => service.UpdateAsync(It.IsAny<int>(), It.IsAny<BookDto>()))
                .ReturnsAsync((null, "Book not found."));

            // Act
            var result = await _booksFunction.UpdateBook(request, id);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Book not found.", message);
        }

        [Fact]
        public async Task UpdateBook_ReturnsOkObjectResult_WhenSuccessful()
        {
            // Arrange
            var bookId = 1;
            var updatedBook = new Book { BookId = bookId, Title = "Updated Book" };

            _mockBookService.Setup(service => service.UpdateAsync(bookId, It.IsAny<BookDto>()))
                .ReturnsAsync((updatedBook, null));
            var context = new DefaultHttpContext();
            var request = context.Request;
            var id = bookId.ToString();
            request.Headers["Authorization"] = $"Bearer {ValidToken}";
            request.Body = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(new BookDto { Title = "Updated Book", AuthorId = 1, CategoryIds = new List<int> { 1, 2 } }));

            // Act
            var result = await _booksFunction.UpdateBook(request, id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;

            Assert.NotNull(response);
            var messageProp = response.GetType().GetProperty("Message");
            var dataProp = response.GetType().GetProperty("Data");

            Assert.NotNull(messageProp);
            Assert.NotNull(dataProp);

            var message = messageProp.GetValue(response)?.ToString();
            var data = dataProp.GetValue(response) as Book;

            Assert.Equal("Book updated successfully.", message);
            Assert.Equal(updatedBook, data);
        }

        [Fact]
        public async Task DeleteBook_ReturnsUnauthorized_WhenAuthorizationHeaderIsMissing()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            var id = "1";

            // Act
            var result = await _booksFunction.DeleteBook(request, id);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
            var response = jsonResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Missing or invalid Authorization header", message);
        }

        [Fact]
        public async Task DeleteBook_ReturnsUnauthorized_WhenTokenIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            var id = "1";
            request.Headers["Authorization"] = "Bearer invalid-token";

            // Act
            var result = await _booksFunction.DeleteBook(request, id);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
            var response = jsonResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Invalid token", message);
        }

        [Fact]
        public async Task DeleteBook_ReturnsBadRequest_WhenIdIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            var id = "invalid-id";
            request.Headers["Authorization"] = $"Bearer {ValidToken}";

            // Act
            var result = await _booksFunction.DeleteBook(request, id);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Invalid ID format.", message);
        }

        [Fact]
        public async Task DeleteBook_ReturnsNotFound_WhenBookDoesNotExist()
        {
            // Arrange
            _mockBookService.Setup(service => service.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Book)null);

            var context = new DefaultHttpContext();
            var request = context.Request;
            var id = "1";
            request.Headers["Authorization"] = $"Bearer {ValidToken}";

            // Act
            var result = await _booksFunction.DeleteBook(request, id);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = notFoundResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Book with ID 1 not found.", message);
        }

        [Fact]
        public async Task DeleteBook_ReturnsOkObjectResult_WhenSuccessful()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            var id = "1";
            request.Headers["Authorization"] = $"Bearer {ValidToken}";

            _mockBookService.Setup(service => service.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new Book { BookId = 1, Title = "Book One" });

            // Act
            var result = await _booksFunction.DeleteBook(request, id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;

            Assert.NotNull(response);
            var messageProp = response.GetType().GetProperty("Message");

            Assert.NotNull(messageProp);

            var message = messageProp.GetValue(response)?.ToString();

            Assert.Equal("Book deleted successfully.", message);
        }

        [Fact]
        public async Task UploadBookImage_ReturnsUnauthorized_WhenAuthorizationHeaderIsMissing()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;

            // Act
            var result = await _booksFunction.UploadBookImage(request, "1");

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
            var response = jsonResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Missing or invalid Authorization header", message);
        }

        [Fact]
        public async Task UploadBookImage_ReturnsUnauthorized_WhenTokenIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = "Bearer invalid-token";

            // Act
            var result = await _booksFunction.UploadBookImage(request, "1");

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
            var response = jsonResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Invalid token", message);
        }

        [Fact]
        public async Task UploadBookImage_ReturnsBadRequest_WhenIdIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = $"Bearer {ValidToken}";
            request.ContentType = "multipart/form-data";

            var file = new FormFile(new MemoryStream(new byte[1024 * 1024]), 0, 1024 * 1024, "file", "test.jpg");
            var formFileCollection = new FormFileCollection { file };
            var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(), formFileCollection);
            request.Form = formCollection;

            // Act
            var result = await _booksFunction.UploadBookImage(request, "invalid");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Invalid ID format.", message);
        }

        [Fact]
        public async Task UploadBookImage_ReturnsNotFound_WhenBookDoesNotExist()
        {
            // Arrange
            _mockBookService.Setup(service => service.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Book)null);

            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = $"Bearer {ValidToken}";
            request.ContentType = "multipart/form-data";

            var file = new FormFile(new MemoryStream(new byte[1024 * 1024]), 0, 1024 * 1024, "file", "test.jpg");
            var formFileCollection = new FormFileCollection { file };
            var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(), formFileCollection);
            request.Form = formCollection;

            // Act
            var result = await _booksFunction.UploadBookImage(request, "1");

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = notFoundResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Book with ID 1 not found.", message);
        }

        [Fact]
        public async Task UploadBookImage_ReturnsBadRequest_WhenWrongContentType()
        {
            // Arrange
            _mockBookService.Setup(service => service.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new Book { BookId = 1, Title = "Book One" });

            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = $"Bearer {ValidToken}";

            // Act
            var result = await _booksFunction.UploadBookImage(request, "1");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("Content-Type must be multipart/form-data.", message);
        }

        [Fact]
        public async Task UploadBookImage_ReturnsBadRequest_WhenFileSizeExceedsLimit()
        {
            // Arrange
            _mockBookService.Setup(service => service.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new Book { BookId = 1, Title = "Book One" });

            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = $"Bearer {ValidToken}";
            request.ContentType = "multipart/form-data";

            var file = new FormFile(new MemoryStream(new byte[3 * 1024 * 1024]), 0, 3 * 1024 * 1024, "file", "test.jpg");
            var formFileCollection = new FormFileCollection { file };
            var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(), formFileCollection);
            request.Form = formCollection;

            // Act
            var result = await _booksFunction.UploadBookImage(request, "1");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;

            var messageProp = response.GetType().GetProperty("Message");
            var message = messageProp?.GetValue(response)?.ToString();

            Assert.Equal("File size exceeds 2 MB", message);
        }

        [Fact]
        public async Task UploadBookImage_ReturnsOkObjectResult_WithFilePath()
        {
            // Arrange
            _mockBookService.Setup(service => service.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new Book { BookId = 1, Title = "Book One" });

            _mockBookService.Setup(service => service.UploadBookImageAsync(It.IsAny<int>(), It.IsAny<IFormFile>()))
                .ReturnsAsync("/1.jpg");

            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Headers["Authorization"] = $"Bearer {ValidToken}";
            request.ContentType = "multipart/form-data";

            var file = new FormFile(new MemoryStream(new byte[1024 * 1024]), 0, 1024 * 1024, "file", "test.jpg");
            var formFileCollection = new FormFileCollection { file };
            var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(), formFileCollection);
            request.Form = formCollection;

            // Act
            var result = await _booksFunction.UploadBookImage(request, "1");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;

            Assert.NotNull(response);
            var filePathProp = response.GetType().GetProperty("FilePath");

            Assert.NotNull(filePathProp);

            var filePath = filePathProp.GetValue(response)?.ToString();

            Assert.Equal("/1.jpg", filePath);
        }
    }
}