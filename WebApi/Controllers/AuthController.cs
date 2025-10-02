using Microsoft.AspNetCore.Mvc;
using WebApi.Models;
using WebApi.Services;
using WebApi.Middleware;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Authenticate user and return JWT token
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>JWT token and user information</returns>
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            _logger.LogInformation("Login attempt for email: {Email}", request.Email);
            
            var response = await _authService.LoginAsync(request);
            
            _logger.LogInformation("User {UserId} logged in successfully", response.User.Id);
            return Ok(response);
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        /// <param name="request">Registration details</param>
        /// <returns>JWT token and user information</returns>
        [HttpPost("register")]
        public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request)
        {
            _logger.LogInformation("Registration attempt for email: {Email}", request.Email);
            
            var response = await _authService.RegisterAsync(request);
            
            _logger.LogInformation("User {UserId} registered successfully", response.User.Id);
            return Ok(response);
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        /// <param name="request">Refresh token</param>
        /// <returns>New JWT token</returns>
        [HttpPost("refresh-token")]
        public async Task<ActionResult<LoginResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var response = await _authService.RefreshTokenAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Revoke user's refresh token (logout)
        /// </summary>
        /// <returns>Success confirmation</returns>
        [HttpPost("logout")]
        public async Task<ActionResult> Logout()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            
            if (userIdClaim != null && int.TryParse(userIdClaim, out int userId))
            {
                await _authService.RevokeTokenAsync(userId);
                _logger.LogInformation("User {UserId} logged out successfully", userId);
            }

            return Ok(new { message = "Logged out successfully" });
        }

        /// <summary>
        /// Get current user information from token
        /// </summary>
        /// <returns>Current user details</returns>
        [HttpGet("me")]
        public ActionResult<object> GetCurrentUser()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            var firstNameClaim = User.FindFirst("firstName")?.Value;
            var lastNameClaim = User.FindFirst("lastName")?.Value;
            var emailClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var rolesClaims = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();

            return Ok(new
            {
                UserId = userIdClaim,
                FirstName = firstNameClaim,
                LastName = lastNameClaim,
                Email = emailClaim,
                Roles = rolesClaims,
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false
            });
        }

        /// <summary>
        /// Test endpoint to check authentication
        /// </summary>
        /// <returns>Authentication status</returns>
        [HttpGet("test")]
        public ActionResult TestAuth()
        {
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            var userName = User.Identity?.Name ?? "Anonymous";
            
            return Ok(new 
            { 
                IsAuthenticated = isAuthenticated,
                UserName = userName,
                Message = isAuthenticated ? "You are authenticated!" : "You are not authenticated."
            });
        }
    }
}