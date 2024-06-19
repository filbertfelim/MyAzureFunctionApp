using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MyAzureFunctionApp.Models.DTOs;
using MyAzureFunctionApp.Services;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Azure.Functions.Worker;
using System.Text.Json;
using AutoMapper;
using MyAzureFunctionApp.Validators;

namespace MyAzureFunctionApp.Controllers
{
    public class BooksFunction
    {
        private readonly IBookService _bookService;
        private readonly IValidator<BookDto> _validator;
        private readonly IMapper _mapper;

        public BooksFunction(IBookService bookService, IValidator<BookDto> validator, IMapper mapper)
        {
            _bookService = bookService;
            _validator = validator;
            _mapper = mapper;
        }

        [Function("GetBooks")]
        public async Task<IActionResult> GetBooks(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "books")] HttpRequest req)
        {
            var books = await _bookService.GetAllAsync();
            return new OkObjectResult(new { Message = "Books retrieved successfully.", Data = books });
        }

        [Function("GetBookById")]
        public async Task<IActionResult> GetBookById(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "books/{id}")] HttpRequest req, string id)
        {
            if (!int.TryParse(id, out int bookId) || bookId <= 0)
            {
                return new BadRequestObjectResult(new { Message = "Invalid ID format." });
            }

            var book = await _bookService.GetByIdAsync(bookId);
            if (book == null)
            {
                return new NotFoundObjectResult(new { Message = $"Book with ID {bookId} not found." });
            }
            return new OkObjectResult(new { Message = "Book retrieved successfully.", Data = book });
        }

        [Function("CreateBook")]
        public async Task<IActionResult> CreateBook(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "books")] HttpRequest req)
        {
            if (req.Body == null)
            {
                return new BadRequestObjectResult(new { Message = "Request body cannot be null." });
            }

            BookDto data;
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                // Check if the JSON contains only the expected fields
                if (!DtoJsonValidator.IsValidJsonStructure<BookDto>(requestBody))
                {
                    return new BadRequestObjectResult(new { Message = "Invalid request structure." });
                }

                data = JsonSerializer.Deserialize<BookDto>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (data == null)
                {
                    return new BadRequestObjectResult(new { Message = "Invalid request body." });
                }
            }
            catch (JsonException)
            {
                return new BadRequestObjectResult(new { Message = "Invalid request structure." });
            }

            ValidationResult validationResult = await _validator.ValidateAsync(data);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(x => new { x.PropertyName, x.ErrorMessage });
                return new BadRequestObjectResult(new { Message = "Validation failed.", Errors = errors });
            }

            // Trim whitespace from the title
            data.Title = data.Title.Trim();

            var (createdBook, errorMessage) = await _bookService.AddAsync(data);
            if (createdBook == null)
            {
                return new BadRequestObjectResult(new { Message = errorMessage });
            }
            return new CreatedResult($"/books/{createdBook.BookId}", new { Message = "Book created successfully.", Data = createdBook });
        }

        [Function("UpdateBook")]
        public async Task<IActionResult> UpdateBook(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "books/{id}")] HttpRequest req, string id)
        {
            if (!int.TryParse(id, out int bookId) || bookId <= 0)
            {
                return new BadRequestObjectResult(new { Message = "Invalid ID format." });
            }

            if (req.Body == null)
            {
                return new BadRequestObjectResult(new { Message = "Request body cannot be null." });
            }

            BookDto data;
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                // Check if the JSON contains only the expected fields
                if (!DtoJsonValidator.IsValidJsonStructure<BookDto>(requestBody))
                {
                    return new BadRequestObjectResult(new { Message = "Invalid request structure." });
                }

                data = JsonSerializer.Deserialize<BookDto>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (data == null)
                {
                    return new BadRequestObjectResult(new { Message = "Invalid request body." });
                }
            }
            catch (JsonException)
            {
                return new BadRequestObjectResult(new { Message = "Invalid request structure." });
            }

            ValidationResult validationResult = await _validator.ValidateAsync(data);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(x => new { x.PropertyName, x.ErrorMessage });
                return new BadRequestObjectResult(new { Message = "Validation failed.", Errors = errors });
            }

            // Trim whitespace from the title
            data.Title = data.Title.Trim();

            var (updatedBook, errorMessage) = await _bookService.UpdateAsync(bookId, data);
            if (updatedBook == null)
            {
                return new BadRequestObjectResult(new { Message = errorMessage });
            }

            return new OkObjectResult(new { Message = "Book updated successfully.", Data = updatedBook });
        }

        [Function("DeleteBook")]
        public async Task<IActionResult> DeleteBook(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "books/{id}")] HttpRequest req, string id)
        {
            if (!int.TryParse(id, out int bookId) || bookId <= 0)
            {
                return new BadRequestObjectResult(new { Message = "Invalid ID format." });
            }

            var book = await _bookService.GetByIdAsync(bookId);
            if (book == null)
            {
                return new NotFoundObjectResult(new { Message = $"Book with ID {bookId} not found." });
            }

            await _bookService.DeleteAsync(bookId);

            return new OkObjectResult(new { Message = "Book deleted successfully." });
        }
    }
}
