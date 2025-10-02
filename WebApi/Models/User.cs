using System.ComponentModel.DataAnnotations;

namespace WebApi.Models
{
    public class User
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string? PhoneNumber { get; set; }
        
        [StringLength(100)]
        public string? Department { get; set; }
        
        [StringLength(100)]
        public string? JobTitle { get; set; }
        
        // Authentication fields
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        
        public string? RefreshToken { get; set; }
        
        public DateTime? RefreshTokenExpiryTime { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public List<string> Roles { get; set; } = new() { "User" };
    }
}