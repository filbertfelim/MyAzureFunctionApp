using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MyAzureFunctionApp.Models.DTOs;
using MyAzureFunctionApp.Services;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Azure.Functions.Worker;
using System.Text.Json;
using MyAzureFunctionApp.Validators;
using Microsoft.Extensions.Configuration;
using MyAzureFunctionApp.Helpers;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;

namespace MyAzureFunctionApp.Controllers
{
    public class BooksFunction : AuthenticatedFunctionBase
    {
        private readonly IBookService _bookService;
        private readonly IValidator<BookDto> _validator;
        private readonly ILogger<BooksFunction> _logger;

        public BooksFunction(IBookService bookService, IValidator<BookDto> validator, IConfiguration configuration, ILogger<BooksFunction> logger) : base(configuration, logger)
        {
            _bookService = bookService;
            _validator = validator;
            _logger = logger;
        }


        [OpenApiOperation(operationId: "GetBooks", tags: new[] { "Books" })]
        [OpenApiSecurity("Bearer", SecuritySchemeType.ApiKey, Scheme =  OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT", In = OpenApiSecurityLocationType.Header, Name = "Authorization")]
        [Function("GetBooks")]
        public async Task<IActionResult> GetBooks(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "books")] HttpRequest req)
        {
            _logger.LogInformation("GetBooks: Processing request to get all books.");

            var validationResult = ValidateToken(req);
            if (validationResult != null)
            {
                _logger.LogWarning("GetBooks: Authorization failed: {Message}", ((JsonResult)validationResult).Value);
                return validationResult;
            }

            var books = await _bookService.GetAllAsync();
            _logger.LogInformation("GetBooks: Successfully retrieved books.");

            return new OkObjectResult(new { Message = "Books retrieved successfully.", Data = books });
        }

        [OpenApiOperation(operationId: "GetBookById", tags: new[] { "Books" })]
        [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(int), Description = "The ID of the book")]
        [OpenApiSecurity("Bearer", SecuritySchemeType.ApiKey, Scheme =  OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT", In = OpenApiSecurityLocationType.Header, Name = "Authorization")]
        [Function("GetBookById")]
        public async Task<IActionResult> GetBookById(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "books/{id}")] HttpRequest req, string id)
        {
            _logger.LogInformation("GetBookById: Processing request to get book by ID: {Id}", id);

            var validationResult = ValidateToken(req);
            if (validationResult != null)
            {
                _logger.LogWarning("GetBookById: Authorization failed: {Message}", ((JsonResult)validationResult).Value);
                return validationResult;
            }

            if (!int.TryParse(id, out int bookId) || bookId <= 0)
            {
                _logger.LogWarning("GetBookById: Invalid ID format: {Id}", id);
                return new BadRequestObjectResult(new { Message = "Invalid ID format." });
            }

            var book = await _bookService.GetByIdAsync(bookId);
            if (book == null)
            {
                _logger.LogWarning("GetBookById: Book not found: {Id}", bookId);
                return new NotFoundObjectResult(new { Message = $"Book with ID {bookId} not found." });
            }

            _logger.LogInformation("GetBookById: Successfully retrieved book with ID: {Id}", bookId);
            return new OkObjectResult(new { Message = "Book retrieved successfully.", Data = book });
        }

        [OpenApiOperation(operationId: "CreateBook", tags: new[] { "Books" })]
        [OpenApiRequestBody("application/json", typeof(BookDto), Description = "The book data to create")]
        [OpenApiSecurity("Bearer", SecuritySchemeType.ApiKey, Scheme =  OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT", In = OpenApiSecurityLocationType.Header, Name = "Authorization")]
        [Function("CreateBook")]
        public async Task<IActionResult> CreateBook(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "books")] HttpRequest req)
        {
            _logger.LogInformation("CreateBook: Processing request to create a new book.");

            var validationResult = ValidateToken(req);
            if (validationResult != null)
            {
                _logger.LogWarning("CreateBook: Authorization failed: {Message}", ((JsonResult)validationResult).Value);
                return validationResult;
            }

            if (req.Body == null)
            {
                _logger.LogWarning("CreateBook: Request body is null.");
                return new BadRequestObjectResult(new { Message = "Request body cannot be null." });
            }

            BookDto data;
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                if (!DtoJsonValidator.IsValidJsonStructure<BookDto>(requestBody))
                {
                    _logger.LogWarning("CreateBook: Invalid request structure.");
                    return new BadRequestObjectResult(new { Message = "Invalid request structure." });
                }

                data = JsonSerializer.Deserialize<BookDto>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (data == null)
                {
                    _logger.LogWarning("CreateBook: Invalid request body.");
                    return new BadRequestObjectResult(new { Message = "Invalid request body." });
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "CreateBook: Failed to deserialize request body.");
                return new BadRequestObjectResult(new { Message = "Invalid request structure." });
            }

