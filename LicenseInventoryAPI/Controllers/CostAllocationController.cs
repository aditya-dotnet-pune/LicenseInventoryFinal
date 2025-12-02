using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LicenseInventoryAPI.Data;
using LicenseInventoryAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LicenseInventoryAPI.Controllers
{
    // DTO for the Percentage Rule
    public class AllocationRequest
    {
        public Guid LicenseId { get; set; }
        public int HR { get; set; }
        public int Sales { get; set; }
        public int Engineering { get; set; }
    }

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
                .OrderByDescending(c => c.PeriodStart)
                .ToListAsync();
        }

        // POST: api/CostAllocation/AllocateByRule
        // New Endpoint for Admin-defined Percentage Split
        [HttpPost("AllocateByRule")]
        public async Task<IActionResult> AllocateByRule([FromBody] AllocationRequest request)
        {
            if (request.HR + request.Sales + request.Engineering != 100)
            {
                return BadRequest("Percentages must sum up to exactly 100%.");
            }

            var license = await _context.Licenses.FindAsync(request.LicenseId);
            if (license == null) return NotFound("License not found");

            // 1. Cleanup: Remove existing allocations for this license to avoid duplicates
            var existing = _context.CostAllocations.Where(c => c.LicenseId == request.LicenseId);
            _context.CostAllocations.RemoveRange(existing);

            // 2. Create Allocations based on Input Percentages
            var allocations = new List<CostAllocation>();

            // Helper to create object
            void AddAllocation(string dept, int percentage)
            {
                if (percentage <= 0) return; // Skip 0% depts

                allocations.Add(new CostAllocation
                {
                    AllocationId = Guid.NewGuid(),
                    LicenseId = request.LicenseId,
                    DepartmentId = dept,
                    AllocationMethod = AllocationMethod.Fixed,
                    // Amount = (Total Cost * Percentage) / 100
                    AllocatedAmount = (license.Cost * percentage) / 100,
                    Currency = license.Currency ?? "INR",
                    PeriodStart = DateTime.UtcNow,
                    PeriodEnd = DateTime.UtcNow.AddYears(1)
                });
            }

            AddAllocation("HR", request.HR);
            AddAllocation("Sales", request.Sales);
            AddAllocation("Engineering", request.Engineering);

            _context.CostAllocations.AddRange(allocations);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cost allocated successfully based on defined rule." });
        }

        // GET: api/CostAllocation/ByDepartment
        [HttpGet("ByDepartment")]
        public async Task<ActionResult<object>> GetCostByDepartment()
        {
            var allocations = await _context.CostAllocations.ToListAsync();

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