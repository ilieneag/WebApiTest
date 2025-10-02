using System.Security.Cryptography;
using System.Text;
using WebApi.Models;
using WebApi.DTOs;
using WebApi.Middleware;

namespace WebApi.Services
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<LoginResponse> RegisterAsync(RegisterRequest request);
        Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request);
        Task<bool> RevokeTokenAsync(int userId);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }

    public class AuthService : IAuthService
    {
        private readonly IUserService _userService;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUserService userService, IJwtService jwtService, ILogger<AuthService> logger)
        {
            _userService = userService;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                throw new ValidationException("Email and password are required");
            }

            var users = await _userService.GetAllUsersAsync();
            var user = users.FirstOrDefault(u => u.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase));

            if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Failed login attempt for email: {Email}", request.Email);
                throw new UnauthorizedException("Invalid email or password");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedException("Account is disabled");
            }

            return await GenerateTokenResponse(user);
        }

        public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
        {
            // Validate request
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                throw new ValidationException("Email and password are required");
            }

            if (request.Password != request.ConfirmPassword)
            {
                throw new ValidationException("Password and confirm password do not match");
            }

            if (request.Password.Length < 6)
            {
                throw new ValidationException("Password must be at least 6 characters long");
            }

            // Check if user already exists
            var existingUsers = await _userService.GetAllUsersAsync();
            if (existingUsers.Any(u => u.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ConflictException("User with this email already exists");
            }

            // Create new user
            var createUserDto = new CreateUserDto
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email
            };

            var newUser = await _userService.CreateUserAsync(createUserDto);
            
            // Set password hash
            newUser.PasswordHash = HashPassword(request.Password);

            return await GenerateTokenResponse(newUser);
        }

        public async Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                throw new ValidationException("Refresh token is required");
            }

            var users = await _userService.GetAllUsersAsync();
            var user = users.FirstOrDefault(u => u.RefreshToken == request.RefreshToken);

            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                throw new UnauthorizedException("Invalid or expired refresh token");
            }

            return await GenerateTokenResponse(user);
        }

        public async Task<bool> RevokeTokenAsync(int userId)
        {
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            
            await _userService.UpdateUserAsync(userId, new UpdateUserDto
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Department = user.Department,
                JobTitle = user.JobTitle
            });

            return true;
        }

        public string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var salt = "TechHive_WebAPI_Salt_2024"; // In production, use a proper salt per user
            var saltedPassword = password + salt;
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
            return Convert.ToBase64String(hashedBytes);
        }

        public bool VerifyPassword(string password, string hash)
        {
            var computedHash = HashPassword(password);
            return computedHash == hash;
        }

        private Task<LoginResponse> GenerateTokenResponse(User user)
        {
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            // Update user with refresh token
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // Refresh token valid for 7 days

            var userDto = new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Department = user.Department,
                JobTitle = user.JobTitle,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                IsActive = user.IsActive
            };

            return Task.FromResult(new LoginResponse
            {
                Token = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60), // Access token expires in 1 hour
                User = userDto
            });
        }
    }
}