            ValidationResult validResult = await _validator.ValidateAsync(data);
            if (!validResult.IsValid)
            {
                var errors = validResult.Errors.Select(x => new { x.PropertyName, x.ErrorMessage });
                _logger.LogWarning("CreateBook: Validation failed: {Errors}", errors);
                return new BadRequestObjectResult(new { Message = "Validation failed.", Errors = errors });
            }

            data.Title = data.Title.Trim();

            var (createdBook, errorMessage) = await _bookService.AddAsync(data);
            if (createdBook == null)
            {
                _logger.LogWarning("CreateBook: Failed to create book: {Message}", errorMessage);
                return new BadRequestObjectResult(new { Message = errorMessage });
            }

            _logger.LogInformation("CreateBook: Successfully created book with ID: {Id}", createdBook.BookId);
            return new CreatedResult($"/books/{createdBook.BookId}", new { Message = "Book created successfully.", Data = createdBook });
        }

        [OpenApiOperation(operationId: "UpdateBook", tags: new[] { "Books" })]
        [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(int), Description = "The ID of the book to update")]
        [OpenApiRequestBody("application/json", typeof(BookDto), Description = "The book data to update")]
        [OpenApiSecurity("Bearer", SecuritySchemeType.ApiKey, Scheme =  OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT", In = OpenApiSecurityLocationType.Header, Name = "Authorization")]
        [Function("UpdateBook")]
        public async Task<IActionResult> UpdateBook(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "books/{id}")] HttpRequest req, string id)
        {
            _logger.LogInformation("UpdateBook: Processing request to update book with ID: {Id}", id);

            var validationResult = ValidateToken(req);
            if (validationResult != null)
            {
                _logger.LogWarning("UpdateBook: Authorization failed: {Message}", ((JsonResult)validationResult).Value);
                return validationResult;
            }

            if (!int.TryParse(id, out int bookId) || bookId <= 0)
            {
                _logger.LogWarning("UpdateBook: Invalid ID format: {Id}", id);
                return new BadRequestObjectResult(new { Message = "Invalid ID format." });
            }

            if (req.Body == null)
            {
                _logger.LogWarning("UpdateBook: Request body is null.");
                return new BadRequestObjectResult(new { Message = "Request body cannot be null." });
            }

            BookDto data;
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                if (!DtoJsonValidator.IsValidJsonStructure<BookDto>(requestBody))
                {
                    _logger.LogWarning("UpdateBook: Invalid request structure.");
                    return new BadRequestObjectResult(new { Message = "Invalid request structure." });
                }

                data = JsonSerializer.Deserialize<BookDto>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (data == null)
                {
                    _logger.LogWarning("UpdateBook: Invalid request body.");
                    return new BadRequestObjectResult(new { Message = "Invalid request body." });
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "UpdateBook: Failed to deserialize request body.");
                return new BadRequestObjectResult(new { Message = "Invalid request structure." });
            }

            ValidationResult validResult = await _validator.ValidateAsync(data);
            if (!validResult.IsValid)
            {
                var errors = validResult.Errors.Select(x => new { x.PropertyName, x.ErrorMessage });
                _logger.LogWarning("UpdateBook: Validation failed: {Errors}", errors);
                return new BadRequestObjectResult(new { Message = "Validation failed.", Errors = errors });
            }

            data.Title = data.Title.Trim();

            var (updatedBook, errorMessage) = await _bookService.UpdateAsync(bookId, data);
            if (updatedBook == null)
            {
                _logger.LogWarning("UpdateBook: Failed to update book: {Message}", errorMessage);
                return new BadRequestObjectResult(new { Message = errorMessage });
            }

            _logger.LogInformation("UpdateBook: Successfully updated book with ID: {Id}", updatedBook.BookId);
            return new OkObjectResult(new { Message = "Book updated successfully.", Data = updatedBook });
        }

        [OpenApiOperation(operationId: "DeleteBook", tags: new[] { "Books" })]
        [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(int), Description = "The ID of the book to delete")]
        [OpenApiSecurity("Bearer", SecuritySchemeType.ApiKey, Scheme =  OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT", In = OpenApiSecurityLocationType.Header, Name = "Authorization")]
        [Function("DeleteBook")]
        public async Task<IActionResult> DeleteBook(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "books/{id}")] HttpRequest req, string id)
        {
            _logger.LogInformation("DeleteBook: Processing request to delete book with ID: {Id}", id);

            var validationResult = ValidateToken(req);
            if (validationResult != null)
            {
                _logger.LogWarning("DeleteBook: Authorization failed: {Message}", ((JsonResult)validationResult).Value);
                return validationResult;
            }

            if (!int.TryParse(id, out int bookId) || bookId <= 0)
            {
                _logger.LogWarning("DeleteBook: Invalid ID format: {Id}", id);
                return new BadRequestObjectResult(new { Message = "Invalid ID format." });
            }

            var book = await _bookService.GetByIdAsync(bookId);
            if (book == null)
            {
                _logger.LogWarning("DeleteBook: Book not found: {Id}", bookId);
                return new NotFoundObjectResult(new { Message = $"Book with ID {bookId} not found." });
            }

            await _bookService.DeleteAsync(bookId);

            _logger.LogInformation("DeleteBook: Successfully deleted book with ID: {Id}", bookId);
            return new OkObjectResult(new { Message = "Book deleted successfully." });
        }
        
        public class MultiPartFormDataModel
        {
            public byte[] FileUpload { get; set; }
        }

        [OpenApiOperation(operationId: "UploadBookImage", tags: new[] { "Books" })]
        [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(int), Description = "The ID of the book")]
        [OpenApiRequestBody(contentType: "multipart/form-data", bodyType: typeof(MultiPartFormDataModel), Required = true, Description = "The image file to upload")]
        [OpenApiSecurity("Bearer", SecuritySchemeType.ApiKey, Scheme =  OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT", In = OpenApiSecurityLocationType.Header, Name = "Authorization")]
        [Function("UploadBookImage")]
        public async Task<IActionResult> UploadBookImage(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "books/{id}/uploadImage")] HttpRequest req, string id)
        {
            _logger.LogInformation("UploadBookImage: Processing request to upload book image with ID: {Id}", id);

            var validationResult = ValidateToken(req);
            if (validationResult != null)
            {
                _logger.LogWarning("UploadBookImage: Authorization failed: {Message}", ((JsonResult)validationResult).Value);
                return validationResult;
            }

            if (!int.TryParse(id, out int bookId) || bookId <= 0)
            {
                _logger.LogWarning("UploadBookImage: Invalid ID format: {Id}", id);
                return new BadRequestObjectResult(new { Message = "Invalid ID format." });
            }

            var book = await _bookService.GetByIdAsync(bookId);
            if (book == null)
            {
                _logger.LogWarning("UploadBookImage: Book not found: {Id}", bookId);
                return new NotFoundObjectResult(new { Message = $"Book with ID {bookId} not found." });
            }

            if (req.Form.Files.Count == 0)
            {
                _logger.LogWarning("UploadBookImage: No file uploaded");
                return new BadRequestObjectResult(new { Message = "No file uploaded" });
            }

            var file = req.Form.Files[0];
            if (file.Length > 2 * 1024 * 1024)
            {
                _logger.LogWarning("UploadBookImage: File size exceeds 2 MB");
                return new BadRequestObjectResult(new { Message = "File size exceeds 2 MB" });
            }

            try
            {
                var relativePath = await _bookService.UploadBookImageAsync(bookId, file);
                return new OkObjectResult(new { FilePath = relativePath });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UploadBookImage: Error uploading image for book: {Id}", bookId);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
