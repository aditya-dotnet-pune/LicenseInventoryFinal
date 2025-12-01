using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LicenseInventoryAPI.Data;
using LicenseInventoryAPI.Models;

namespace LicenseInventoryAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComplianceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ComplianceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/Compliance/RunCheck
        // Calculates usage and updates license counts
        [HttpPost("RunCheck")]
        public async Task<IActionResult> RunComplianceCheck()
        {
            var licenses = await _context.Licenses.Include(l => l.Installations).ToListAsync();
            var allInstallations = await _context.SoftwareInstallations.ToListAsync();

            foreach (var license in licenses)
            {
                // Simple matching logic: Match by Product Name
                // In real world, this is more complex (SKU matching)
                var matchCount = allInstallations.Count(i =>
                    i.ProductName.Equals(license.ProductName, StringComparison.OrdinalIgnoreCase));

                license.AssignedLicenses = matchCount;

                // Check for Over-usage
                if (license.AssignedLicenses > license.TotalEntitlements)
                {
                    // Log Alert
                    _context.ComplianceEvents.Add(new ComplianceEvent
                    {
                        EventId = Guid.NewGuid(),
                        LicenseId = license.LicenseId,
                        Type = ComplianceEventType.OverUse,
                        Severity = ComplianceSeverity.High,
                        Details = $"License {license.ProductName} is over-utilized. Used: {matchCount}, Total: {license.TotalEntitlements}",
                        DetectedAt = DateTime.UtcNow
                    });
                }

                // Check for Expiry
                if (license.ExpiryDate.HasValue && license.ExpiryDate.Value < DateTime.UtcNow.AddDays(30))
                {
                    _context.ComplianceEvents.Add(new ComplianceEvent
                    {
                        EventId = Guid.NewGuid(),
                        LicenseId = license.LicenseId,
                        Type = ComplianceEventType.Expiry,
                        Severity = ComplianceSeverity.Medium,
                        Details = $"License {license.ProductName} expires on {license.ExpiryDate.Value.ToShortDateString()}",
                        DetectedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Compliance check completed successfully." });
        }

        // GET: api/Compliance/Alerts
        [HttpGet("Alerts")]
        public async Task<ActionResult<IEnumerable<ComplianceEvent>>> GetAlerts()
        {
            return await _context.ComplianceEvents
                                 .Include(c => c.License)
                                 .Where(c => !c.IsResolved)
                                 .OrderByDescending(c => c.DetectedAt)
                                 .ToListAsync();
        }
    }
}