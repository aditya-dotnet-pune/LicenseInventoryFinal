using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LicenseInventoryAPI.Data;
using LicenseInventoryAPI.Models;
using System.Text.Json;

namespace LicenseInventoryAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LicensesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LicensesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ... Existing Methods (Get, Create, Update, Renew, Delete) ...

        // NEW: CSV Import Endpoint
        [HttpPost("import")]
        public async Task<IActionResult> ImportLicenses(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var licensesToAdd = new List<License>();
            int successCount = 0;
            int errorCount = 0;

            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                // Read header line and skip it
                var header = await reader.ReadLineAsync();

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var values = line.Split(',');

                    // Simple Validation: We expect 7 columns based on your sample CSV
                    if (values.Length < 7)
                    {
                        errorCount++;
                        continue;
                    }

                    try
                    {
                        var license = new License
                        {
                            LicenseId = Guid.NewGuid(),
                            ProductName = values[0].Trim(),
                            Vendor = values[1].Trim(),
                            // Parse Enum (PerUser, PerDevice, etc.)
                            LicenseType = Enum.Parse<LicenseType>(values[2].Trim(), true),
                            TotalEntitlements = int.Parse(values[3].Trim()),
                            Cost = decimal.Parse(values[4].Trim()),
                            PurchaseDate = DateTime.Parse(values[5].Trim()),
                            Currency = "INR", // Defaulting to INR as per requirements
                            AssignedLicenses = 0
                        };

                        // Handle nullable ExpiryDate
                        if (!string.IsNullOrWhiteSpace(values[6]))
                        {
                            license.ExpiryDate = DateTime.Parse(values[6].Trim());
                        }

                        licensesToAdd.Add(license);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing line: {line}. Error: {ex.Message}");
                        errorCount++;
                    }
                }
            }

            if (licensesToAdd.Count > 0)
            {
                _context.Licenses.AddRange(licensesToAdd);

                // Optional: Log bulk import action
                var log = new AuditLog
                {
                    LogId = Guid.NewGuid(),
                    Action = "Import",
                    EntityName = "License",
                    EntityId = "Bulk",
                    Timestamp = DateTime.UtcNow,
                    PerformedByUserId = "Admin",
                    Changes = $"Imported {successCount} licenses from CSV."
                };
                _context.AuditLogs.Add(log);

                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                message = $"Import complete. Success: {successCount}, Failed: {errorCount}",
                count = successCount
            });
        }

        // --- EXISTING METHODS RE-INCLUDED FOR CONTEXT ---

        // Helper method to log actions
        private async Task LogAction(string action, string entityName, string entityId, string details)
        {
            var log = new AuditLog
            {
                LogId = Guid.NewGuid(),
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Timestamp = DateTime.UtcNow,
                PerformedByUserId = "Admin",
                Changes = details
            };
            _context.AuditLogs.Add(log);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<License>>> GetLicenses()
        {
            return await _context.Licenses.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<License>> GetLicense(Guid id)
        {
            var license = await _context.Licenses.FindAsync(id);
            if (license == null) return NotFound();
            return license;
        }

        [HttpPost]
        public async Task<ActionResult<License>> CreateLicense(License license)
        {
            license.LicenseId = Guid.NewGuid();
            _context.Licenses.Add(license);
            await LogAction("Created", "License", license.LicenseId.ToString(), $"Created license for {license.ProductName}");
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetLicense", new { id = license.LicenseId }, license);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLicense(Guid id, License license)
        {
            if (id != license.LicenseId) return BadRequest();
            _context.Entry(license).State = EntityState.Modified;
            try
            {
                await LogAction("Updated", "License", id.ToString(), "Updated license details");
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Licenses.Any(e => e.LicenseId == id)) return NotFound();
                else throw;
            }
            return NoContent();
        }

        [HttpPost("Renew/{id}")]
        public async Task<IActionResult> RenewLicense(Guid id)
        {
            var license = await _context.Licenses.FindAsync(id);
            if (license == null) return NotFound("License not found");

            var oldDate = license.ExpiryDate;
            if (license.ExpiryDate < DateTime.UtcNow)
                license.ExpiryDate = DateTime.UtcNow.AddYears(1);
            else
                license.ExpiryDate = license.ExpiryDate?.AddYears(1);

            var expiryAlerts = await _context.ComplianceEvents
                .Where(e => e.LicenseId == id && e.Type == ComplianceEventType.Expiry && !e.IsResolved)
                .ToListAsync();

            foreach (var alert in expiryAlerts)
            {
                alert.IsResolved = true;
                alert.ResolutionNotes = "Auto-resolved via Renewal";
                alert.ResolvedByUserId = "Admin";
            }

            await LogAction("Renewed", "License", id.ToString(), $"Renewed {license.ProductName}. Old Expiry: {oldDate?.ToShortDateString()}");
            await _context.SaveChangesAsync();
            return Ok(new { message = "Renewed", newExpiryDate = license.ExpiryDate });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLicense(Guid id)
        {
            var license = await _context.Licenses.FindAsync(id);
            if (license == null) return NotFound();
            _context.Licenses.Remove(license);
            await LogAction("Deleted", "License", id.ToString(), $"Deleted license {license.ProductName}");
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}