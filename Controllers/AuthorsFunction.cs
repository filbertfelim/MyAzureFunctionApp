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
using MyAzureFunctionApp.Models;
using MyAzureFunctionApp.Validators;

namespace MyAzureFunctionApp.Controllers
{
    public class AuthorsFunction
    {
        private readonly IAuthorService _authorService;
        private readonly IValidator<AuthorDto> _validator;
        private readonly IMapper _mapper;

        public AuthorsFunction(IAuthorService authorService, IValidator<AuthorDto> validator, IMapper mapper)
        {
            _authorService = authorService;
            _validator = validator;
            _mapper = mapper;
        }

        [Function("GetAuthors")]
        public async Task<IActionResult> GetAuthors(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "authors")] HttpRequest req)
        {
            var authors = await _authorService.GetAllAsync();
            return new OkObjectResult(new { Message = "Authors retrieved successfully.", Data = authors });
        }

        [Function("GetAuthorById")]
        public async Task<IActionResult> GetAuthorById(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "authors/{id}")] HttpRequest req, string id)
        {
            if (!int.TryParse(id, out int authorId) || authorId <= 0)
            {
                return new BadRequestObjectResult(new { Message = "Invalid ID format, ID should be a number." });
            }
            var author = await _authorService.GetByIdAsync(authorId);
            if (author == null)
            {
                return new NotFoundObjectResult(new { Message = $"Author with ID {authorId} not found." });
            }
            return new OkObjectResult(new { Message = "Author retrieved successfully.", Data = author });
        }

        [Function("CreateAuthor")]
        public async Task<IActionResult> CreateAuthor(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "authors")] HttpRequest req)
        {
            if (req.Body == null)
            {
                return new BadRequestObjectResult(new { Message = "Request body cannot be null." });
            }
            AuthorDto data;
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                // Check if the JSON contains only the expected fields
                if (!DtoJsonValidator.IsValidJsonStructure<AuthorDto>(requestBody))
                {
                    return new BadRequestObjectResult(new { Message = "Invalid request structure." });
                }

                data = JsonSerializer.Deserialize<AuthorDto>(requestBody, new JsonSerializerOptions
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

            ValidationResult result = await _validator.ValidateAsync(data);
            if (!result.IsValid)
            {
                var errors = result.Errors.Select(x => new { x.PropertyName, x.ErrorMessage });
                return new BadRequestObjectResult(new { Message = "Validation failed.", Errors = errors });
            }

            data.Name = data.Name.Trim();

            var author = _mapper.Map<AuthorDto>(data);
            var createdAuthor = await _authorService.AddAsync(author);
            return new CreatedResult($"/authors/{createdAuthor.AuthorId}", new { Message = "Author created successfully.", Data = createdAuthor });
        }

        [Function("UpdateAuthor")]
        public async Task<IActionResult> UpdateAuthor(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "authors/{id}")] HttpRequest req, string id)
        {
            if (!int.TryParse(id, out int authorId) || authorId <= 0)
            {
                return new BadRequestObjectResult(new { Message = "Invalid ID format, ID should be a number." });
            }
            if (req.Body == null)
            {
                return new BadRequestObjectResult(new { Message = "Request body cannot be null." });
            }
            AuthorDto data;
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                // Check if the JSON contains only the expected fields
                if (!DtoJsonValidator.IsValidJsonStructure<AuthorDto>(requestBody))
                {
                    return new BadRequestObjectResult(new { Message = "Invalid request structure." });
                }

                data = JsonSerializer.Deserialize<AuthorDto>(requestBody, new JsonSerializerOptions
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

            ValidationResult result = await _validator.ValidateAsync(data);
            if (!result.IsValid)
            {
                var errors = result.Errors.Select(x => new { x.PropertyName, x.ErrorMessage });
                return new BadRequestObjectResult(new { Message = "Validation failed.", Errors = errors });
            }

            data.Name = data.Name.Trim();
            
            var author = _mapper.Map<AuthorDto>(data);
            var updatedAuthor = await _authorService.UpdateAsync(authorId, author);
            if (updatedAuthor == null)
            {
                return new NotFoundObjectResult(new { Message = $"Author with ID {authorId} not found." });
            }

            return new OkObjectResult(new { Message = "Author updated successfully.", Data = updatedAuthor });
        }

        [Function("DeleteAuthor")]
        public async Task<IActionResult> DeleteAuthor(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "authors/{id}")] HttpRequest req, string id)
        {
            if (!int.TryParse(id, out int authorId) || authorId <= 0)
            {
                return new BadRequestObjectResult(new { Message = "Invalid ID format, ID should be a number." });
            }
            var author = await _authorService.GetByIdAsync(authorId);
            if (author == null)
            {
                return new NotFoundObjectResult(new { Message = $"Author with ID {authorId} not found." });
            }

            await _authorService.DeleteAsync(authorId);

            return new OkObjectResult(new { Message = "Author deleted successfully." });
        }
    }
}
