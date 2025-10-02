# Request/Response Logging Middleware Documentation

## Overview

This middleware provides comprehensive HTTP request and response logging capabilities for ASP.NET Core applications. It offers three different levels of logging to suit different environments and requirements.

## Features

- **HTTP Method & Path Logging**: Captures HTTP method (GET, POST, etc.) and request path
- **Response Status Code**: Logs HTTP status codes with appropriate log levels
- **Request Duration**: Measures and logs request processing time
- **Client IP Address**: Captures client IP for security auditing
- **Header Logging**: Configurable request/response header logging
- **Body Logging**: Optional request/response body logging for debugging
- **Error Handling**: Proper exception logging without breaking the request pipeline
- **Performance Optimized**: Minimal overhead with configurable options
- **Security Conscious**: Excludes sensitive headers by default

## Middleware Types

### 1. RequestResponseLoggingMiddleware (Basic)

The simplest middleware that logs essential request/response information.

**What it logs:**
- HTTP method and request path
- Response status code
- Request duration
- Client IP address

**Usage:**
```csharp
app.UseRequestResponseLogging();
```

**Sample Output:**
```
info: Incoming Request: GET /api/users from 127.0.0.1
info: Outgoing Response: GET /api/users responded 200 in 45ms
```

### 2. DetailedRequestResponseLoggingMiddleware (Development)

Enhanced middleware with optional request/response body logging.

**What it logs:**
- All basic logging features
- Request headers (filtered)
- Response headers (filtered)
- Request body (optional)
- Response body (optional)

**Usage:**
```csharp
app.UseDetailedRequestResponseLogging(
    logRequestBody: true,
    logResponseBody: true,
    maxBodySize: 8192
);
```

**Sample Output:**
```
info: Incoming Request: GET /api/users from 127.0.0.1
      Headers: Accept: application/json; User-Agent: curl/7.68.0
info: Outgoing Response: GET /api/users responded 200 in 45ms
      Response Headers: Content-Type: application/json; charset=utf-8
      Response Body: [{"id":1,"firstName":"John"...}]
```

### 3. ConfigurableRequestResponseLoggingMiddleware (Production)

Fully configurable middleware with fine-grained control over what gets logged.

**Configuration Options:**
```csharp
public class RequestResponseLoggingOptions
{
    public bool LogRequestBody { get; set; } = false;
    public bool LogResponseBody { get; set; } = false;
    public int MaxBodySize { get; set; } = 4096;
    public bool LogHeaders { get; set; } = true;
    public bool LogResponseHeaders { get; set; } = true;
    public bool LogDuration { get; set; } = true;
    public bool LogClientIP { get; set; } = true;
    public List<string> ExcludePaths { get; set; } = new List<string>();
    public List<string> ExcludeHeaders { get; set; } = new List<string> 
    { 
        "Authorization", 
        "Cookie", 
        "Set-Cookie" 
    };
}
```

**Usage:**
```csharp
builder.Services.Configure<RequestResponseLoggingOptions>(options =>
{
    options.LogRequestBody = false;
    options.LogResponseBody = false;
    options.LogDuration = true;
    options.ExcludePaths = new List<string> { "/health", "/metrics" };
});

app.UseConfigurableRequestResponseLogging();
```

## Log Levels

The middleware automatically assigns appropriate log levels based on HTTP status codes:

- **Information** (200-399): Successful requests and redirects
- **Warning** (400-499): Client errors (Bad Request, Not Found, etc.)
- **Error** (500+): Server errors

## Security Considerations

### Default Excluded Headers
The middleware excludes sensitive headers by default:
- `Authorization`
- `Cookie`
- `Set-Cookie`

### Body Logging Security
- **Request Body Logging**: Disabled by default to prevent logging sensitive data (passwords, tokens)
- **Response Body Logging**: Disabled by default to prevent logging sensitive information
- **Size Limits**: Configurable maximum body size to prevent memory issues

