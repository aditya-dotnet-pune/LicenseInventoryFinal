using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LicenseInventoryAPI.Models
{
    // Group 3: For Finance/Reporting
    public class CostAllocation
    {
        [Key]
        public Guid AllocationId { get; set; }

        [Required]
        public Guid LicenseId { get; set; }
        public License? License { get; set; }

        [Required]
        public string DepartmentId { get; set; } = string.Empty;

        public AllocationMethod AllocationMethod { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AllocatedAmount { get; set; }

        public string Currency { get; set; } = "USD";

        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }
}