using System.Net;
using System.Text.Json;

namespace WebApi.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IWebHostEnvironment environment)
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
            // Don't handle the exception if the response has already started
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("Cannot handle exception, response has already started");
                return;
            }

            context.Response.Clear();
            context.Response.ContentType = "application/json";
            
            var response = new ErrorResponse();
            
            switch (exception)
            {
                case ValidationException validationEx:
                    response.Error = "Validation failed";
                    response.Message = validationEx.Message;
                    response.Details = validationEx.Errors;
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;
                    
                case NotFoundException notFoundEx:
                    response.Error = "Resource not found";
                    response.Message = notFoundEx.Message;
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    break;
                    
                case UnauthorizedException unauthorizedEx:
                    response.Error = "Unauthorized access";
                    response.Message = unauthorizedEx.Message;
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    break;
                    
                case ConflictException conflictEx:
                    response.Error = "Conflict occurred";
                    response.Message = conflictEx.Message;
                    context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                    break;
                    
                case ArgumentException argEx:
                    response.Error = "Invalid argument";
                    response.Message = argEx.Message;
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;
                    
                case InvalidOperationException invalidOpEx:
                    response.Error = "Invalid operation";
                    response.Message = invalidOpEx.Message;
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;
                    
                default:
                    response.Error = "Internal server error";
                    response.Message = _environment.IsDevelopment() 
                        ? exception.Message 
                        : "An error occurred while processing your request";
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    break;
            }

            // Add additional information in development environment
            if (_environment.IsDevelopment())
            {
                response.StackTrace = exception.StackTrace;
                response.Source = exception.Source;
            }

            // Add correlation ID if available
            if (context.TraceIdentifier != null)
            {
                response.TraceId = context.TraceIdentifier;
            }

            response.Timestamp = DateTime.UtcNow;
            response.Path = context.Request.Path;
            response.Method = context.Request.Method;

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = _environment.IsDevelopment()
            });

            try
            {
                await context.Response.WriteAsync(jsonResponse);
            }
            catch (InvalidOperationException)
            {
                // Response stream might be closed, log the error but don't throw
                _logger.LogWarning("Failed to write error response, response stream may be closed");
            }
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

    // Custom exception classes for better error categorization
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