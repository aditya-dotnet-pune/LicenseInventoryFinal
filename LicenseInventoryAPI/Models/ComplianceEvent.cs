using System.ComponentModel.DataAnnotations;

namespace LicenseInventoryAPI.Models
{
    // Group 2: Stores alerts like expiry warnings or over-usage
    public class ComplianceEvent
    {
        [Key]
        public Guid EventId { get; set; }

        [Required]
        public Guid LicenseId { get; set; }
        public License? License { get; set; }

        public ComplianceEventType Type { get; set; }

        public ComplianceSeverity Severity { get; set; }

        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

        public string Details { get; set; } = string.Empty;

        public bool IsResolved { get; set; } = false;

        public string? ResolvedByUserId { get; set; }

        public string? ResolutionNotes { get; set; }
    }
}