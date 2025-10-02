using System.Diagnostics;
using System.Text;

namespace WebApi.Middleware
{
    public class DetailedRequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<DetailedRequestResponseLoggingMiddleware> _logger;
        private readonly bool _logRequestBody;
        private readonly bool _logResponseBody;
        private readonly int _maxBodySize;

        public DetailedRequestResponseLoggingMiddleware(
            RequestDelegate next, 
            ILogger<DetailedRequestResponseLoggingMiddleware> logger,
            bool logRequestBody = false,
            bool logResponseBody = false,
            int maxBodySize = 4096)
        {
            _next = next;
            _logger = logger;
            _logRequestBody = logRequestBody;
            _logResponseBody = logResponseBody;
            _maxBodySize = maxBodySize;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var request = context.Request;
            string requestBody = string.Empty;
            
            // Log incoming request with optional body
            if (_logRequestBody && request.ContentLength > 0)
            {
                requestBody = await ReadRequestBodyAsync(request);
                
                _logger.LogInformation(
                    "Incoming Request: {Method} {Path} from {RemoteIpAddress}\n" +
                    "Headers: {Headers}\n" +
                    "Body: {RequestBody}",
                    request.Method,
                    request.Path,
                    context.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    GetHeadersAsString(request.Headers),
                    requestBody
                );
            }
            else
            {
                _logger.LogInformation(
                    "Incoming Request: {Method} {Path} from {RemoteIpAddress}\n" +
                    "Headers: {Headers}",
                    request.Method,
                    request.Path,
                    context.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    GetHeadersAsString(request.Headers)
                );
            }

            // Only capture response body if logging is enabled AND no exceptions occur
            Stream? originalResponseBodyStream = null;
            MemoryStream? responseBody = null;
            bool exceptionOccurred = false;

            if (_logResponseBody)
            {
                originalResponseBodyStream = context.Response.Body;
                responseBody = new MemoryStream();
                context.Response.Body = responseBody;
            }

            try
            {
                // Call the next middleware in the pipeline
                await _next(context);
            }
            catch (Exception ex)
            {
                exceptionOccurred = true;
                
                // Restore original response stream for error handling middleware
                if (originalResponseBodyStream != null)
                {
                    context.Response.Body = originalResponseBodyStream;
                }

                _logger.LogError(ex, 
                    "Exception occurred during request processing: {Method} {Path}",
                    request.Method,
                    request.Path
                );
                
                throw; // Re-throw to let error handling middleware handle it
            }
            finally
            {
                stopwatch.Stop();
                var response = context.Response;
                
                // Only process response body if no exception occurred and we were capturing it
                string responseBodyContent = string.Empty;
                if (_logResponseBody && !exceptionOccurred && responseBody != null && responseBody.Length > 0)
                {
                    responseBody.Seek(0, SeekOrigin.Begin);
                    responseBodyContent = await new StreamReader(responseBody).ReadToEndAsync();
                    responseBody.Seek(0, SeekOrigin.Begin);
                    
                    // Copy the response body back to the original stream
                    if (originalResponseBodyStream != null)
                    {
                        await responseBody.CopyToAsync(originalResponseBodyStream);
                    }
                }

                // Clean up
                responseBody?.Dispose();

                var logLevel = GetLogLevel(response.StatusCode);
                
                if (_logResponseBody && !string.IsNullOrEmpty(responseBodyContent))
                {
                    _logger.Log(logLevel,
                        "Outgoing Response: {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds}ms\n" +
                        "Response Headers: {ResponseHeaders}\n" +
                        "Response Body: {ResponseBody}",
                        request.Method,
                        request.Path,
                        response.StatusCode,
                        stopwatch.ElapsedMilliseconds,
                        GetHeadersAsString(response.Headers),
                        TruncateIfNeeded(responseBodyContent)
                    );
                }
                else
                {
                    _logger.Log(logLevel,
                        "Outgoing Response: {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds}ms\n" +
                        "Response Headers: {ResponseHeaders}",
                        request.Method,
                        request.Path,
                        response.StatusCode,
                        stopwatch.ElapsedMilliseconds,
                        GetHeadersAsString(response.Headers)
                    );
                }
            }
        }

        private async Task<string> ReadRequestBodyAsync(HttpRequest request)
        {
            request.EnableBuffering();
            var buffer = new byte[Convert.ToInt32(Math.Min(request.ContentLength ?? 0, _maxBodySize))];
            await request.Body.ReadExactlyAsync(buffer, 0, buffer.Length);
            var bodyText = Encoding.UTF8.GetString(buffer);
            request.Body.Seek(0, SeekOrigin.Begin);
            return TruncateIfNeeded(bodyText);
        }

        private string TruncateIfNeeded(string content)
        {
            if (content.Length <= _maxBodySize)
                return content;
            
            return content.Substring(0, _maxBodySize) + "... (truncated)";
        }

        private static string GetHeadersAsString(IHeaderDictionary headers)
        {
            var headerStrings = headers
                .Where(h => !h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase)) // Don't log auth headers
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