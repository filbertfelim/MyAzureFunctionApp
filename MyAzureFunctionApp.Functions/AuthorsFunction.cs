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
using Microsoft.Extensions.Configuration;
using MyAzureFunctionApp.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using System.Net;
using Microsoft.OpenApi.Models;

namespace MyAzureFunctionApp.Controllers
{
    public class AuthorsFunction : AuthenticatedFunctionBase
    {
        private readonly IAuthorService _authorService;
        private readonly IValidator<AuthorDto> _validator;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthorsFunction> _logger;

        public AuthorsFunction(IAuthorService authorService, IValidator<AuthorDto> validator, IMapper mapper, IConfiguration configuration, ILogger<AuthorsFunction> logger)
            : base(configuration, logger)
        {
            _authorService = authorService;
            _validator = validator;
            _mapper = mapper;
            _logger = logger;
        }

        
        [OpenApiOperation(operationId: "GetAuthors", tags: new[] { "Authors" })]
        [OpenApiSecurity("Bearer", SecuritySchemeType.ApiKey, Scheme =  OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT", In = OpenApiSecurityLocationType.Header, Name = "Authorization")]
        [Function("GetAuthors")]
        public async Task<IActionResult> GetAuthors(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "authors")] HttpRequest req)
        {
            _logger.LogInformation("GetAuthors: Processing request to get all authors");
            var validationResult = ValidateToken(req);
            if (validationResult != null)
            {
                _logger.LogWarning("GetAuthors: Authorization failed: {Message}", ((JsonResult)validationResult).Value);
                return validationResult;
            }
            var authors = await _authorService.GetAllAsync();
            _logger.LogInformation("GetAuthors: Successfully retrieved authors");
            return new OkObjectResult(new { Message = "Authors retrieved successfully.", Data = authors });
        }

        
        [OpenApiOperation(operationId: "GetAuthorById", tags: new[] { "Authors" })]
        [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(int), Description = "The ID of the author")]
        [OpenApiSecurity("Bearer", SecuritySchemeType.ApiKey, Scheme =  OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT", In = OpenApiSecurityLocationType.Header, Name = "Authorization")]
        [Function("GetAuthorById")]
        public async Task<IActionResult> GetAuthorById(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "authors/{id}")] HttpRequest req, string id)
        {
            _logger.LogInformation("GetAuthorById: Processing request to get author by ID: {Id}", id);
            var validationResult = ValidateToken(req);
            if (validationResult != null)
            {
                 _logger.LogWarning("GetAuthorById: Authorization failed: {Message}", ((JsonResult)validationResult).Value);
                return validationResult;
            }
            if (!int.TryParse(id, out int authorId) || authorId <= 0)
            {
                _logger.LogWarning("GetAuthorById: Invalid ID format: {Id}", id);
                return new BadRequestObjectResult(new { Message = "Invalid ID format, ID should be a number." });
            }
            var author = await _authorService.GetByIdAsync(authorId);
            if (author == null)
            {
                _logger.LogWarning("GetAuthorById: Author not found: {Id}", authorId);
                return new NotFoundObjectResult(new { Message = $"Author with ID {authorId} not found." });
            }
            _logger.LogInformation("GetAuthorById: Successfully retrieved author with ID: {Id}", authorId);
            return new OkObjectResult(new { Message = "Author retrieved successfully.", Data = author });
        }

        [OpenApiOperation(operationId: "CreateAuthor", tags: new[] { "Authors" })]
        [OpenApiRequestBody("application/json", typeof(AuthorDto), Description = "The author data to create")]
        [OpenApiSecurity("Bearer", SecuritySchemeType.ApiKey, Scheme =  OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT", In = OpenApiSecurityLocationType.Header, Name = "Authorization")]
        [Function("CreateAuthor")]
        public async Task<IActionResult> CreateAuthor(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "authors")] HttpRequest req)
        {
            _logger.LogInformation("CreateAuthor: Processing request to create a new author");
            var validationResult = ValidateToken(req);
            if (validationResult != null)
            {
                _logger.LogWarning("CreateAuthor: Authorization failed: {Message}", ((JsonResult)validationResult).Value);
                return validationResult;
            }
            if (req.Body == null)
            {
                _logger.LogWarning("CreateAuthor: Request body is null");
                return new BadRequestObjectResult(new { Message = "Request body cannot be null." });
            }
            AuthorDto data;
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                // Check if the JSON contains only the expected fields
                if (!DtoJsonValidator.IsValidJsonStructure<AuthorDto>(requestBody))
                {
                    _logger.LogWarning("CreateAuthor: Invalid request structure");
                    return new BadRequestObjectResult(new { Message = "Invalid request structure." });
                }

                data = JsonSerializer.Deserialize<AuthorDto>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (data == null)
                {
                    _logger.LogWarning("CreateAuthor: Invalid request body");
                    return new BadRequestObjectResult(new { Message = "Invalid request body." });
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "CreateAuthor: Failed to deserialize request body");
                return new BadRequestObjectResult(new { Message = "Invalid request structure." });
            }

            ValidationResult result = await _validator.ValidateAsync(data);
            if (!result.IsValid)
            {
                var errors = result.Errors.Select(x => new { x.PropertyName, x.ErrorMessage });
                _logger.LogWarning("CreateAuthor: Validation failed: {Errors}", errors);
                return new BadRequestObjectResult(new { Message = "Validation failed.", Errors = errors });
            }

