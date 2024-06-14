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

public class BooksFunction
{
    private readonly AppDbContext _dbContext;

    public BooksFunction(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [Function("GetBooks")]
    public async Task<HttpResponseData> GetBooks([HttpTrigger(AuthorizationLevel.Function, "get", Route = "books")] HttpRequestData req)
    {
        var books = await _dbContext.Books
            .Include(b => b.Author)
            .Include(b => b.BookCategories)
                .ThenInclude(bc => bc.Category)
            .ToListAsync();

        var bookDtos = books.Select(b => new BookDto
        {
            BookId = b.BookId,
            Title = b.Title,
            Author = new AuthorDto
            {
                AuthorId = b.Author.AuthorId,
                Name = b.Author.Name
            },
            BookCategories = b.BookCategories.Select(bc => new BookCategoryDto
            {
                BookId = bc.BookId,
                Category = new CategoryDto
                {
                    CategoryId = bc.Category.CategoryId,
                    Name = bc.Category.Name
                }
            }).ToList()
        }).ToList();

        var response = req.CreateResponse(HttpStatusCode.OK);

        var jsonOptions = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = true
        };

        var jsonResponse = new { data = bookDtos };
        var json = JsonSerializer.Serialize(jsonResponse, jsonOptions);

        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(json);

        return response;
    }

    [Function("GetBookById")]
    public async Task<HttpResponseData> GetBookById([HttpTrigger(AuthorizationLevel.Function, "get", Route = "books/{id}")] HttpRequestData req, int id)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        var book = await _dbContext.Books
            .Include(b => b.Author)
            .Include(b => b.BookCategories)
                .ThenInclude(bc => bc.Category)
            .FirstOrDefaultAsync(b => b.BookId == id);

        if (book == null)
        {
            response = req.CreateResponse(HttpStatusCode.NotFound);
            var errorResponse = new { message = $"Book with ID {id} not found." };
            var jsonError = JsonSerializer.Serialize(errorResponse);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(jsonError);
            return response;
        }

        var bookDto = new BookDto
        {
            BookId = book.BookId,
            Title = book.Title,
            Author = new AuthorDto
            {
                AuthorId = book.Author.AuthorId,
                Name = book.Author.Name
            },
            BookCategories = book.BookCategories.Select(bc => new BookCategoryDto
            {
                BookId = bc.BookId,
                Category = new CategoryDto
                {
                    CategoryId = bc.Category.CategoryId,
                    Name = bc.Category.Name
                }
            }).ToList()
        };

        response = req.CreateResponse(HttpStatusCode.OK);

