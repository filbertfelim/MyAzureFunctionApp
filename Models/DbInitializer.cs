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

            if (context.Books.Any() || context.Categories.Any() || context.Authors.Any())
            {
                return; 
            }
        }
    }
}
