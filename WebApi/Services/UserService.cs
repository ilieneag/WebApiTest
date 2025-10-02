using WebApi.Models;
using WebApi.DTOs;
using WebApi.Services;

namespace WebApi.Services
{
    public class UserService : IUserService
    {
        // In-memory storage for demonstration. In production, you'd use a database.
        private static readonly List<User> _users = new List<User>
        {
            new User 
            { 
                Id = 1, 
                FirstName = "John", 
                LastName = "Doe", 
                Email = "john.doe@techhive.com",
                PhoneNumber = "+1-555-0101",
                Department = "IT",
                JobTitle = "Software Developer",
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new User 
            { 
                Id = 2, 
                FirstName = "Jane", 
                LastName = "Smith", 
                Email = "jane.smith@techhive.com",
                PhoneNumber = "+1-555-0102",
                Department = "HR",
                JobTitle = "HR Manager",
                CreatedAt = DateTime.UtcNow.AddDays(-25)
            },
            new User 
            { 
                Id = 3, 
                FirstName = "Mike", 
                LastName = "Johnson", 
                Email = "mike.johnson@techhive.com",
                PhoneNumber = "+1-555-0103",
                Department = "IT",
                JobTitle = "System Administrator",
                CreatedAt = DateTime.UtcNow.AddDays(-20)
            }
        };
        private static int _nextId = 4;

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            await Task.Delay(1); // Simulate async operation
            return _users.Where(u => u.IsActive).OrderBy(u => u.LastName).ThenBy(u => u.FirstName);
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            await Task.Delay(1); // Simulate async operation
            return _users.FirstOrDefault(u => u.Id == id && u.IsActive);
        }

        public async Task<User> CreateUserAsync(CreateUserDto createUserDto)
        {
            await Task.Delay(1); // Simulate async operation
            
            var user = new User
            {
                Id = _nextId++,
                FirstName = createUserDto.FirstName,
                LastName = createUserDto.LastName,
                Email = createUserDto.Email,
                PhoneNumber = createUserDto.PhoneNumber,
                Department = createUserDto.Department,
                JobTitle = createUserDto.JobTitle,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _users.Add(user);
            return user;
        }

        public async Task<User?> UpdateUserAsync(int id, UpdateUserDto updateUserDto)
        {
            await Task.Delay(1); // Simulate async operation
            
            var user = _users.FirstOrDefault(u => u.Id == id && u.IsActive);
            if (user == null)
                return null;

            // Update only provided fields
            if (!string.IsNullOrEmpty(updateUserDto.FirstName))
                user.FirstName = updateUserDto.FirstName;
            
            if (!string.IsNullOrEmpty(updateUserDto.LastName))
                user.LastName = updateUserDto.LastName;
            
            if (!string.IsNullOrEmpty(updateUserDto.Email))
                user.Email = updateUserDto.Email;
            
            if (updateUserDto.PhoneNumber != null)
                user.PhoneNumber = updateUserDto.PhoneNumber;
            
            if (updateUserDto.Department != null)
                user.Department = updateUserDto.Department;
            
            if (updateUserDto.JobTitle != null)
                user.JobTitle = updateUserDto.JobTitle;
            
            if (updateUserDto.IsActive.HasValue)
                user.IsActive = updateUserDto.IsActive.Value;

            user.UpdatedAt = DateTime.UtcNow;
            return user;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            await Task.Delay(1); // Simulate async operation
            
            var user = _users.FirstOrDefault(u => u.Id == id && u.IsActive);
            if (user == null)
                return false;

            // Soft delete - mark as inactive instead of removing
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            return true;
        }

        public async Task<bool> UserExistsAsync(int id)
        {
            await Task.Delay(1); // Simulate async operation
            return _users.Any(u => u.Id == id && u.IsActive);
        }

        public async Task<bool> EmailExistsAsync(string email, int? excludeUserId = null)
        {
            await Task.Delay(1); // Simulate async operation
            return _users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase) 
                                && u.IsActive 
                                && (excludeUserId == null || u.Id != excludeUserId));
        }
    }
}