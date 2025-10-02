using System.Diagnostics;

namespace WebApi.Middleware
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var request = context.Request;
            
            // Log incoming request
            _logger.LogInformation(
                "Incoming Request: {Method} {Path} from {RemoteIpAddress}",
                request.Method,
                request.Path,
                context.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
            );

            try
            {
                // Call the next middleware in the pipeline
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Exception occurred during request processing: {Method} {Path}",
                    request.Method,
                    request.Path
                );
                throw; // Re-throw to let other middleware handle it
            }
            finally
            {
                stopwatch.Stop();
                var response = context.Response;
                
                // Log outgoing response
                var logLevel = GetLogLevel(response.StatusCode);
                _logger.Log(logLevel,
                    "Outgoing Response: {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds}ms",
                    request.Method,
                    request.Path,
                    response.StatusCode,
                    stopwatch.ElapsedMilliseconds
                );
            }
        }

        /// <summary>
        /// Determines the appropriate log level based on HTTP status code
        /// </summary>
        /// <param name="statusCode">HTTP status code</param>
        /// <returns>Appropriate log level</returns>
        private static LogLevel GetLogLevel(int statusCode)
        {
            return statusCode switch
            {
                >= 500 => LogLevel.Error,      // Server errors
                >= 400 => LogLevel.Warning,    // Client errors
                >= 300 => LogLevel.Information, // Redirects
                _ => LogLevel.Information       // Success responses
            };
        }
    }
}