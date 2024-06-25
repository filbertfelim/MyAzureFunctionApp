using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace MyAzureFunctionApp.Helpers
{
    public abstract class AuthenticatedFunctionBase
    {
        private readonly string _validAudience;
        protected readonly ILogger _logger;

        protected AuthenticatedFunctionBase(IConfiguration configuration, ILogger logger)
        {
            _validAudience = configuration["AzureAd:ClientId"];
            _logger = logger;
        }

        protected IActionResult ValidateToken(HttpRequest req)
        {
            var authHeader = req.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                _logger.LogWarning("AuthValidation: Missing or invalid Authorization header.");
                return new JsonResult(new { Message = "Missing or invalid Authorization header" }) { StatusCode = StatusCodes.Status401Unauthorized };
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var handler = new JwtSecurityTokenHandler();

            try
            {
                var jwtToken = handler.ReadJwtToken(token);
                var aud = jwtToken.Claims.FirstOrDefault(c => c.Type == "aud")?.Value;

                if (aud != $"api://{_validAudience}")
                {
                    _logger.LogWarning("AuthValidation: Invalid audience in token.");
                    return new JsonResult(new { Message = "Invalid audience" }) { StatusCode = StatusCodes.Status401Unauthorized };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AuthValidation: Invalid token.");
                return new JsonResult(new { Message = "Invalid token" }) { StatusCode = StatusCodes.Status401Unauthorized };
            }

            return null; // Return null if the token is valid
        }
    }
}
