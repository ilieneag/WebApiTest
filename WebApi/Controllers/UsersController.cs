using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebApi.Models;
using WebApi.DTOs;
using WebApi.Services;
using WebApi.Middleware;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Require authentication for all endpoints
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Get all users
        /// </summary>
        /// <returns>List of all active users</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        /// <summary>
        /// Get a specific user by ID
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>User details</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            if (id <= 0)
            {
                throw new ValidationException("User ID must be greater than 0");
            }

            var user = await _userService.GetUserByIdAsync(id);
            
            if (user == null)
            {
                throw new NotFoundException($"User with ID {id} not found");
            }

            return Ok(user);
        }

        /// <summary>
        /// Create a new user
        /// </summary>
        /// <param name="createUserDto">User creation data</param>
        /// <returns>Created user</returns>
        [HttpPost]
        public async Task<ActionResult<User>> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                
                throw new ValidationException("Validation failed", errors);
            }

            // Check if email already exists
            if (await _userService.EmailExistsAsync(createUserDto.Email))
            {
                throw new ConflictException("A user with this email already exists");
            }

            var user = await _userService.CreateUserAsync(createUserDto);
            
            return CreatedAtAction(
                nameof(GetUser), 
                new { id = user.Id }, 
                user
            );
        }

        /// <summary>
        /// Update an existing user
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="updateUserDto">User update data</param>
        /// <returns>Updated user</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<User>> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
        {
            if (id <= 0)
            {
                throw new ValidationException("User ID must be greater than 0");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                
                throw new ValidationException("Validation failed", errors);
            }

            // Check if user exists
            if (!await _userService.UserExistsAsync(id))
            {
                throw new NotFoundException($"User with ID {id} not found");
            }

            // Check if email already exists for another user
            if (!string.IsNullOrEmpty(updateUserDto.Email) && 
                await _userService.EmailExistsAsync(updateUserDto.Email, id))
            {
                throw new ConflictException("A user with this email already exists");
            }

            var updatedUser = await _userService.UpdateUserAsync(id, updateUserDto);
            
            if (updatedUser == null)
            {
                throw new NotFoundException($"User with ID {id} not found");
            }

            return Ok(updatedUser);
        }

        /// <summary>
        /// Delete a user (soft delete)
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Success message</returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            if (id <= 0)
            {
                throw new ValidationException("User ID must be greater than 0");
            }

            var result = await _userService.DeleteUserAsync(id);
            
            if (!result)
            {
                throw new NotFoundException($"User with ID {id} not found");
            }

            return Ok(new { message = $"User with ID {id} has been successfully deleted" });
        }

        /// <summary>
        /// Test endpoint to demonstrate error handling
        /// </summary>
        /// <param name="errorType">Type of error to simulate</param>
        /// <returns>Throws specified error type</returns>
        [HttpGet("test-error/{errorType}")]
        public ActionResult TestError(string errorType)
        {
            switch (errorType.ToLower())
            {
                case "validation":
                    throw new ValidationException("This is a test validation error", new { field1 = "Error message 1", field2 = "Error message 2" });
                
                case "notfound":
                    throw new NotFoundException("This is a test not found error");
                
                case "conflict":
                    throw new ConflictException("This is a test conflict error");
                
                case "unauthorized":
                    throw new UnauthorizedException("This is a test unauthorized error");
                
                case "argument":
                    throw new ArgumentException("This is a test argument error");
                
                case "invalidoperation":
                    throw new InvalidOperationException("This is a test invalid operation error");
                
                case "generic":
                    throw new Exception("This is a test generic exception");
                
                default:
                    return Ok(new { message = "No error thrown. Valid error types: validation, notfound, conflict, unauthorized, argument, invalidoperation, generic" });
            }
        }
    }
}