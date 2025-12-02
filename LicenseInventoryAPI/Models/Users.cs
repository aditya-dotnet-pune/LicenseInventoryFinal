using System.ComponentModel.DataAnnotations;

namespace LicenseInventoryAPI.Models
{
    public class Users
    {
        [Key]
        public Guid UserId { get; set; } // Must be Guid to match UNIQUEIDENTIFIER in DB

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Viewer";
    }
}