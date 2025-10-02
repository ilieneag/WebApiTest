# Error Handling Middleware Documentation

## Overview

The Error Handling Middleware provides a centralized way to handle all unhandled exceptions in the ASP.NET Core application. It catches exceptions thrown by controllers or other middleware and returns consistent JSON error responses.

## Features

- **Global Exception Handling**: Catches all unhandled exceptions in the application pipeline
- **Consistent Error Format**: Returns standardized JSON error responses
- **Custom Exception Support**: Handles custom business exceptions with appropriate HTTP status codes
- **Environment-Aware**: Shows detailed error information in development, generic messages in production
- **Structured Logging**: Logs all exceptions with appropriate log levels
- **Correlation Tracking**: Includes trace IDs for error correlation and debugging

## Error Response Format

All error responses follow this consistent JSON structure:

```json
{
  "error": "Error category",
  "message": "Human-readable error description", 
  "details": "Additional error details (validation errors, etc.)",
  "stackTrace": "Full stack trace (development only)",
  "source": "Exception source (development only)",
  "traceId": "Unique request identifier",
  "timestamp": "2025-10-02T14:52:26.533Z",
  "path": "/api/users/999",
  "method": "GET"
}
```

### Response Fields

- **error**: Error category (e.g., "Validation failed", "Resource not found")
- **message**: Human-readable description of the error
- **details**: Additional context (validation errors, field-specific messages)
- **stackTrace**: Full exception stack trace (development environment only)
- **source**: Exception source information (development environment only)
- **traceId**: Unique request identifier for correlation
- **timestamp**: UTC timestamp when the error occurred
- **path**: Request path that caused the error
- **method**: HTTP method used

## Supported Exception Types

### 1. ValidationException (400 Bad Request)
Used for input validation errors and business rule violations.

**Example:**
```csharp
throw new ValidationException("User ID must be greater than 0");

// With detailed validation errors
throw new ValidationException("Validation failed", new { 
    email = "Invalid email format",
    firstName = "First name is required" 
});
```

**Response:**
```json
{
  "error": "Validation failed",
  "message": "User ID must be greater than 0",
  "details": {
    "email": "Invalid email format",
    "firstName": "First name is required"
  },
  "traceId": "0HN7GLLMTM47C:00000001",
  "timestamp": "2025-10-02T14:52:26.533Z",
  "path": "/api/users/0",
  "method": "GET"
}
```

### 2. NotFoundException (404 Not Found)
Used when requested resources don't exist.

**Example:**
```csharp
throw new NotFoundException($"User with ID {id} not found");
```

**Response:**
```json
{
  "error": "Resource not found",
  "message": "User with ID 999 not found",
  "traceId": "0HN7GLLMTM47C:00000002",
  "timestamp": "2025-10-02T14:52:26.533Z",
  "path": "/api/users/999",
  "method": "GET"
}
```

### 3. ConflictException (409 Conflict)
Used for resource conflicts (duplicate emails, etc.).

**Example:**
```csharp
throw new ConflictException("A user with this email already exists");
```

**Response:**
```json
{
  "error": "Conflict occurred",
  "message": "A user with this email already exists",
  "traceId": "0HN7GLLMTM47C:00000003",
  "timestamp": "2025-10-02T14:52:26.533Z",
  "path": "/api/users",
  "method": "POST"
}
```

### 4. UnauthorizedException (401 Unauthorized)
Used for authentication and authorization failures.

**Example:**
```csharp
throw new UnauthorizedException("Access denied for this resource");
```

**Response:**
```json
{
  "error": "Unauthorized access",
  "message": "Access denied for this resource",
  "traceId": "0HN7GLLMTM47C:00000004",
  "timestamp": "2025-10-02T14:52:26.533Z",
  "path": "/api/users/1",
  "method": "PUT"
}
```

### 5. ArgumentException (400 Bad Request)
Handles standard .NET ArgumentException.

**Response:**
```json
{
  "error": "Invalid argument",
  "message": "Parameter cannot be null",
  "traceId": "0HN7GLLMTM47C:00000005",
  "timestamp": "2025-10-02T14:52:26.533Z",
  "path": "/api/users",
  "method": "POST"
}
```

### 6. InvalidOperationException (400 Bad Request)
Handles standard .NET InvalidOperationException.

**Response:**
```json
{
  "error": "Invalid operation",
  "message": "Operation is not valid in current state",
  "traceId": "0HN7GLLMTM47C:00000006",
  "timestamp": "2025-10-02T14:52:26.533Z",
  "path": "/api/users/1",
  "method": "DELETE"
}
```

### 7. Generic Exceptions (500 Internal Server Error)
Handles all other unhandled exceptions.

**Development Response:**
```json
{
  "error": "Internal server error",
  "message": "Detailed exception message",
  "stackTrace": "Full stack trace...",
  "source": "ExceptionSource",
  "traceId": "0HN7GLLMTM47C:00000007",
  "timestamp": "2025-10-02T14:52:26.533Z",
  "path": "/api/users",
  "method": "GET"
}
```

