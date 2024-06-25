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
using MyAzureFunctionApp.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MyAzureFunctionApp.Controllers
{
    public class CategoriesFunction : AuthenticatedFunctionBase
    {
        private readonly ICategoryService _categoryService;
        private readonly IValidator<CategoryDto> _validator;
        private readonly IMapper _mapper;
        private readonly ILogger<CategoriesFunction> _logger;

        public CategoriesFunction(ICategoryService categoryService, IValidator<CategoryDto> validator, IMapper mapper, IConfiguration configuration, ILogger<CategoriesFunction> logger) 
            : base(configuration, logger)
        {
            _categoryService = categoryService;
            _validator = validator;
            _mapper = mapper;
            _logger = logger;
        }

        [Function("GetCategories")]
        public async Task<IActionResult> GetCategories(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "categories")] HttpRequest req)
        {
            _logger.LogInformation("GetCategories: Processing request to get all categories.");

            var validationResult = ValidateToken(req);
            if (validationResult != null)
            {
                _logger.LogWarning("GetCategories: Authorization failed: {Message}", ((JsonResult)validationResult).Value);
                return validationResult;
            }

            var categories = await _categoryService.GetAllAsync();
            _logger.LogInformation("GetCategories: Successfully retrieved categories.");

            return new OkObjectResult(new { Message = "Categories retrieved successfully.", Data = categories });
        }

        [Function("GetCategoryById")]
        public async Task<IActionResult> GetCategoryById(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "categories/{id}")] HttpRequest req, string id)
        {
            _logger.LogInformation("GetCategoryById: Processing request to get category by ID: {Id}", id);

            var validationResult = ValidateToken(req);
            if (validationResult != null)
            {
                _logger.LogWarning("GetCategoryById: Authorization failed: {Message}", ((JsonResult)validationResult).Value);
                return validationResult;
            }

            if (!int.TryParse(id, out int categoryId) || categoryId <= 0)
            {
                _logger.LogWarning("GetCategoryById: Invalid ID format: {Id}", id);
                return new BadRequestObjectResult(new { Message = "Invalid ID format." });
            }

            var category = await _categoryService.GetByIdAsync(categoryId);
            if (category == null)
            {
                _logger.LogWarning("GetCategoryById: Category not found: {Id}", categoryId);
                return new NotFoundObjectResult(new { Message = $"Category with ID {categoryId} not found." });
            }

            _logger.LogInformation("GetCategoryById: Successfully retrieved category with ID: {Id}", categoryId);
            return new OkObjectResult(new { Message = "Category retrieved successfully.", Data = category });
        }

        [Function("CreateCategory")]
        public async Task<IActionResult> CreateCategory(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "categories")] HttpRequest req)
        {
            _logger.LogInformation("CreateCategory: Processing request to create a new category.");

            var validationResult = ValidateToken(req);
            if (validationResult != null)
            {
                _logger.LogWarning("CreateCategory: Authorization failed: {Message}", ((JsonResult)validationResult).Value);
                return validationResult;
            }

            if (req.Body == null)
            {
                _logger.LogWarning("CreateCategory: Request body is null.");
                return new BadRequestObjectResult(new { Message = "Request body cannot be null." });
            }

            CategoryDto data;
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                if (!DtoJsonValidator.IsValidJsonStructure<CategoryDto>(requestBody))
                {
                    _logger.LogWarning("CreateCategory: Invalid request structure.");
                    return new BadRequestObjectResult(new { Message = "Invalid request structure." });
                }

                data = JsonSerializer.Deserialize<CategoryDto>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (data == null)
                {
                    _logger.LogWarning("CreateCategory: Invalid request body.");
                    return new BadRequestObjectResult(new { Message = "Invalid request body." });
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "CreateCategory: Failed to deserialize request body.");
                return new BadRequestObjectResult(new { Message = "Invalid request structure." });
            }

            ValidationResult validResult = await _validator.ValidateAsync(data);
            if (!validResult.IsValid)
            {
                var errors = validResult.Errors.Select(x => new { x.PropertyName, x.ErrorMessage });
                _logger.LogWarning("CreateCategory: Validation failed: {Errors}", errors);
                return new BadRequestObjectResult(new { Message = "Validation failed.", Errors = errors });
            }

            data.Name = data.Name.Trim();

            var category = _mapper.Map<CategoryDto>(data);
            var (createdCategory, errorMessage) = await _categoryService.AddAsync(category);
            if (createdCategory == null)
            {
                _logger.LogWarning("CreateCategory: Failed to create category: {Message}", errorMessage);
                return new BadRequestObjectResult(new { Message = errorMessage });
            }

            _logger.LogInformation("CreateCategory: Successfully created category with ID: {Id}", createdCategory.CategoryId);
            return new CreatedResult($"/categories/{createdCategory.CategoryId}", new { Message = "Category created successfully.", Data = createdCategory });
        }

        [Function("UpdateCategory")]
        public async Task<IActionResult> UpdateCategory(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "categories/{id}")] HttpRequest req, string id)
        {
            _logger.LogInformation("UpdateCategory: Processing request to update category with ID: {Id}", id);

            var validationResult = ValidateToken(req);
            if (validationResult != null)
            {
                _logger.LogWarning("UpdateCategory: Authorization failed: {Message}", ((JsonResult)validationResult).Value);
                return validationResult;
            }

            if (!int.TryParse(id, out int categoryId) || categoryId <= 0)
            {
                _logger.LogWarning("UpdateCategory: Invalid ID format: {Id}", id);
                return new BadRequestObjectResult(new { Message = "Invalid ID format." });
            }

            if (req.Body == null)
            {
                _logger.LogWarning("UpdateCategory: Request body is null.");
                return new BadRequestObjectResult(new { Message = "Request body cannot be null." });
            }

            CategoryDto data;
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                if (!DtoJsonValidator.IsValidJsonStructure<CategoryDto>(requestBody))
                {
                    _logger.LogWarning("UpdateCategory: Invalid request structure.");
                    return new BadRequestObjectResult(new { Message = "Invalid request structure." });
                }

                data = JsonSerializer.Deserialize<CategoryDto>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (data == null)
                {
                    _logger.LogWarning("UpdateCategory: Invalid request body.");
                    return new BadRequestObjectResult(new { Message = "Invalid request body." });
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "UpdateCategory: Failed to deserialize request body.");
                return new BadRequestObjectResult(new { Message = "Invalid request structure." });
            }

            ValidationResult validResult = await _validator.ValidateAsync(data);
            if (!validResult.IsValid)
            {
                var errors = validResult.Errors.Select(x => new { x.PropertyName, x.ErrorMessage });
                _logger.LogWarning("UpdateCategory: Validation failed: {Errors}", errors);
                return new BadRequestObjectResult(new { Message = "Validation failed.", Errors = errors });
            }

            data.Name = data.Name.Trim();

            var category = _mapper.Map<CategoryDto>(data);
            var (updatedCategory, errorMessage) = await _categoryService.UpdateAsync(categoryId, category);
            if (updatedCategory == null)
            {
                _logger.LogWarning("UpdateCategory: Failed to update category: {Message}", errorMessage);
                return new BadRequestObjectResult(new { Message = errorMessage });
            }

            _logger.LogInformation("UpdateCategory: Successfully updated category with ID: {Id}", updatedCategory.CategoryId);
            return new OkObjectResult(new { Message = "Category updated successfully.", Data = updatedCategory });
        }

        [Function("DeleteCategory")]
        public async Task<IActionResult> DeleteCategory(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "categories/{id}")] HttpRequest req, string id)
        {
            _logger.LogInformation("DeleteCategory: Processing request to delete category with ID: {Id}", id);

            var validationResult = ValidateToken(req);
            if (validationResult != null)
            {
                _logger.LogWarning("DeleteCategory: Authorization failed: {Message}", ((JsonResult)validationResult).Value);
                return validationResult;
            }

            if (!int.TryParse(id, out int categoryId) || categoryId <= 0)
            {
                _logger.LogWarning("DeleteCategory: Invalid ID format: {Id}", id);
                return new BadRequestObjectResult(new { Message = "Invalid ID format." });
            }

            var category = await _categoryService.GetByIdAsync(categoryId);
            if (category == null)
            {
                _logger.LogWarning("DeleteCategory: Category not found: {Id}", categoryId);
                return new NotFoundObjectResult(new { Message = $"Category with ID {categoryId} not found." });
            }

            await _categoryService.DeleteAsync(categoryId);

            _logger.LogInformation("DeleteCategory: Successfully deleted category with ID: {Id}", categoryId);
            return new OkObjectResult(new { Message = "Category deleted successfully." });
        }
    }
}