### Recommended Production Settings
```csharp
builder.Services.Configure<RequestResponseLoggingOptions>(options =>
{
    options.LogRequestBody = false;          // Security: Don't log request bodies
    options.LogResponseBody = false;         // Security: Don't log response bodies
    options.LogHeaders = true;               // Useful for debugging
    options.LogDuration = true;              // Performance monitoring
    options.LogClientIP = true;              // Security auditing
    options.ExcludePaths = new List<string> 
    { 
        "/health", 
        "/metrics", 
        "/favicon.ico" 
    };
    options.ExcludeHeaders = new List<string> 
    { 
        "Authorization", 
        "Cookie", 
        "Set-Cookie", 
        "X-API-Key" 
    };
});
```

## Environment-Specific Configuration

### Development Configuration
```csharp
if (app.Environment.IsDevelopment())
{
    app.UseDetailedRequestResponseLogging(
        logRequestBody: true,
        logResponseBody: true,
        maxBodySize: 16384
    );
}
```

### Production Configuration
```csharp
if (app.Environment.IsProduction())
{
    builder.Services.Configure<RequestResponseLoggingOptions>(options =>
    {
        options.LogRequestBody = false;
        options.LogResponseBody = false;
        options.ExcludePaths = new List<string> { "/health", "/metrics" };
    });
    app.UseConfigurableRequestResponseLogging();
}
```

## Performance Impact

### Minimal Overhead
- Basic middleware: ~1-2ms overhead per request
- Detailed middleware: ~3-5ms overhead per request (without body logging)
- Body logging: Additional overhead depends on body size

### Optimization Tips
1. **Disable body logging in production** for better performance
2. **Use path exclusions** for high-frequency endpoints like health checks
3. **Set appropriate MaxBodySize** to prevent memory issues
4. **Configure log levels** appropriately in production

## Integration with ASP.NET Core Logging

The middleware integrates seamlessly with ASP.NET Core's logging infrastructure:

### appsettings.json Configuration
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "WebApi.Middleware.RequestResponseLoggingMiddleware": "Information",
      "WebApi.Middleware.DetailedRequestResponseLoggingMiddleware": "Information",
      "WebApi.Middleware.ConfigurableRequestResponseLoggingMiddleware": "Information"
    }
  }
}
```

### Structured Logging
The middleware supports structured logging with named parameters:
```csharp
_logger.LogInformation(
    "Incoming Request: {Method} {Path} from {RemoteIpAddress}",
    request.Method,
    request.Path,
    clientIP
);
```

## Testing the Middleware

### Using the Logging.http File
The project includes a `Logging.http` file with test requests to verify logging functionality:

1. **GET requests** - Test basic logging
2. **POST requests** - Test request body logging
3. **Error scenarios** - Test error-level logging
4. **Various status codes** - Test log level assignment

### Sample Test Commands
```bash
# Test basic GET request
wget -qO- "http://localhost:5229/api/users"

# Test POST with body
wget -qO- --post-data='{"firstName":"Test","lastName":"User","email":"test@example.com"}' \
     --header="Content-Type: application/json" \
     "http://localhost:5229/api/users"

# Test error scenario
wget -qO- "http://localhost:5229/api/users/999"
```

## Troubleshooting

### Common Issues

1. **Middleware not logging**: Ensure middleware is registered early in the pipeline
2. **Missing logs**: Check log level configuration in appsettings.json
3. **Performance issues**: Disable body logging and exclude high-frequency paths
4. **Memory issues**: Reduce MaxBodySize or disable body logging

### Debugging
Enable debug logging to see detailed middleware behavior:
```json
{
  "Logging": {
    "LogLevel": {
      "WebApi.Middleware": "Debug"
    }
  }
}
```

## Best Practices

1. **Register early** in the middleware pipeline for complete request coverage
2. **Use environment-specific configuration** to balance detail vs. performance
3. **Exclude sensitive headers** to maintain security
4. **Monitor log volume** in production to prevent storage issues
5. **Use structured logging** for better log analysis and searching
6. **Test thoroughly** in development with body logging enabled
7. **Monitor performance** impact in production environments

## Future Enhancements

Consider implementing these additional features:

1. **Correlation IDs**: Add unique request identifiers for tracing
2. **Async logging**: Use background logging for better performance
3. **Log filtering**: Advanced filtering based on user roles or request characteristics
4. **Metrics integration**: Export timing data to metrics systems
5. **Log rotation**: Automatic log cleanup for long-running applications
6. **Custom formatters**: Support for different log output formats
7. **Rate limiting**: Prevent log flooding from high-frequency requests