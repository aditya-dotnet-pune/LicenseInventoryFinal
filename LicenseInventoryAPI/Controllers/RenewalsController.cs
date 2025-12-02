using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LicenseInventoryAPI.Data;
using LicenseInventoryAPI.Models;

namespace LicenseInventoryAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RenewalsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RenewalsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Renewal>>> GetRenewals()
        {
            return await _context.Renewals.OrderByDescending(r => r.DueDate).ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<Renewal>> CreateRenewal(Renewal renewal)
        {
            renewal.RenewalId = Guid.NewGuid();
            if (string.IsNullOrEmpty(renewal.Status)) renewal.Status = "Pending";

            _context.Renewals.Add(renewal);

            // Log creation
            _context.AuditLogs.Add(new AuditLog
            {
                LogId = Guid.NewGuid(),
                Action = "Created",
                EntityName = "Renewal",
                EntityId = renewal.RenewalId.ToString(),
                Timestamp = DateTime.UtcNow,
                PerformedByUserId = "Admin",
                Changes = $"Requested renewal for {renewal.SoftwareName}"
            });

            await _context.SaveChangesAsync();
            return CreatedAtAction("GetRenewals", new { id = renewal.RenewalId }, renewal);
        }

        // PUT: api/Renewals/5/Status
        [HttpPut("{id}/Status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] string status)
        {
            var renewal = await _context.Renewals.FindAsync(id);
            if (renewal == null) return NotFound("Renewal task not found");

            var oldStatus = renewal.Status;
            renewal.Status = status;

            // --- BUSINESS LOGIC FOR APPROVAL ---
            if (status == "Approved")
            {
                // 1. Find the License
                var license = await _context.Licenses.FindAsync(renewal.LicenseId);
                if (license != null)
                {
                    // 2. Extend Expiry Date by 1 Year
                    if (license.ExpiryDate < DateTime.UtcNow)
                        license.ExpiryDate = DateTime.UtcNow.AddYears(1);
                    else
                        license.ExpiryDate = license.ExpiryDate?.AddYears(1);

                    // 3. Resolve Compliance Alerts
                    var alerts = await _context.ComplianceEvents
                        .Where(e => e.LicenseId == renewal.LicenseId && e.Type == ComplianceEventType.Expiry && !e.IsResolved)
                        .ToListAsync();

                    foreach (var alert in alerts)
                    {
                        alert.IsResolved = true;
                        alert.ResolutionNotes = "Resolved via Finance Approval";
                        alert.ResolvedByUserId = "Finance";
                    }
                }
            }

            // Log the decision
            _context.AuditLogs.Add(new AuditLog
            {
                LogId = Guid.NewGuid(),
                Action = status == "Approved" ? "Approved" : "Rejected",
                EntityName = "Renewal",
                EntityId = id.ToString(),
                Timestamp = DateTime.UtcNow,
                PerformedByUserId = "Finance",
                Changes = $"Renewal {status} for {renewal.SoftwareName}"
            });

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Renewal {status} successfully." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRenewal(Guid id)
        {
            var renewal = await _context.Renewals.FindAsync(id);
            if (renewal == null) return NotFound();
            _context.Renewals.Remove(renewal);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}