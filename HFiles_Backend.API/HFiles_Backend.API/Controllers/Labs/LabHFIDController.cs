using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HFiles_Backend.Infrastructure.Data;
using HFiles_Backend.Application.DTOs.Labs;
using System.ComponentModel.DataAnnotations;
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





        // Verify HFID for Labs
        [HttpGet("labs/hfid")]
        public async Task<IActionResult> GetHFIDByEmail([FromQuery][Required] string email)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var lab = await _context.LabSignupUsers
                .Where(u => u.Email == email)
                .Select(u => new { u.Email, u.LabName, u.HFID })
                .FirstOrDefaultAsync();

            if (lab == null)
                return NotFound("Lab not found.");

            if (string.IsNullOrEmpty(lab.HFID))
                return NotFound("HFID not generated yet for this user.");

            return Ok(lab);
        }





        // Verify HFID for Users
        [HttpPost("users/hfid")]
        public async Task<IActionResult> GetUserDetails([FromBody] HFIDRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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
