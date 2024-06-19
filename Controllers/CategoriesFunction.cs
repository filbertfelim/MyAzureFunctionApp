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
    public class CategoriesFunction
    {
        private readonly ICategoryService _categoryService;
        private readonly IValidator<CategoryDto> _validator;
        private readonly IMapper _mapper;

        public CategoriesFunction(ICategoryService categoryService, IValidator<CategoryDto> validator, IMapper mapper)
        {
            _categoryService = categoryService;
            _validator = validator;
            _mapper = mapper;
        }

        [Function("GetCategories")]
        public async Task<IActionResult> GetCategories(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "categories")] HttpRequest req)
        {
            var categories = await _categoryService.GetAllAsync();
            return new OkObjectResult(new { Message = "Categories retrieved successfully.", Data = categories });
        }

        [Function("GetCategoryById")]
        public async Task<IActionResult> GetCategoryById(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "categories/{id}")] HttpRequest req, string id)
        {
            if (!int.TryParse(id, out int categoryId) || categoryId <= 0)
            {
                return new BadRequestObjectResult(new { Message = "Invalid ID format." });
            }

            var category = await _categoryService.GetByIdAsync(categoryId);
            if (category == null)
            {
                return new NotFoundObjectResult(new { Message = $"Category with ID {categoryId} not found." });
            }
            return new OkObjectResult(new { Message = "Category retrieved successfully.", Data = category });
        }

        [Function("CreateCategory")]
        public async Task<IActionResult> CreateCategory(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "categories")] HttpRequest req)
        {
            if (req.Body == null)
            {
                return new BadRequestObjectResult(new { Message = "Request body cannot be null." });
            }

            CategoryDto data;
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                // Check if the JSON contains only the expected fields
                if (!DtoJsonValidator.IsValidJsonStructure<CategoryDto>(requestBody))
                {
                    return new BadRequestObjectResult(new { Message = "Invalid request structure." });
                }

                data = JsonSerializer.Deserialize<CategoryDto>(requestBody, new JsonSerializerOptions
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

            // Trim whitespace from the name
            data.Name = data.Name.Trim();

            var category = _mapper.Map<CategoryDto>(data);
            var (createdCategory, errorMessage) = await _categoryService.AddAsync(category);
            if (createdCategory == null)
            {
                return new BadRequestObjectResult(new { Message = errorMessage });
            }
            return new CreatedResult($"/categories/{createdCategory.CategoryId}", new { Message = "Category created successfully.", Data = createdCategory });
        }

        [Function("UpdateCategory")]
        public async Task<IActionResult> UpdateCategory(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "categories/{id}")] HttpRequest req, string id)
        {
            if (!int.TryParse(id, out int categoryId) || categoryId <= 0)
            {
                return new BadRequestObjectResult(new { Message = "Invalid ID format." });
            }

            if (req.Body == null)
            {
                return new BadRequestObjectResult(new { Message = "Request body cannot be null." });
            }

            CategoryDto data;
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                // Check if the JSON contains only the expected fields
                if (!DtoJsonValidator.IsValidJsonStructure<CategoryDto>(requestBody))
                {
                    return new BadRequestObjectResult(new { Message = "Invalid request structure." });
                }

                data = JsonSerializer.Deserialize<CategoryDto>(requestBody, new JsonSerializerOptions
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

            // Trim whitespace from the name
            data.Name = data.Name.Trim();

            var category = _mapper.Map<CategoryDto>(data);
            var (updatedCategory, errorMessage) = await _categoryService.UpdateAsync(categoryId, category);
            if (updatedCategory == null)
            {
                return new BadRequestObjectResult(new { Message = errorMessage });
            }

            return new OkObjectResult(new { Message = "Category updated successfully.", Data = updatedCategory });
        }

        [Function("DeleteCategory")]
        public async Task<IActionResult> DeleteCategory(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "categories/{id}")] HttpRequest req, string id)
        {
            if (!int.TryParse(id, out int categoryId) || categoryId <= 0)
            {
                return new BadRequestObjectResult(new { Message = "Invalid ID format." });
            }

            var category = await _categoryService.GetByIdAsync(categoryId);
            if (category == null)
            {
                return new NotFoundObjectResult(new { Message = $"Category with ID {categoryId} not found." });
            }

            await _categoryService.DeleteAsync(categoryId);

            return new OkObjectResult(new { Message = "Category deleted successfully." });
        }
    }
}
