using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace MyAzureFunctionApp.Functions
{
    public static class SwaggerUIFunction
    {
        [Function("ServeSwaggerUI")]
        public static async Task<HttpResponseData> ServeSwaggerUI(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "swagger/ui/{*path}")] HttpRequestData req,
            string path,
            FunctionContext context)
        {
            var logger = context.GetLogger("ServeSwaggerUI");
            var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "swagger");
            var fullPath = Path.Combine(root, path ?? "index.html");

            logger.LogInformation($"Serving Swagger UI file: {fullPath}");

            if (!File.Exists(fullPath))
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync("File not found.");
                return notFoundResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteBytesAsync(await File.ReadAllBytesAsync(fullPath));
            return response;
        }
    }
}
