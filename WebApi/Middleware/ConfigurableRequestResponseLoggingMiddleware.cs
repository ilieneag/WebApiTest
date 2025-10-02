using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text;

namespace WebApi.Middleware
{
    public class ConfigurableRequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ConfigurableRequestResponseLoggingMiddleware> _logger;
        private readonly RequestResponseLoggingOptions _options;

        public ConfigurableRequestResponseLoggingMiddleware(
            RequestDelegate next, 
            ILogger<ConfigurableRequestResponseLoggingMiddleware> logger,
            IOptions<RequestResponseLoggingOptions> options)
        {
            _next = next;
            _logger = logger;
            _options = options.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var request = context.Request;
            
            // Skip logging for excluded paths
            if (_options.ExcludePaths.Any(path => request.Path.StartsWithSegments(path)))
            {
                await _next(context);
                return;
            }

            var stopwatch = _options.LogDuration ? Stopwatch.StartNew() : null;
            string requestBody = string.Empty;
            
            // Log incoming request
            if (_options.LogRequestBody && request.ContentLength > 0)
            {
                requestBody = await ReadRequestBodyAsync(request);
            }

            LogIncomingRequest(context, requestBody);

            // Capture response body if logging is enabled
            var originalResponseBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            if (_options.LogResponseBody)
            {
                context.Response.Body = responseBody;
            }

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
                stopwatch?.Stop();
                
                string responseBodyContent = string.Empty;
                if (_options.LogResponseBody && responseBody.Length > 0)
                {
                    responseBody.Seek(0, SeekOrigin.Begin);
                    responseBodyContent = await new StreamReader(responseBody).ReadToEndAsync();
                    responseBody.Seek(0, SeekOrigin.Begin);
                    await responseBody.CopyToAsync(originalResponseBodyStream);
                }
                else if (_options.LogResponseBody)
                {
                    context.Response.Body = originalResponseBodyStream;
                }

                LogOutgoingResponse(context, responseBodyContent, stopwatch?.ElapsedMilliseconds);
            }
        }

        private void LogIncomingRequest(HttpContext context, string requestBody)
        {
            var request = context.Request;
            var logMessage = new StringBuilder();
            
            logMessage.AppendLine($"Incoming Request: {request.Method} {request.Path}");
            
            if (_options.LogClientIP)
            {
                var clientIP = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                logMessage.AppendLine($"Client IP: {clientIP}");
            }

            if (_options.LogHeaders)
            {
                var headers = GetFilteredHeaders(request.Headers);
                if (!string.IsNullOrEmpty(headers))
                {
                    logMessage.AppendLine($"Request Headers: {headers}");
                }
            }

            if (_options.LogRequestBody && !string.IsNullOrEmpty(requestBody))
            {
                logMessage.AppendLine($"Request Body: {requestBody}");
            }

            _logger.LogInformation(logMessage.ToString().TrimEnd());
        }

        private void LogOutgoingResponse(HttpContext context, string responseBody, long? elapsedMilliseconds)
        {
            var request = context.Request;
            var response = context.Response;
            var logMessage = new StringBuilder();
            
            logMessage.Append($"Outgoing Response: {request.Method} {request.Path} responded {response.StatusCode}");
            
            if (_options.LogDuration && elapsedMilliseconds.HasValue)
            {
                logMessage.Append($" in {elapsedMilliseconds}ms");
            }
            
            logMessage.AppendLine();

            if (_options.LogResponseHeaders)
            {
                var headers = GetFilteredHeaders(response.Headers);
                if (!string.IsNullOrEmpty(headers))
                {
                    logMessage.AppendLine($"Response Headers: {headers}");
                }
            }

            if (_options.LogResponseBody && !string.IsNullOrEmpty(responseBody))
            {
                logMessage.AppendLine($"Response Body: {responseBody}");
            }

            var logLevel = GetLogLevel(response.StatusCode);
            _logger.Log(logLevel, logMessage.ToString().TrimEnd());
        }

        private async Task<string> ReadRequestBodyAsync(HttpRequest request)
        {
            request.EnableBuffering();
            var buffer = new byte[Convert.ToInt32(Math.Min(request.ContentLength ?? 0, _options.MaxBodySize))];
            await request.Body.ReadExactlyAsync(buffer, 0, buffer.Length);
            var bodyText = Encoding.UTF8.GetString(buffer);
            request.Body.Seek(0, SeekOrigin.Begin);
            return TruncateIfNeeded(bodyText);
        }

        private string TruncateIfNeeded(string content)
        {
            if (content.Length <= _options.MaxBodySize)
                return content;
            
            return content.Substring(0, _options.MaxBodySize) + "... (truncated)";
        }

        private string GetFilteredHeaders(IHeaderDictionary headers)
        {
            var headerStrings = headers
                .Where(h => !_options.ExcludeHeaders.Contains(h.Key, StringComparer.OrdinalIgnoreCase))
                .Select(h => $"{h.Key}: {string.Join(", ", h.Value.ToArray())}");
            
            return string.Join("; ", headerStrings.ToArray());
        }

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