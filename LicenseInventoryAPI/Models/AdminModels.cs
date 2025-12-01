using System.ComponentModel.DataAnnotations;

namespace LicenseInventoryAPI.Models
{
    // Simple User model for RBAC
    public class User
    {
        [Key]
        public Guid UserId { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        public UserRole Role { get; set; }
    }

    // Group 3: Immutable audit trail
    public class AuditLog
    {
        [Key]
        public Guid LogId { get; set; }

        public string Action { get; set; } = string.Empty; // e.g., "Created", "Updated", "Deleted"

        public string EntityName { get; set; } = string.Empty; // e.g., "License", "Device"

        public string EntityId { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string PerformedByUserId { get; set; } = string.Empty;

        public string? Changes { get; set; } // JSON string of changes
    }
}