namespace WebApi.Middleware
{
    public static class MiddlewareExtensions
    {
        /// <summary>
        /// Adds the simple error handling middleware to the application pipeline (recommended)
        /// </summary>
        /// <param name="builder">The application builder</param>
        /// <returns>The application builder for chaining</returns>
        public static IApplicationBuilder UseSimpleErrorHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SimpleErrorHandlingMiddleware>();
        }

        /// <summary>
        /// Adds the request/response logging middleware to the application pipeline
        /// </summary>
        /// <param name="builder">The application builder</param>
        /// <returns>The application builder for chaining</returns>
        public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestResponseLoggingMiddleware>();
        }

        /// <summary>
        /// Adds the detailed request/response logging middleware to the application pipeline
        /// </summary>
        /// <param name="builder">The application builder</param>
        /// <param name="logRequestBody">Whether to log request bodies</param>
        /// <param name="logResponseBody">Whether to log response bodies</param>
        /// <param name="maxBodySize">Maximum size of body content to log</param>
        /// <returns>The application builder for chaining</returns>
        public static IApplicationBuilder UseDetailedRequestResponseLogging(
            this IApplicationBuilder builder,
            bool logRequestBody = false,
            bool logResponseBody = false,
            int maxBodySize = 4096)
        {
            return builder.UseMiddleware<DetailedRequestResponseLoggingMiddleware>(
                logRequestBody, 
                logResponseBody, 
                maxBodySize);
        }

        /// <summary>
        /// Adds the configurable request/response logging middleware to the application pipeline
        /// </summary>
        /// <param name="builder">The application builder</param>
        /// <param name="configureOptions">Action to configure logging options</param>
        /// <returns>The application builder for chaining</returns>
        public static IApplicationBuilder UseConfigurableRequestResponseLogging(
            this IApplicationBuilder builder,
            Action<RequestResponseLoggingOptions>? configureOptions = null)
        {
            if (configureOptions != null)
            {
                builder.ApplicationServices.GetRequiredService<IServiceCollection>()
                    .Configure(configureOptions);
            }
            
            return builder.UseMiddleware<ConfigurableRequestResponseLoggingMiddleware>();
        }
    }
}