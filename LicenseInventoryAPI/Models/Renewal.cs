using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LicenseInventoryAPI.Models
{
    public class Renewal
    {
        [Key]
        public Guid RenewalId { get; set; }

        // Link to the License being renewed
        [Required]
        public Guid LicenseId { get; set; }

        [Required]
        [MaxLength(200)]
        public string SoftwareName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public DateTime DueDate { get; set; }

        public string? QuoteDetails { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Cost { get; set; }
    }
}