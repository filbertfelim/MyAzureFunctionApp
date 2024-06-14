using MyAzureFunctionApp.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

public class CategoriesFunction
{
    private readonly AppDbContext _dbContext;

    public CategoriesFunction(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [Function("GetCategories")]
    public async Task<HttpResponseData> GetCategories([HttpTrigger(AuthorizationLevel.Function, "get", Route = "categories")] HttpRequestData req)
    {
        var categories = await _dbContext.Categories.ToListAsync();

        var categoryDtos = categories.Select(c => new CategoryDto
        {
            CategoryId = c.CategoryId,
            Name = c.Name
        }).ToList();

        var response = req.CreateResponse(HttpStatusCode.OK);

        var jsonOptions = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = true
        };

        var jsonResponse = new { data = categoryDtos };
        var json = JsonSerializer.Serialize(jsonResponse, jsonOptions);

        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(json);

        return response;
    }

    [Function("GetCategoryById")]
    public async Task<HttpResponseData> GetCategoryById([HttpTrigger(AuthorizationLevel.Function, "get", Route = "categories/{id}")] HttpRequestData req, int id)
    {
        var category = await _dbContext.Categories.FindAsync(id);
        var response = req.CreateResponse(HttpStatusCode.OK);

        if (category == null)
        {
            response = req.CreateResponse(HttpStatusCode.NotFound);
            var errorResponse = new { message = "Category not found." };
            var jsonError = JsonSerializer.Serialize(errorResponse);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(jsonError);
            return response;
        }

        var categoryDto = new CategoryDto
        {
            CategoryId = category.CategoryId,
            Name = category.Name
        };

        response = req.CreateResponse(HttpStatusCode.OK);

        var jsonOptions = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = true
        };

        var jsonResponse = new { data = categoryDto };
        var json = JsonSerializer.Serialize(jsonResponse, jsonOptions);

        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(json);

        return response;
    }

    [Function("CreateCategory")]
    public async Task<HttpResponseData> CreateCategory([HttpTrigger(AuthorizationLevel.Function, "post", Route = "categories")] HttpRequestData req)
    {
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var input = JsonSerializer.Deserialize<CategoryDto>(requestBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        var response = req.CreateResponse(HttpStatusCode.BadRequest);

        if (input == null || string.IsNullOrWhiteSpace(input.Name))
        {
            var errorResponse = new { message = "Invalid input data." };
            var json = JsonSerializer.Serialize(errorResponse);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(json);
            return response;
        }

        var category = new Category
        {
            Name = input.Name
        };

        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync();

        response = req.CreateResponse(HttpStatusCode.Created);
        
        var jsonOptions = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = true
        };

        var categoryResponse = new
        {
            data = new
            {
                categoryId = category.CategoryId,
                name = category.Name
            }
        };

        var jsonResponse = JsonSerializer.Serialize(categoryResponse, jsonOptions);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(jsonResponse);

        return response;
    }

    [Function("UpdateCategory")]
    public async Task<HttpResponseData> UpdateCategory([HttpTrigger(AuthorizationLevel.Function, "put", Route = "categories/{id}")] HttpRequestData req, int id)
    {
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var input = JsonSerializer.Deserialize<CategoryDto>(requestBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        var response = req.CreateResponse(HttpStatusCode.BadRequest);

        if (input == null || string.IsNullOrWhiteSpace(input.Name))
        {
            var errorResponse = new { message = "Invalid input data." };
            var json = JsonSerializer.Serialize(errorResponse);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(json);
            return response;
        }

        var category = await _dbContext.Categories.FindAsync(id);

        if (category == null)
        {
            var errorResponse = new { message = "Category not found." };
            var json = JsonSerializer.Serialize(errorResponse);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(json);
            return response;
        }

        category.Name = input.Name;

        await _dbContext.SaveChangesAsync();

        response = req.CreateResponse(HttpStatusCode.OK);

        var jsonOptions = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = true
        };

        var categoryResponse = new
        {
            data = new
            {
                categoryId = category.CategoryId,
                name = category.Name
            }
        };

        var jsonResponse = JsonSerializer.Serialize(categoryResponse, jsonOptions);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(jsonResponse);

        return response;
    }

    [Function("DeleteCategory")]
    public async Task<HttpResponseData> DeleteCategory([HttpTrigger(AuthorizationLevel.Function, "delete", Route = "categories/{id}")] HttpRequestData req, int id)
    {
        var category = await _dbContext.Categories.FindAsync(id);
        var response = req.CreateResponse(HttpStatusCode.NoContent);
        if (category == null)
        {
            response = req.CreateResponse(HttpStatusCode.NotFound);
            var errorResponse = new { message = "Category not found." };
            var json = JsonSerializer.Serialize(errorResponse);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(json);
            return response;
        }

        _dbContext.Categories.Remove(category);
        await _dbContext.SaveChangesAsync();

        response = req.CreateResponse(HttpStatusCode.NoContent);
        return response;
    }
}