            data.Name = data.Name.Trim();

            var author = _mapper.Map<AuthorDto>(data);
            var createdAuthor = await _authorService.AddAsync(author);
            _logger.LogInformation("CreateAuthor: Successfully created author with ID: {Id}", createdAuthor.AuthorId);
            return new CreatedResult($"/authors/{createdAuthor.AuthorId}", new { Message = "Author created successfully.", Data = createdAuthor });
        }

        [OpenApiOperation(operationId: "UpdateAuthor", tags: new[] { "Authors" })]
        [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(int), Description = "The ID of the author to update")]
        [OpenApiRequestBody("application/json", typeof(AuthorDto), Description = "The author data to update")]
        [OpenApiSecurity("Bearer", SecuritySchemeType.ApiKey, Scheme =  OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT", In = OpenApiSecurityLocationType.Header, Name = "Authorization")]
        [Function("UpdateAuthor")]
        public async Task<IActionResult> UpdateAuthor(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "authors/{id}")] HttpRequest req, string id)
        {
            _logger.LogInformation("UpdateAuthor: Processing request to update author with ID: {Id}", id);
            var validationResult = ValidateToken(req);
            if (validationResult != null)
            {
                _logger.LogWarning("UpdateAuthor: Authorization failed: {Message}", ((JsonResult)validationResult).Value);
                return validationResult;
            }
            if (!int.TryParse(id, out int authorId) || authorId <= 0)
            {
                _logger.LogWarning("UpdateAuthor: Invalid ID format: {Id}", id);
                return new BadRequestObjectResult(new { Message = "Invalid ID format, ID should be a number." });
            }
            if (req.Body == null)
            {
                _logger.LogWarning("UpdateAuthor: Request body is null");
                return new BadRequestObjectResult(new { Message = "Request body cannot be null." });
            }
            AuthorDto data;
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                // Check if the JSON contains only the expected fields
                if (!DtoJsonValidator.IsValidJsonStructure<AuthorDto>(requestBody))
                {
                    _logger.LogWarning("UpdateAuthor: Invalid request structure");
                    return new BadRequestObjectResult(new { Message = "Invalid request structure." });
                }

                data = JsonSerializer.Deserialize<AuthorDto>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (data == null)
                {
                    _logger.LogWarning("UpdateAuthor: Invalid request body");
                    return new BadRequestObjectResult(new { Message = "Invalid request body." });
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "UpdateAuthor: Failed to deserialize request body");
                return new BadRequestObjectResult(new { Message = "Invalid request structure." });
            }

            ValidationResult result = await _validator.ValidateAsync(data);
            if (!result.IsValid)
            {
                var errors = result.Errors.Select(x => new { x.PropertyName, x.ErrorMessage });
                _logger.LogWarning("UpdateAuthor: Validation failed: {Errors}", errors);
                return new BadRequestObjectResult(new { Message = "Validation failed.", Errors = errors });
            }

            data.Name = data.Name.Trim();
            
            var author = _mapper.Map<AuthorDto>(data);
            var updatedAuthor = await _authorService.UpdateAsync(authorId, author);
            if (updatedAuthor == null)
            {
                _logger.LogWarning("UpdateAuthor: Author not found: {Id}", authorId);
                return new NotFoundObjectResult(new { Message = $"Author with ID {authorId} not found." });
            }

            _logger.LogInformation("UpdateAuthor: Successfully updated author with ID: {Id}", authorId);
            return new OkObjectResult(new { Message = "Author updated successfully.", Data = updatedAuthor });
        }

        [OpenApiOperation(operationId: "DeleteAuthor", tags: new[] { "Authors" })]
        [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(int), Description = "The ID of the author to delete")]
        [OpenApiSecurity("Bearer", SecuritySchemeType.ApiKey, Scheme =  OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT", In = OpenApiSecurityLocationType.Header, Name = "Authorization")]
        [Function("DeleteAuthor")]
        public async Task<IActionResult> DeleteAuthor(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "authors/{id}")] HttpRequest req, string id)
        {
            _logger.LogInformation("DeleteAuthor: Processing request to delete author with ID: {Id}", id);
            var validationResult = ValidateToken(req);
            if (validationResult != null)
            {
                _logger.LogWarning("DeleteAuthor: Authorization failed: {Message}", ((JsonResult)validationResult).Value);
                return validationResult;
            }
            if (!int.TryParse(id, out int authorId) || authorId <= 0)
            {
                _logger.LogWarning("DeleteAuthor: Invalid ID format: {Id}", id);
                return new BadRequestObjectResult(new { Message = "Invalid ID format, ID should be a number." });
            }
            var author = await _authorService.GetByIdAsync(authorId);
            if (author == null)
            {
                _logger.LogWarning("DeleteAuthor: Author not found: {Id}", authorId);
                return new NotFoundObjectResult(new { Message = $"Author with ID {authorId} not found." });
            }

            await _authorService.DeleteAsync(authorId);

            _logger.LogInformation("DeleteAuthor: Successfully deleted author with ID: {Id}", authorId);
            return new OkObjectResult(new { Message = "Author deleted successfully." });
        }
    }
}
