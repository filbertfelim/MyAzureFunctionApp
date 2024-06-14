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

public class AuthorsFunction
{
    private readonly AppDbContext _dbContext;

    public AuthorsFunction(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [Function("GetAuthors")]
    public async Task<HttpResponseData> GetAuthors([HttpTrigger(AuthorizationLevel.Function, "get", Route = "authors")] HttpRequestData req)
    {
        var authors = await _dbContext.Authors.ToListAsync();

        var authorDtos = authors.Select(a => new AuthorDto
        {
            AuthorId = a.AuthorId,
            Name = a.Name
        }).ToList();

        var response = req.CreateResponse(HttpStatusCode.OK);

        var jsonOptions = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = true
        };

        var jsonResponse = new { data = authorDtos };
        var json = JsonSerializer.Serialize(jsonResponse, jsonOptions);

        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(json);

        return response;
    }

    [Function("GetAuthorById")]
    public async Task<HttpResponseData> GetAuthorById([HttpTrigger(AuthorizationLevel.Function, "get", Route = "authors/{id}")] HttpRequestData req, int id)
    {
        var author = await _dbContext.Authors.FindAsync(id);
        var response = req.CreateResponse(HttpStatusCode.NotFound);

        if (author == null)
        {
            var errorResponse = new { message = "Author not found." };
            var jsonError = JsonSerializer.Serialize(errorResponse);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(jsonError);
            return response;
        }

        var authorDto = new AuthorDto
        {
            AuthorId = author.AuthorId,
            Name = author.Name
        };

        response = req.CreateResponse(HttpStatusCode.OK);

        var jsonOptions = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = true
        };

        var jsonResponse = new { data = authorDto };
        var json = JsonSerializer.Serialize(jsonResponse, jsonOptions);

        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(json);

        return response;
    }

    [Function("CreateAuthor")]
    public async Task<HttpResponseData> CreateAuthor([HttpTrigger(AuthorizationLevel.Function, "post", Route = "authors")] HttpRequestData req)
    {
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var input = JsonSerializer.Deserialize<AuthorDto>(requestBody, new JsonSerializerOptions
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

        var author = new Author
        {
            Name = input.Name
        };

        _dbContext.Authors.Add(author);
        await _dbContext.SaveChangesAsync();

        response = req.CreateResponse(HttpStatusCode.Created);
        
        var jsonOptions = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = true
        };

        var authorResponse = new
        {
            data = new
            {
                authorId = author.AuthorId,
                name = author.Name
            }
        };

        var jsonResponse = JsonSerializer.Serialize(authorResponse, jsonOptions);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(jsonResponse);

        return response;
    }

    [Function("UpdateAuthor")]
    public async Task<HttpResponseData> UpdateAuthor([HttpTrigger(AuthorizationLevel.Function, "put", Route = "authors/{id}")] HttpRequestData req, int id)
    {
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var input = JsonSerializer.Deserialize<AuthorDto>(requestBody, new JsonSerializerOptions
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

        var author = await _dbContext.Authors.FindAsync(id);

        if (author == null)
        {
            var errorResponse = new { message = "Author not found." };
            var json = JsonSerializer.Serialize(errorResponse);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(json);
            return response;
        }

        author.Name = input.Name;

        await _dbContext.SaveChangesAsync();

        response = req.CreateResponse(HttpStatusCode.OK);

        var jsonOptions = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = true
        };

        var authorResponse = new
        {
            data = new
            {
                authorId = author.AuthorId,
                name = author.Name
            }
        };

        var jsonResponse = JsonSerializer.Serialize(authorResponse, jsonOptions);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(jsonResponse);

        return response;
    }

    [Function("DeleteAuthor")]
    public async Task<HttpResponseData> DeleteAuthor([HttpTrigger(AuthorizationLevel.Function, "delete", Route = "authors/{id}")] HttpRequestData req, int id)
    {
        var author = await _dbContext.Authors.FindAsync(id);
        var response = req.CreateResponse(HttpStatusCode.NoContent);

        if (author == null)
        {
            response = req.CreateResponse(HttpStatusCode.NotFound);
            var errorResponse = new { message = "Author not found." };
            var json = JsonSerializer.Serialize(errorResponse);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(json);
            return response;
        }

        _dbContext.Authors.Remove(author);
        await _dbContext.SaveChangesAsync();

        response = req.CreateResponse(HttpStatusCode.NoContent);
        return response;
    }
}
