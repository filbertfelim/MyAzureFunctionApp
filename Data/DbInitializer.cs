using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace MyAzureFunctionApp.Models
{
    public class DbInitializer
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using var context = new AppDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<AppDbContext>>());
            context.Database.Migrate();

            if (context.Authors.Any() || context.Categories.Any() || context.Books.Any() || context.BookCategories.Any())
            {
                return;
            }

            // Seed initial data
            var authors = new[]
            {
                new Author { AuthorId = 1, Name = "Author 1" },
                new Author { AuthorId = 2, Name = "Author 2" },
                new Author { AuthorId = 3, Name = "Author 3" },
                new Author { AuthorId = 4, Name = "Author 4" },
                new Author { AuthorId = 5, Name = "Author 5" }
            };
            context.Authors.AddRange(authors);

            var categories = new[]
            {
                new Category { CategoryId = 1, Name = "Category 1" },
                new Category { CategoryId = 2, Name = "Category 2" },
                new Category { CategoryId = 3, Name = "Category 3" },
                new Category { CategoryId = 4, Name = "Category 4" },
                new Category { CategoryId = 5, Name = "Category 5" }
            };
            context.Categories.AddRange(categories);

            var books = new[]
            {
                new Book { BookId = 1, Title = "Book 1", AuthorId = 1 },
                new Book { BookId = 2, Title = "Book 2", AuthorId = 2 },
                new Book { BookId = 3, Title = "Book 3", AuthorId = 3 },
                new Book { BookId = 4, Title = "Book 4", AuthorId = 4 },
                new Book { BookId = 5, Title = "Book 5", AuthorId = 5 },
                new Book { BookId = 6, Title = "Book 6", AuthorId = 1 },
                new Book { BookId = 7, Title = "Book 7", AuthorId = 2 },
                new Book { BookId = 8, Title = "Book 8", AuthorId = 3 },
                new Book { BookId = 9, Title = "Book 9", AuthorId = 4 },
                new Book { BookId = 10, Title = "Book 10", AuthorId = 5 }
            };
            context.Books.AddRange(books);

            var bookCategories = new[]
            {
                new BookCategory { BookId = 1, CategoryId = 1 },
                new BookCategory { BookId = 1, CategoryId = 2 },
                new BookCategory { BookId = 2, CategoryId = 2 },
                new BookCategory { BookId = 2, CategoryId = 3 },
                new BookCategory { BookId = 3, CategoryId = 3 },
                new BookCategory { BookId = 3, CategoryId = 4 },
                new BookCategory { BookId = 4, CategoryId = 4 },
                new BookCategory { BookId = 4, CategoryId = 5 },
                new BookCategory { BookId = 5, CategoryId = 5 },
                new BookCategory { BookId = 5, CategoryId = 1 },
                new BookCategory { BookId = 6, CategoryId = 1 },
                new BookCategory { BookId = 6, CategoryId = 2 },
                new BookCategory { BookId = 7, CategoryId = 2 },
                new BookCategory { BookId = 7, CategoryId = 3 },
                new BookCategory { BookId = 8, CategoryId = 3 },
                new BookCategory { BookId = 8, CategoryId = 4 },
                new BookCategory { BookId = 9, CategoryId = 4 },
                new BookCategory { BookId = 9, CategoryId = 5 },
                new BookCategory { BookId = 10, CategoryId = 5 },
                new BookCategory { BookId = 10, CategoryId = 1 }
            };
            context.BookCategories.AddRange(bookCategories);

            context.SaveChanges();
        }
    }
}
