using System.ComponentModel.DataAnnotations;

namespace LicenseInventoryAPI.Models
{
    public class Device
    {
        [Key]
        public Guid DeviceId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Hostname { get; set; } = string.Empty;

        // Represents the primary user of the device (from AD or Input)
        public string? OwnerUserId { get; set; }

        public DateTime LastSeen { get; set; }

        // Navigation Property
        public ICollection<SoftwareInstallation> InstalledSoftware { get; set; } = new List<SoftwareInstallation>();
    }
}