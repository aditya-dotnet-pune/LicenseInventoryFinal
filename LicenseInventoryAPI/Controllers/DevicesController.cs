using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LicenseInventoryAPI.Data;
using LicenseInventoryAPI.Models;

namespace LicenseInventoryAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DevicesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DevicesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Devices
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Device>>> GetDevices()
        {
            return await _context.Devices.Include(d => d.InstalledSoftware).ToListAsync();
        }

        // GET: api/Devices/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Device>> GetDevice(Guid id)
        {
            var device = await _context.Devices
                .Include(d => d.InstalledSoftware)
                .FirstOrDefaultAsync(d => d.DeviceId == id);

            if (device == null) return NotFound();
            return device;
        }

        // POST: api/Devices
        [HttpPost]
        public async Task<ActionResult<Device>> OnboardDevice(Device device)
        {
            device.DeviceId = Guid.NewGuid();
            device.LastSeen = DateTime.UtcNow;
            _context.Devices.Add(device);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetDevices", new { id = device.DeviceId }, device);
        }

        // PUT: api/Devices/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDevice(Guid id, Device device)
        {
            if (id != device.DeviceId) return BadRequest();

            var existingDevice = await _context.Devices.FindAsync(id);
            if (existingDevice == null) return NotFound();

            existingDevice.Hostname = device.Hostname;
            existingDevice.OwnerUserId = device.OwnerUserId;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Devices/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDevice(Guid id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device == null) return NotFound();

            _context.Devices.Remove(device);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Devices/Install
        [HttpPost("Install")]
        public async Task<ActionResult<SoftwareInstallation>> AddInstallation(SoftwareInstallation installation)
        {
            installation.InstallationId = Guid.NewGuid();
            installation.InstallDate = DateTime.UtcNow;

            _context.SoftwareInstallations.Add(installation);
            await _context.SaveChangesAsync();

            return Ok(installation);
        }

        // NEW: DELETE Installation
        [HttpDelete("Install/{installationId}")]
        public async Task<IActionResult> RemoveInstallation(Guid installationId)
        {
            var installation = await _context.SoftwareInstallations.FindAsync(installationId);
            if (installation == null) return NotFound();

            _context.SoftwareInstallations.Remove(installation);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // NEW: UPDATE Installation
        [HttpPut("Install/{installationId}")]
        public async Task<IActionResult> UpdateInstallation(Guid installationId, SoftwareInstallation installation)
        {
            if (installationId != installation.InstallationId) return BadRequest();

            _context.Entry(installation).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.SoftwareInstallations.Any(e => e.InstallationId == installationId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }
    }
}