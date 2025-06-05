using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HFiles_Backend.Infrastructure.Data;
using HFiles_Backend.Application.DTOs.Labs;
using System.ComponentModel.DataAnnotations;
using HFiles_Backend.API.Services;
using HFiles_Backend.Application.Common;
using Microsoft.AspNetCore.Authorization;
namespace HFiles_Backend.API.Controllers.Labs

{
    [ApiController]
    [Route("api")]
    public class LabHFIDController(AppDbContext context, LabAuthorizationService labAuthorizationService) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly LabAuthorizationService _labAuthorizationService = labAuthorizationService;





        // Verify HFID for Labs
        [HttpGet("labs/hfid")]
        public async Task<IActionResult> GetHFIDByEmail([FromQuery][Required] string email)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(ApiResponseFactory.Fail(errors));
                }

                var lab = await _context.LabSignups
                    .Where(u => u.Email == email)
                    .Select(u => new { u.Email, u.LabName, u.HFID })
                    .FirstOrDefaultAsync();

                if (lab == null)
                    return NotFound(ApiResponseFactory.Fail($"Lab with email '{email}' not found."));

                if (string.IsNullOrEmpty(lab.HFID))
                    return NotFound(ApiResponseFactory.Fail("HFID has not been generated yet for this user."));

                return Ok(ApiResponseFactory.Success(lab, "HFID retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }





        // Verify HFID for Users
        [HttpPost("users/hfid")]
        public async Task<IActionResult> GetUserDetails([FromBody] HFIDRequest dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(ApiResponseFactory.Fail(errors));
                }

                var userDetails = await _context.UserDetails
                    .Where(u => u.user_membernumber == dto.HFID)
                    .Select(u => new
                    {
                        Username = $"{u.user_firstname} {u.user_lastname}",
                        UserEmail = u.user_email
                    })
                    .FirstOrDefaultAsync();

                if (userDetails == null)
                    return NotFound(ApiResponseFactory.Fail($"No user found with HFID '{dto.HFID}'"));

                return Ok(ApiResponseFactory.Success(userDetails, "User details retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }
    }
}
