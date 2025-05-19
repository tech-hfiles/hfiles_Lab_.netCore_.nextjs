using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HFiles_Backend.Infrastructure.Data;

namespace HFiles_Backend.API.Controllers.Labs
{
    [ApiController]
    [Route("api/[controller]")]
    public class LabHFIDController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LabHFIDController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/LabHFID?email=abc@example.com
        [HttpGet]
        public async Task<IActionResult> GetHFIDByEmail([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest("Email is required.");

            var user = await _context.LabSignupUsers
                .Where(u => u.Email == email)
                .Select(u => new { u.Email, u.LabName, u.HFID })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound("Lab not found.");

            if (string.IsNullOrEmpty(user.HFID))
                return NotFound("HFID not generated yet for this user.");

            return Ok(new
            {
                user.Email,
                user.LabName,
                user.HFID
            });
        }
    }
}