**Production Response:**
```json
{
  "error": "Internal server error",
  "message": "An error occurred while processing your request",
  "traceId": "0HN7GLLMTM47C:00000007",
  "timestamp": "2025-10-02T14:52:26.533Z",
  "path": "/api/users",
  "method": "GET"
}
```

## Implementation

### 1. Register Middleware
Add the error handling middleware early in the pipeline in `Program.cs`:

```csharp
app.UseErrorHandling(); // Must be first or very early
```

### 2. Use Custom Exceptions in Controllers
Replace manual error handling with custom exceptions:

```csharp
// Instead of this:
if (id <= 0)
{
    return BadRequest(new { message = "Invalid user ID" });
}

// Use this:
if (id <= 0)
{
    throw new ValidationException("User ID must be greater than 0");
}
```

### 3. Controller Simplification
Controllers become much cleaner without try-catch blocks:

```csharp
[HttpGet("{id}")]
public async Task<ActionResult<User>> GetUser(int id)
{
    if (id <= 0)
        throw new ValidationException("User ID must be greater than 0");

    var user = await _userService.GetUserByIdAsync(id);
    
    if (user == null)
        throw new NotFoundException($"User with ID {id} not found");

    return Ok(user);
}
```

## Environment-Specific Behavior

### Development Environment
- Shows detailed exception messages
- Includes full stack traces
- Includes exception source information
- Pretty-prints JSON responses

### Production Environment
- Shows generic error messages for security
- Excludes stack traces and sensitive information
- Compact JSON responses
- Logs detailed errors for debugging

## Security Considerations

### Information Disclosure
- **Production**: Generic error messages prevent information leakage
- **Development**: Detailed errors help with debugging
- **Sensitive Data**: Never expose passwords, tokens, or PII in error messages

### Logging
- All exceptions are logged with full details regardless of environment
- Use correlation IDs to trace issues without exposing sensitive information
- Log levels appropriate to error severity

## Testing the Error Handling

### Using the Test Endpoint
The `UsersController` includes a special test endpoint to demonstrate different error types:

```http
GET /api/users/test-error/{errorType}
```

Supported error types:
- `validation` - ValidationException
- `notfound` - NotFoundException  
- `conflict` - ConflictException
- `unauthorized` - UnauthorizedException
- `argument` - ArgumentException
- `invalidoperation` - InvalidOperationException
- `generic` - Generic Exception

### Example Test Requests

```bash
# Test validation error
curl http://localhost:5233/api/users/test-error/validation

# Test not found error  
curl http://localhost:5233/api/users/test-error/notfound

# Test generic exception
curl http://localhost:5233/api/users/test-error/generic
```

## Best Practices

### 1. Use Appropriate Exception Types
Choose the right exception type for your error scenario:

```csharp
// User input validation
throw new ValidationException("Email format is invalid");

// Resource not found
throw new NotFoundException("User not found");

// Business rule violation
throw new ConflictException("Email already exists");

// Authorization failure
throw new UnauthorizedException("Insufficient permissions");
```

### 2. Provide Meaningful Messages
Write clear, actionable error messages:

```csharp
// Good
throw new ValidationException("User ID must be greater than 0");

// Bad
throw new ValidationException("Invalid input");
```

### 3. Include Relevant Details
For validation errors, include field-specific information:

```csharp
var errors = new {
    email = "Email format is invalid",
    firstName = "First name is required",
    age = "Age must be between 18 and 120"
};
throw new ValidationException("Validation failed", errors);
```

### 4. Log Appropriately
The middleware automatically logs exceptions, but you can add context:

```csharp
try
{
    // Business logic
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to process user {UserId}", userId);
    throw; // Re-throw to let middleware handle response
}
```

## Troubleshooting

### Common Issues

1. **Middleware Order**: Error handling must be registered early in the pipeline
2. **Custom Exceptions**: Make sure to use the provided custom exception types
3. **Model Validation**: Convert ModelState errors to ValidationException for consistency

### Debugging

1. **Check Logs**: All exceptions are logged with full details
2. **Use Trace IDs**: Include trace ID when reporting issues
3. **Development Mode**: Enable detailed error information in development

### Monitoring

1. **Error Rates**: Monitor exception frequency and types
2. **Response Times**: Track impact on application performance  
3. **Log Analysis**: Use structured logging for error analysis

## Future Enhancements

Consider implementing these additional features:

1. **Rate Limiting**: Prevent error-based DoS attacks
2. **Error Metrics**: Export error statistics to monitoring systems
3. **Custom Error Pages**: Serve custom error pages for browser requests
4. **Retry Logic**: Automatic retry for transient failures
5. **Circuit Breaker**: Prevent cascading failures
6. **Error Notifications**: Alert administrators of critical errors
7. **Error Recovery**: Automatic recovery strategies for certain error types