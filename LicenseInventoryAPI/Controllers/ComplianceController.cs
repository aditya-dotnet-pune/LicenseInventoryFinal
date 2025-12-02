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
        [HttpPost("RunCheck")]
        public async Task<IActionResult> RunComplianceCheck()
        {
            // 1. Fetch all necessary data
            var licenses = await _context.Licenses.Include(l => l.Installations).ToListAsync();
            var allInstallations = await _context.SoftwareInstallations.ToListAsync();

            // 2. Clear existing unresolved system-generated alerts to avoid duplicates
            // (In a real production app, you might want to update existing ones instead)
            var existingAlerts = await _context.ComplianceEvents.Where(e => !e.IsResolved).ToListAsync();
            _context.ComplianceEvents.RemoveRange(existingAlerts);

            var newAlerts = new List<ComplianceEvent>();

            foreach (var license in licenses)
            {
                // A. Update Usage Counts
                // Match installations by Product Name (Case insensitive)
                var matchCount = allInstallations.Count(i =>
                    string.Equals(i.ProductName, license.ProductName, StringComparison.OrdinalIgnoreCase));

                license.AssignedLicenses = matchCount;

                // --- RULE 1: Over-Entitlement Usage ---
                if (license.AssignedLicenses > license.TotalEntitlements)
                {
                    newAlerts.Add(new ComplianceEvent
                    {
                        EventId = Guid.NewGuid(),
                        LicenseId = license.LicenseId,
                        Type = ComplianceEventType.OverUse,
                        Severity = ComplianceSeverity.High,
                        Details = $"Over-usage detected: {matchCount} installed vs {license.TotalEntitlements} owned.",
                        DetectedAt = DateTime.UtcNow
                    });
                }

                // --- RULE 2: Unused Licenses (Reclamation) ---
                if (license.AssignedLicenses == 0 && license.TotalEntitlements > 0)
                {
                    newAlerts.Add(new ComplianceEvent
                    {
                        EventId = Guid.NewGuid(),
                        LicenseId = license.LicenseId,
                        Type = ComplianceEventType.Unused, // Ensure this Enum exists in Models
                        Severity = ComplianceSeverity.Low, // Low severity as it's an optimization, not a risk
                        Details = $"Zero usage detected. {license.TotalEntitlements} licenses eligible for reclamation.",
                        DetectedAt = DateTime.UtcNow
                    });
                }

                // --- RULE 3: Tiered Expiry Alerts ---
                if (license.ExpiryDate.HasValue)
                {
                    var daysUntilExpiry = (license.ExpiryDate.Value - DateTime.UtcNow).TotalDays;

                    // Only trigger if not already expired (negative days would be 'Expired') 
                    // or handle expired as Critical too. Here we handle the 90-day window.

                    if (daysUntilExpiry <= 90)
                    {
                        ComplianceSeverity severity;
                        string urgency;

                        if (daysUntilExpiry <= 30)
                        {
                            severity = ComplianceSeverity.High;
                            urgency = "Critical";
                        }
                        else if (daysUntilExpiry <= 60)
                        {
                            severity = ComplianceSeverity.Medium;
                            urgency = "Warning";
                        }
                        else // 61-90 days
                        {
                            severity = ComplianceSeverity.Low;
                            urgency = "Notice";
                        }

                        // Add Alert
                        newAlerts.Add(new ComplianceEvent
                        {
                            EventId = Guid.NewGuid(),
                            LicenseId = license.LicenseId,
                            Type = ComplianceEventType.Expiry,
                            Severity = severity,
                            Details = $"{urgency}: Expires in {Math.Ceiling(daysUntilExpiry)} days ({license.ExpiryDate.Value.ToShortDateString()})",
                            DetectedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            _context.ComplianceEvents.AddRange(newAlerts);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Compliance check completed.", alertsGenerated = newAlerts.Count });
        }

        // GET: api/Compliance/Alerts
        [HttpGet("Alerts")]
        public async Task<ActionResult<IEnumerable<ComplianceEvent>>> GetAlerts()
        {
            return await _context.ComplianceEvents
                                 .Include(c => c.License)
                                 .Where(c => !c.IsResolved)
                                 .OrderByDescending(c => c.Severity) // High priority first
                                 .ThenBy(c => c.DetectedAt)
                                 .ToListAsync();
        }
    }
}