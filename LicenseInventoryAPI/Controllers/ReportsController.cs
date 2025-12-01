using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LicenseInventoryAPI.Data;

namespace LicenseInventoryAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Reports/Dashboard
        [HttpGet("Dashboard")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var totalLicenses = await _context.Licenses.CountAsync();
            var totalDevices = await _context.Devices.CountAsync();
            var totalSpend = await _context.Licenses.SumAsync(l => l.Cost);
            var activeAlerts = await _context.ComplianceEvents.CountAsync(e => !e.IsResolved);

            var topVendors = await _context.Licenses
                .GroupBy(l => l.Vendor)
                .Select(g => new { Vendor = g.Key, Count = g.Count(), TotalCost = g.Sum(l => l.Cost) })
                .OrderByDescending(x => x.TotalCost)
                .Take(5)
                .ToListAsync();

            return Ok(new
            {
                TotalLicenses = totalLicenses,
                TotalDevices = totalDevices,
                TotalSpend = totalSpend,
                ActiveAlerts = activeAlerts,
                TopVendors = topVendors
            });
        }
    }
}