using System.Net;
using System.Text.Json;

namespace WebApi.Middleware
{
    public class SimpleErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SimpleErrorHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public SimpleErrorHandlingMiddleware(RequestDelegate next, ILogger<SimpleErrorHandlingMiddleware> logger, IWebHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred while processing the request");
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Early exit if response has already started
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("Cannot handle exception, response has already started");
                return;
            }

            var response = context.Response;
            response.ContentType = "application/json";

            var errorResponse = CreateErrorResponse(context, exception);
            response.StatusCode = GetStatusCode(exception);

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = _environment.IsDevelopment()
            };

            var json = JsonSerializer.Serialize(errorResponse, options);

            try
            {
                await response.WriteAsync(json);
            }
            catch (Exception writeEx)
            {
                _logger.LogWarning(writeEx, "Failed to write error response");
            }
        }

        private ErrorResponse CreateErrorResponse(HttpContext context, Exception exception)
        {
            var response = new ErrorResponse
            {
                Timestamp = DateTime.UtcNow,
                Path = context.Request.Path,
                Method = context.Request.Method,
                TraceId = context.TraceIdentifier
            };

            switch (exception)
            {
                case ValidationException validationEx:
                    response.Error = "Validation failed";
                    response.Message = validationEx.Message;
                    response.Details = validationEx.Errors;
                    break;

                case NotFoundException notFoundEx:
                    response.Error = "Resource not found";
                    response.Message = notFoundEx.Message;
                    break;

                case UnauthorizedException unauthorizedEx:
                    response.Error = "Unauthorized access";
                    response.Message = unauthorizedEx.Message;
                    break;

                case ConflictException conflictEx:
                    response.Error = "Conflict occurred";
                    response.Message = conflictEx.Message;
                    break;

                case ArgumentException argEx:
                    response.Error = "Invalid argument";
                    response.Message = argEx.Message;
                    break;

                case InvalidOperationException invalidOpEx:
                    response.Error = "Invalid operation";
                    response.Message = invalidOpEx.Message;
                    break;

                default:
                    response.Error = "Internal server error";
                    response.Message = _environment.IsDevelopment()
                        ? exception.Message
                        : "An error occurred while processing your request";
                    break;
            }

            // Add development-only information
            if (_environment.IsDevelopment())
            {
                response.StackTrace = exception.StackTrace;
                response.Source = exception.Source;
            }

            return response;
        }

        private static int GetStatusCode(Exception exception)
        {
            return exception switch
            {
                ValidationException => (int)HttpStatusCode.BadRequest,
                NotFoundException => (int)HttpStatusCode.NotFound,
                UnauthorizedException => (int)HttpStatusCode.Unauthorized,
                ConflictException => (int)HttpStatusCode.Conflict,
                ArgumentException => (int)HttpStatusCode.BadRequest,
                InvalidOperationException => (int)HttpStatusCode.BadRequest,
                _ => (int)HttpStatusCode.InternalServerError
            };
        }
    }

    public class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public object? Details { get; set; }
        public string? StackTrace { get; set; }
        public string? Source { get; set; }
        public string? TraceId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Path { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
    }

    public class ValidationException : Exception
    {
        public object? Errors { get; }

        public ValidationException(string message) : base(message)
        {
        }

        public ValidationException(string message, object errors) : base(message)
        {
            Errors = errors;
        }

        public ValidationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message)
        {
        }

        public NotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class UnauthorizedException : Exception
    {
        public UnauthorizedException(string message) : base(message)
        {
        }

        public UnauthorizedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message)
        {
        }

        public ConflictException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}