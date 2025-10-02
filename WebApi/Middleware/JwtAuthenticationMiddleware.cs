using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using WebApi.Services;

namespace WebApi.Middleware
{
    public class JwtAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtAuthenticationMiddleware> _logger;

        public JwtAuthenticationMiddleware(
            RequestDelegate next, 
            ILogger<JwtAuthenticationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower();
            
            // Skip authentication for public endpoints
            if (IsPublicEndpoint(path))
            {
                _logger.LogInformation("JWT Middleware: Skipping authentication for public endpoint: {Path}", path);
                await _next(context);
                return;
            }

            _logger.LogInformation("JWT Middleware: Checking authentication for protected endpoint: {Path}", path);

            var token = ExtractTokenFromRequest(context.Request);
            
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("JWT Middleware: No token provided for protected endpoint: {Path}", path);
                await HandleUnauthorizedAsync(context, "No authentication token provided");
                return;
            }

            try
            {
                // Get JWT service from request scope
                var jwtService = context.RequestServices.GetRequiredService<IJwtService>();
                
                // Validate the token
                if (!jwtService.ValidateToken(token))
                {
                    _logger.LogWarning("JWT Middleware: Invalid token provided for endpoint: {Path}", path);
                    await HandleUnauthorizedAsync(context, "Invalid authentication token");
                    return;
                }

                // Extract claims from the token and set user context
                var principal = GetPrincipalFromToken(token);
                if (principal != null)
                {
                    context.User = principal;
                    
                    var userId = principal.FindFirst("userId")?.Value;
                    var userEmail = principal.FindFirst(ClaimTypes.Email)?.Value;
                    
                    _logger.LogInformation("JWT Middleware: Token validated successfully for user {UserId} ({Email}) accessing {Path}", 
                        userId, userEmail, path);
                }
                else
                {
                    _logger.LogWarning("JWT Middleware: Could not extract user claims from token for endpoint: {Path}", path);
                    await HandleUnauthorizedAsync(context, "Invalid token claims");
                    return;
                }

                // Continue to the next middleware
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JWT Middleware: Error validating JWT token for endpoint: {Path}", path);
                await HandleUnauthorizedAsync(context, "Authentication error occurred");
            }
        }

        private bool IsPublicEndpoint(string? path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            var publicEndpoints = new[]
            {
                "/api/auth/login",
                "/api/auth/register", 
                "/api/auth/test",
                "/api/auth/refresh-token",
                "/swagger",
                "/health"
            };

            // Check for exact root path match
            if (path.Equals("/", StringComparison.OrdinalIgnoreCase))
                return true;

            var isPublic = publicEndpoints.Any(endpoint => path.StartsWith(endpoint, StringComparison.OrdinalIgnoreCase));
            _logger.LogInformation("JWT Middleware: Checking if {Path} is public. Result: {IsPublic}", path, isPublic);
            
            return isPublic;
        }

        private string? ExtractTokenFromRequest(HttpRequest request)
        {
            // Check Authorization header first
            var authHeader = request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authHeader.Substring("Bearer ".Length).Trim();
            }

            // Check query string as fallback (for debugging purposes)
            if (request.Query.ContainsKey("token"))
            {
                return request.Query["token"].FirstOrDefault();
            }

            return null;
        }

        private ClaimsPrincipal? GetPrincipalFromToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);

                var claims = jwtToken.Claims.ToList();
                var identity = new ClaimsIdentity(claims, "jwt");
                return new ClaimsPrincipal(identity);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract claims from JWT token");
                return null;
            }
        }

        private async Task HandleUnauthorizedAsync(HttpContext context, string message)
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";

            var response = new
            {
                Error = "Unauthorized",
                Message = message,
                StatusCode = 401,
                Timestamp = DateTime.UtcNow,
                Path = context.Request.Path.Value,
                Method = context.Request.Method
            };

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(response, options);
            await context.Response.WriteAsync(json);
        }
    }

    public static class JwtAuthenticationMiddlewareExtensions
    {
        /// <summary>
        /// Adds JWT authentication middleware to the application pipeline
        /// </summary>
        /// <param name="builder">The application builder</param>
        /// <returns>The application builder for chaining</returns>
        public static IApplicationBuilder UseJwtAuthentication(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<JwtAuthenticationMiddleware>();
        }
    }
}