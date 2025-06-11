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
    public class LabHFIDController(AppDbContext context, LabAuthorizationService labAuthorizationService, ILogger<LabHFIDController> logger) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly LabAuthorizationService _labAuthorizationService = labAuthorizationService;
        private readonly ILogger<LabHFIDController> _logger = logger;





        // Verify HFID for Labs
        [HttpGet("labs/hfid")]
        public async Task<IActionResult> GetHFIDByEmail([FromQuery][Required] string email)
        {
            HttpContext.Items["Log-Category"] = "Identity Verification";

            _logger.LogInformation("Received request to fetch HFID for Email: {Email}", email);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("Validation failed: {@Errors}", errors);
                return BadRequest(ApiResponseFactory.Fail(errors));
            }

            try
            {
                var lab = await _context.LabSignups
                    .Where(u => u.Email == email)
                    .Select(u => new { u.Email, u.LabName, u.HFID })
                    .FirstOrDefaultAsync();

                if (lab == null)
                {
                    _logger.LogWarning("HFID retrieval failed: Lab with Email {Email} not found.", email);
                    return NotFound(ApiResponseFactory.Fail($"Lab with email '{email}' not found."));
                }

                if (string.IsNullOrEmpty(lab.HFID))
                {
                    _logger.LogWarning("HFID retrieval failed: No HFID generated yet for Email {Email}.", email);
                    return NotFound(ApiResponseFactory.Fail("HFID has not been generated yet for this user."));
                }

                _logger.LogInformation("Successfully fetched HFID for Email {Email}. Returning response.", email);
                return Ok(ApiResponseFactory.Success(lab, "HFID retrieved successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HFID retrieval failed due to an unexpected error for Email {Email}", email);
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }






        // Verify HFID for Users
        [HttpPost("users/hfid")]
        public async Task<IActionResult> GetUserDetails([FromBody] HFIDRequest dto)
        {
            HttpContext.Items["Log-Category"] = "Identity Verification";

            _logger.LogInformation("Received request to fetch user details for HFID: {HFID}", dto.HFID);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("Validation failed: {@Errors}", errors);
                return BadRequest(ApiResponseFactory.Fail(errors));
            }

            try
            {
                var userDetails = await _context.UserDetails
                    .Where(u => u.user_membernumber == dto.HFID)
                    .Select(u => new
                    {
                        Username = $"{u.user_firstname} {u.user_lastname}",
                        UserEmail = u.user_email
                    })
                    .FirstOrDefaultAsync();

                if (userDetails == null)
                {
                    _logger.LogWarning("User details retrieval failed: No user found with HFID {HFID}", dto.HFID);
                    return NotFound(ApiResponseFactory.Fail($"No user found with HFID '{dto.HFID}'"));
                }

                _logger.LogInformation("Successfully fetched user details for HFID {HFID}. Returning response.", dto.HFID);
                return Ok(ApiResponseFactory.Success(userDetails, "User details retrieved successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User details retrieval failed due to an unexpected error for HFID {HFID}", dto.HFID);
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }
    }
}
