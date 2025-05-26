using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HFiles_Backend.Infrastructure.Data;
using HFiles_Backend.Application.DTOs.Labs;
namespace HFiles_Backend.API.Controllers.Labs

{
    [ApiController]
    [Route("api")]
    public class LabHFIDController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LabHFIDController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("LabHFID")]
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


        [HttpPost("verify-HFID")]
        public async Task<IActionResult> GetUserDetails([FromBody] HFIDRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.HFID))
                return BadRequest("HFID is required in the request body.");

            var userDetails = await _context.UserDetails
                .Where(u => u.user_membernumber == dto.HFID)
                .Select(u => new
                {
                    Username = $"{u.user_firstname} {u.user_lastname}",
                    UserEmail = u.user_email
                })
                .FirstOrDefaultAsync();

            if (userDetails == null)
                return NotFound($"No user found with HFID {dto.HFID}.");

            return Ok(userDetails);
        }


    }
}
