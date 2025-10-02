namespace WebApi.Middleware
{
    public class RequestResponseLoggingOptions
    {
        /// <summary>
        /// Whether to log request bodies. Default is false for security and performance reasons.
        /// </summary>
        public bool LogRequestBody { get; set; } = false;

        /// <summary>
        /// Whether to log response bodies. Default is false for performance reasons.
        /// </summary>
        public bool LogResponseBody { get; set; } = false;

        /// <summary>
        /// Maximum size of request/response body to log in bytes. Default is 4KB.
        /// </summary>
        public int MaxBodySize { get; set; } = 4096;

        /// <summary>
        /// Whether to log request headers. Default is true.
        /// </summary>
        public bool LogHeaders { get; set; } = true;

        /// <summary>
        /// Whether to log response headers. Default is true.
        /// </summary>
        public bool LogResponseHeaders { get; set; } = true;

        /// <summary>
        /// Whether to log request duration. Default is true.
        /// </summary>
        public bool LogDuration { get; set; } = true;

        /// <summary>
        /// Whether to log client IP address. Default is true.
        /// </summary>
        public bool LogClientIP { get; set; } = true;

        /// <summary>
        /// List of paths to exclude from logging (e.g., health checks, static files)
        /// </summary>
        public List<string> ExcludePaths { get; set; } = new List<string>();

        /// <summary>
        /// List of headers to exclude from logging (for security reasons)
        /// </summary>
        public List<string> ExcludeHeaders { get; set; } = new List<string> 
        { 
            "Authorization", 
            "Cookie", 
            "Set-Cookie" 
        };
    }
}