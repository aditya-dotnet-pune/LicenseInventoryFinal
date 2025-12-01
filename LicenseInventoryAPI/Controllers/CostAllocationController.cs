using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LicenseInventoryAPI.Data;
using LicenseInventoryAPI.Models;

namespace LicenseInventoryAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CostAllocationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CostAllocationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/CostAllocation
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CostAllocation>>> GetAllocations()
        {
            return await _context.CostAllocations
                .Include(c => c.License)
                .ToListAsync();
        }

        // POST: api/CostAllocation
        [HttpPost]
        public async Task<ActionResult<CostAllocation>> CreateAllocation(CostAllocation allocation)
        {
            allocation.AllocationId = Guid.NewGuid();
            _context.CostAllocations.Add(allocation);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetAllocations", new { id = allocation.AllocationId }, allocation);
        }

        // GET: api/CostAllocation/ByDepartment
        [HttpGet("ByDepartment")]
        public async Task<ActionResult<object>> GetCostByDepartment()
        {
            var allocations = await _context.CostAllocations.ToListAsync();

            // Group by DepartmentId and Sum the Amount
            var stats = allocations
                .GroupBy(a => a.DepartmentId)
                .Select(g => new
                {
                    Department = g.Key,
                    TotalAllocated = g.Sum(a => a.AllocatedAmount),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.TotalAllocated)
                .ToList();

            return Ok(stats);
        }
    }
}