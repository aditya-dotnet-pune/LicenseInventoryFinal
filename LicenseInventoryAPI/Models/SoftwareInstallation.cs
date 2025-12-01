using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LicenseInventoryAPI.Models
{
    // Group 1 & 2: Links a Device to a Product and potentially a License
    public class SoftwareInstallation
    {
        [Key]
        public Guid InstallationId { get; set; }

        [Required]
        public string ProductName { get; set; } = string.Empty;

        public string Version { get; set; } = string.Empty;

        public DateTime InstallDate { get; set; }

        // Foreign Key to Device
        public Guid DeviceId { get; set; }
        public Device? Device { get; set; }

        // Foreign Key to License (Nullable, because software might be installed but not yet matched to a license)
        public Guid? LicenseId { get; set; }
        public License? License { get; set; }
    }
}