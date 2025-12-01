using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LicenseInventoryAPI.Models
{
    public class License
    {
        [Key]
        public Guid LicenseId { get; set; }

        [Required]
        [MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Vendor { get; set; } = string.Empty;

        [Required]
        public LicenseType LicenseType { get; set; }

        [Required]
        public int TotalEntitlements { get; set; }

        // This can be updated via business logic or computed
        public int AssignedLicenses { get; set; }

        [NotMapped]
        public int AvailableLicenses => TotalEntitlements - AssignedLicenses;

        [Required]
        public DateTime PurchaseDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Cost { get; set; }

        [Required]
        [MaxLength(3)]
        public string Currency { get; set; } = "USD"; // e.g., USD, EUR

        public string? Notes { get; set; }

        // Navigation Properties
        public ICollection<SoftwareInstallation> Installations { get; set; } = new List<SoftwareInstallation>();
        public ICollection<ComplianceEvent> ComplianceEvents { get; set; } = new List<ComplianceEvent>();
        public ICollection<CostAllocation> CostAllocations { get; set; } = new List<CostAllocation>();
    }
}