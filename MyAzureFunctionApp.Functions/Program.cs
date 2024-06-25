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
using Microsoft.ApplicationInsights.Extensibility;
using System.Data;
using Npgsql;

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
        services.AddApplicationInsightsTelemetryWorkerService();
        services.Configure<TelemetryConfiguration>(config =>
        {
            config.ConnectionString = configuration["Values:APPLICATIONINSIGHTS_CONNECTION_STRING"];
        });
        var connectionString = configuration["Values:PostgreSqlConnectionString"];
        var connectionTimeout = int.Parse(configuration["Values:ConnectionTimeout"]);
        var commandTimeout = int.Parse(configuration["Values:CommandTimeout"]);

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.CommandTimeout(commandTimeout);
            }));

        services.AddScoped<IDbConnection>(sp => new NpgsqlConnection($"{connectionString};Timeout={connectionTimeout}"));

        services.AddScoped<IDapperUnitOfWork>(sp =>
        {
            var connection = sp.GetRequiredService<IDbConnection>();
            return new DapperUnitOfWork(connection, commandTimeout);
        });

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
    .ConfigureLogging(logging =>
    {
        logging.AddApplicationInsights();
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        DbInitializer.Initialize(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

host.Run();
