using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LicenseInventoryAPI.Data;
using LicenseInventoryAPI.Models;

namespace LicenseInventoryAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuditController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuditController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Audit
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AuditLog>>> GetAuditLogs()
        {
            // Return newest logs first
            return await _context.AuditLogs
                .OrderByDescending(a => a.Timestamp)
                .Take(100) // Limit to last 100 for performance
                .ToListAsync();
        }
    }
}