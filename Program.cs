using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyAzureFunctionApp.Models;
using MyAzureFunctionApp.Repositories;
using MyAzureFunctionApp.Services;
using MyAzureFunctionApp.Validators;
using FluentValidation;
using MyAzureFunctionApp.Models.DTOs;
using System.Text.Json.Serialization;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration((context, builder) =>
    {
        builder.SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
               .AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;
        var connectionString = configuration["Values:PostgreSqlConnectionString"];
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Add repositories
        services.AddScoped<IAuthorRepository, AuthorRepository>();
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();

        // Add services
        services.AddScoped<IAuthorService, AuthorService>();
        services.AddScoped<IBookService, BookService>();
        services.AddScoped<ICategoryService, CategoryService>();

        // Add validators
        services.AddScoped<IValidator<AuthorDto>, AuthorDtoValidator>();
        services.AddScoped<IValidator<BookDto>, BookDtoValidator>();
        services.AddScoped<IValidator<CategoryDto>, CategoryDtoValidator>();

        // Add AutoMapper
        services.AddAutoMapper(typeof(MappingProfile));

        // Configure JSON options
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.WriteIndented = true;
            });
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        DbInitializer.Initialize(services);  // Seed the database
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

host.Run();