        var jsonOptions = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = true
        };

        var jsonResponse = new { data = bookDto };
        var json = JsonSerializer.Serialize(jsonResponse, jsonOptions);

        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(json);

        return response;
    }

    [Function("CreateBook")]
    public async Task<HttpResponseData> CreateBook([HttpTrigger(AuthorizationLevel.Function, "post", Route = "books")] HttpRequestData req)
    {
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var response = req.CreateResponse(HttpStatusCode.BadRequest);
        
        var input = JsonSerializer.Deserialize<CreateUpdateBookDto>(requestBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (input == null || input.CategoryIds == null || !input.CategoryIds.Any())
        {
            response = req.CreateResponse(HttpStatusCode.BadRequest);
            var errorResponse = new { message = "Invalid input data." };
            var json = JsonSerializer.Serialize(errorResponse);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(json);
            return response;
        }

        // Check if author exists
        var author = await _dbContext.Authors.FindAsync(input.AuthorId);
        if (author == null)
        {
            response = req.CreateResponse(HttpStatusCode.BadRequest);
            var errorResponse = new { message = "Author does not exist." };
            var json = JsonSerializer.Serialize(errorResponse);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(json);
            return response;
        }

        // Check if all categories exist
        var categories = await _dbContext.Categories.Where(c => input.CategoryIds.Contains(c.CategoryId)).ToListAsync();
        if (categories.Count != input.CategoryIds.Count)
        {
            response = req.CreateResponse(HttpStatusCode.BadRequest);
            var errorResponse = new { message = "One or more categories do not exist." };
            var json = JsonSerializer.Serialize(errorResponse);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(json);
            return response;
        }

        var book = new Book
        {
            Title = input.Title,
            AuthorId = input.AuthorId,
            BookCategories = input.CategoryIds.Select(id => new BookCategory
            {
                CategoryId = id
            }).ToList()
        };

        _dbContext.Books.Add(book);
        await _dbContext.SaveChangesAsync();

        // Retrieve the created book with related data
        var createdBook = await _dbContext.Books
            .Include(b => b.Author)
            .Include(b => b.BookCategories)
                .ThenInclude(bc => bc.Category)
            .FirstOrDefaultAsync(b => b.BookId == book.BookId);

        response = req.CreateResponse(HttpStatusCode.Created);

        var jsonOptions = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = true
        };

        var bookResponse = new
        {
            data = new
            {
                bookId = createdBook.BookId,
                title = createdBook.Title,
                authorId = createdBook.AuthorId,
                author = new
                {
                    authorId = createdBook.Author.AuthorId,
                    name = createdBook.Author.Name
                },
                bookCategories = createdBook.BookCategories.Select(bc => new
                {
                    bookId = bc.BookId,
                    book = (object)null,
                    categoryId = bc.CategoryId,
                    category = new
                    {
                        categoryId = bc.Category.CategoryId,
                        name = bc.Category.Name
                    }
                }).ToList()
            }
        };

        var jsonResponse = JsonSerializer.Serialize(bookResponse, jsonOptions);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(jsonResponse);

        return response;
    }


    [Function("UpdateBook")]
    public async Task<HttpResponseData> UpdateBook([HttpTrigger(AuthorizationLevel.Function, "put", Route = "books/{id}")] HttpRequestData req, int id)
    {
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var response = req.CreateResponse(HttpStatusCode.BadRequest);
        
        var input = JsonSerializer.Deserialize<CreateUpdateBookDto>(requestBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (input == null || input.CategoryIds == null || !input.CategoryIds.Any())
        {
            response = req.CreateResponse(HttpStatusCode.BadRequest);
            var errorResponse = new { message = "Invalid input data." };
            var json = JsonSerializer.Serialize(errorResponse);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(json);
            return response;
        }

        var book = await _dbContext.Books
            .Include(b => b.BookCategories)
            .FirstOrDefaultAsync(b => b.BookId == id);

        if (book == null)
        {
            response = req.CreateResponse(HttpStatusCode.NotFound);
            var errorResponse = new { message = "Book not found." };
            var json = JsonSerializer.Serialize(errorResponse);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(json);
            return response;
        }

        // Check if author exists
        var author = await _dbContext.Authors.FindAsync(input.AuthorId);
        if (author == null)
        {
            response = req.CreateResponse(HttpStatusCode.BadRequest);
            var errorResponse = new { message = "Author does not exist." };
            var json = JsonSerializer.Serialize(errorResponse);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(json);
            return response;
        }

        // Check if all categories exist
        var categories = await _dbContext.Categories.Where(c => input.CategoryIds.Contains(c.CategoryId)).ToListAsync();
        if (categories.Count != input.CategoryIds.Count)
        {
            response = req.CreateResponse(HttpStatusCode.BadRequest);
            var errorResponse = new { message = "One or more categories do not exist." };
            var json = JsonSerializer.Serialize(errorResponse);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(json);
            return response;
        }

        book.Title = input.Title;
        book.AuthorId = input.AuthorId;
        book.BookCategories = input.CategoryIds.Select(id => new BookCategory
        {
            BookId = book.BookId,
            CategoryId = id
        }).ToList();

        await _dbContext.SaveChangesAsync();

        // Retrieve the updated book with related data
        var updatedBook = await _dbContext.Books
            .Include(b => b.Author)
            .Include(b => b.BookCategories)
                .ThenInclude(bc => bc.Category)
            .FirstOrDefaultAsync(b => b.BookId == book.BookId);

        response = req.CreateResponse(HttpStatusCode.OK);

        var jsonOptions = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = true
        };

        var bookResponse = new
        {
            data = new
            {
                bookId = updatedBook.BookId,
                title = updatedBook.Title,
                authorId = updatedBook.AuthorId,
                author = new
                {
                    authorId = updatedBook.Author.AuthorId,
                    name = updatedBook.Author.Name
                },
                bookCategories = updatedBook.BookCategories.Select(bc => new
                {
                    bookId = bc.BookId,
                    book = (object)null, // or any other relevant book information if needed
                    categoryId = bc.CategoryId,
                    category = new
                    {
                        categoryId = bc.Category.CategoryId,
                        name = bc.Category.Name
                    }
                }).ToList()
            }
        };

        var jsonResponse = JsonSerializer.Serialize(bookResponse, jsonOptions);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(jsonResponse);

        return response;
    }

    [Function("DeleteBook")]
    public async Task<HttpResponseData> DeleteBook([HttpTrigger(AuthorizationLevel.Function, "delete", Route = "books/{id}")] HttpRequestData req, int id)
    {
        var book = await _dbContext.Books.FindAsync(id);
        var response = req.CreateResponse(HttpStatusCode.NoContent);

        if (book == null)
        {
            response = req.CreateResponse(HttpStatusCode.NotFound);
            var errorResponse = new { message = "Book not found." };
            var json = JsonSerializer.Serialize(errorResponse);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(json);
            return response;
        }

        _dbContext.Books.Remove(book);
        await _dbContext.SaveChangesAsync();

        response = req.CreateResponse(HttpStatusCode.NoContent);
        return response;
    }
}
