using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LicenseInventoryAPI.Data;
using LicenseInventoryAPI.Models;

namespace LicenseInventoryAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Simple DTO for receiving data
        public class UserLoginRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
        {
            // 1. Direct String Matching in Database
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.Password == request.Password);

            // 2. If no match found, return 401
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid username or password" });
            }

            // 3. Match found! Return the user info (Role is key for frontend)
            return Ok(new
            {
                UserId = user.UserId,
                Username = user.Username,
                Role = user.Role
            });
        }
    }
}