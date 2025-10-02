using Serilog;
using WebApi.Services;
using WebApi.Middleware;
using WebApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// Configure Serilog early to capture startup logs
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/startup-.log", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting WebApi application");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog from appsettings.json
    builder.Host.UseSerilog((context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName));

    // Configure JWT Settings
    var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
    if (jwtSettings == null)
    {
        throw new InvalidOperationException("JWT settings are not configured properly");
    }

    // Add services to the container.
    builder.Services.AddControllers();

    // Configure JWT authentication
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Log.Warning("JWT Authentication failed: {Message}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Log.Debug("JWT Token validated for user: {UserId}", 
                    context.Principal?.FindFirst("userId")?.Value ?? "Unknown");
                return Task.CompletedTask;
            }
        };
    });

    // Add authorization
    builder.Services.AddAuthorization();

    // Register services
    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IJwtService, JwtService>();
    builder.Services.AddScoped<IAuthService, AuthService>();

    var app = builder.Build();

    // Add simple error handling middleware first to catch all exceptions (no stream conflicts)
    app.UseSimpleErrorHandling();

    // Add Serilog request logging middleware
    app.UseSerilogRequestLogging(options =>
    {
        // Customize the message template
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        
        // Enrich with additional data
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? "Unknown");
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
            diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown");
        };
        
        // Get logger from DI container
        options.GetLevel = (httpContext, elapsed, ex) =>
        {
            if (ex != null) return Serilog.Events.LogEventLevel.Error;
            if (httpContext.Response.StatusCode >= 500) return Serilog.Events.LogEventLevel.Error;
            if (httpContext.Response.StatusCode >= 400) return Serilog.Events.LogEventLevel.Warning;
            return Serilog.Events.LogEventLevel.Information;
        };
    });

    // Configure the HTTP request pipeline.

    // Add our custom detailed logging middleware for development (optional - shows request/response bodies)
    // Note: Placed after error handling to avoid stream conflicts
    if (app.Environment.IsDevelopment())
    {
        // Use basic logging instead of detailed logging to avoid conflicts
        app.UseDetailedRequestResponseLogging(logRequestBody: true, logResponseBody: true, maxBodySize: 8192);
    }

    // Remove HTTPS redirection so you can test with http
    // app.UseHttpsRedirection();

    // Add authentication and authorization middleware
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    Log.Information("WebApi application configured successfully");
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

