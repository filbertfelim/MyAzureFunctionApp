using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MyAzureFunctionApp.Models.DTOs;
using MyAzureFunctionApp.Services;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Azure.Functions.Worker;
using System.Text.Json;

namespace MyAzureFunctionApp.Controllers
{
    public class AuthorsFunction
    {
        private readonly IAuthorService _authorService;
        private readonly IValidator<AuthorDto> _validator;

        public AuthorsFunction(IAuthorService authorService, IValidator<AuthorDto> validator)
        {
            _authorService = authorService;
            _validator = validator;
        }

        [Function("GetAuthors")]
        public async Task<IActionResult> GetAuthors(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "authors")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetAuthors function processed a request.");

            var authors = await _authorService.GetAllAsync();
            return new OkObjectResult(new { Message = "Authors retrieved successfully.", Data = authors });
        }

        [Function("GetAuthorById")]
        public async Task<IActionResult> GetAuthorById(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "authors/{id}")] HttpRequest req,
            int id,
            ILogger log)
        {
            log.LogInformation($"GetAuthorById function processed a request for ID: {id}");

            var author = await _authorService.GetByIdAsync(id);
            if (author == null)
            {
                return new NotFoundObjectResult(new { Message = $"Author with ID {id} not found." });
            }

            return new OkObjectResult(new { Message = "Author retrieved successfully.", Data = author });
        }

        [Function("CreateAuthor")]
        public async Task<IActionResult> CreateAuthor(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "authors")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("CreateAuthor function processed a request.");

            AuthorDto data;
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                data = JsonSerializer.Deserialize<AuthorDto>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
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

            var createdAuthor = await _authorService.AddAsync(data);

            return new CreatedResult($"/authors/{createdAuthor.AuthorId}", new { Message = "Author created successfully.", Data = createdAuthor });
        }

        [Function("UpdateAuthor")]
        public async Task<IActionResult> UpdateAuthor(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "authors/{id}")] HttpRequest req,
            int id,
            ILogger log)
        {
            log.LogInformation($"UpdateAuthor function processed a request for ID: {id}");

            AuthorDto data;
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                data = JsonSerializer.Deserialize<AuthorDto>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
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

            var updatedAuthor = await _authorService.UpdateAsync(id, data);
            if (updatedAuthor == null)
            {
                return new NotFoundObjectResult(new { Message = $"Author with ID {id} not found." });
            }

            return new OkObjectResult(new { Message = "Author updated successfully.", Data = updatedAuthor });
        }

        [Function("DeleteAuthor")]
        public async Task<IActionResult> DeleteAuthor(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "authors/{id}")] HttpRequest req,
            int id,
            ILogger log)
        {
            log.LogInformation($"DeleteAuthor function processed a request for ID: {id}");

            var author = await _authorService.GetByIdAsync(id);
            if (author == null)
            {
                return new NotFoundObjectResult(new { Message = $"Author with ID {id} not found." });
            }

            await _authorService.DeleteAsync(id);

            return new OkObjectResult(new { Message = "Author deleted successfully." });
        }
    }
}
