using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using HFiles_Backend.Infrastructure.Data;
using HFiles_Backend.API.Services;
using System.Threading.Tasks;
using System.Linq;
using HFiles_Backend.Application.DTOs.Labs;
using HFiles_Backend.Domain.Entities.Labs;
using Microsoft.AspNetCore.Authorization;
using HFiles_Backend.Application.Common;

namespace HFiles_Backend.API.Controllers.Labs
{
    [ApiController]
    [Route("api/")]
    public class LabSignupUserController(
        AppDbContext context,
        IPasswordHasher<LabSignup> passwordHasher,
        EmailService emailService,
        ILogger<LabSignupUserController> logger,
        LabAuthorizationService labAuthorizationService) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly IPasswordHasher<LabSignup> _passwordHasher = passwordHasher;
        private readonly EmailService _emailService = emailService;
        private readonly ILogger<LabSignupUserController> _logger = logger;
        private readonly LabAuthorizationService _labAuthorizationService = labAuthorizationService;





        // Creates Lab 
        [HttpPost("labs")]
        public async Task<IActionResult> Signup([FromBody] Signup dto)
        {
            HttpContext.Items["Log-Category"] = "Lab Management";

            _logger.LogInformation("Received lab signup request for Lab Name: {LabName}, Email: {Email}", dto.LabName, dto.Email);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("Validation failed: {@Errors}", errors);
                return BadRequest(ApiResponseFactory.Fail(errors));
            }

            try
            {
                if (await _context.LabSignups.AnyAsync(u => u.Email == dto.Email))
                {
                    _logger.LogWarning("Lab signup failed: Email {Email} already registered.", dto.Email);
                    return BadRequest(ApiResponseFactory.Fail("Email already registered."));
                }

                var otpEntry = await _context.LabOtpEntries
                    .Where(o => o.Email == dto.Email)
                    .OrderByDescending(o => o.CreatedAt)
                    .FirstOrDefaultAsync();

                if (otpEntry == null)
                {
                    _logger.LogWarning("Lab signup failed: No OTP found for Email {Email}.", dto.Email);
                    return BadRequest(ApiResponseFactory.Fail("OTP not generated for this email."));
                }

                if (otpEntry.ExpiryTime < DateTime.UtcNow)
                {
                    _logger.LogWarning("Lab signup failed: OTP expired for Email {Email}.", dto.Email);
                    return BadRequest(ApiResponseFactory.Fail("OTP has expired."));
                }

                if (otpEntry.OtpCode != dto.Otp)
                {
                    _logger.LogWarning("Lab signup failed: Invalid OTP entered for Email {Email}.", dto.Email);
                    return BadRequest(ApiResponseFactory.Fail("Invalid OTP."));
                }

                const string HFilesEmail = "hfilessocial@gmail.com";
                const string HFIDPrefix = "HF";
                var epochTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var last6Epoch = epochTime % 1000000;
                var labPrefix = dto.LabName.Length >= 3 ? dto.LabName.Substring(0, 3).ToUpper() : dto.LabName.ToUpper();
                var randomDigits = new Random().Next(1000, 9999);
                var hfid = $"{HFIDPrefix}{last6Epoch}{labPrefix}{randomDigits}";

                var user = new LabSignup
                {
                    LabName = dto.LabName,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    Pincode = dto.Pincode,
                    CreatedAtEpoch = epochTime,
                    HFID = hfid,
                    IsSuperAdmin = false,
                    PasswordHash = _passwordHasher.HashPassword(null!, dto.Password)
                };

                _context.LabSignups.Add(user);
                _context.LabOtpEntries.Remove(otpEntry);

                await _context.SaveChangesAsync();

                HttpContext.Items["CreatedLabId"] = user.Id;
                _logger.LogInformation("Lab signup successful. Lab ID: {LabId}, HFID: {HFID}", user.Id, hfid);

                await _emailService.SendEmailAsync(dto.Email, "Hfiles Registration Received", $@"<html>...</html>");
                await _emailService.SendEmailAsync(HFilesEmail, "New Lab Signup", $@"<html>...</html>");

                return Ok(ApiResponseFactory.Success(new { user.IsSuperAdmin }, "User registered successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lab signup failed due to an unexpected error for Email {Email}", dto.Email);
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }





        // Update Address and Profile Photo for Lab
        [HttpPatch("labs/update")]
        [Authorize]
        public async Task<IActionResult> UpdateLabUserProfile([FromForm] ProfileUpdate dto, IFormFile? ProfilePhoto)
        {
            HttpContext.Items["Log-Category"] = "Lab Management";

            _logger.LogInformation("Received profile update request for Lab ID: {LabId}", dto.Id);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("Validation failed: {@Errors}", errors);
                return BadRequest(ApiResponseFactory.Fail(errors));
            }

            try
            {
                var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
                {
                    _logger.LogWarning("Promotion failed: Invalid or missing lab ID in token.");
                    return Unauthorized(ApiResponseFactory.Fail("Invalid or missing labId in token."));
                }

                if (!await _labAuthorizationService.IsLabAuthorized(labId, User))
                {
                    _logger.LogWarning("Branch creation failed: Lab ID {LabId} is not authorized.", labId);
                    return Unauthorized(ApiResponseFactory.Fail("Permission denied. You can only create/modify/delete data for your main lab or its branches."));
                }

                var loggedInLab = await _context.LabSignups.FirstOrDefaultAsync(l => l.Id == labId);
                if (loggedInLab == null)
                {
                    _logger.LogWarning("Promotion failed: Lab ID {LabId} not found.", labId);
                    return BadRequest(ApiResponseFactory.Fail("Lab not found"));
                }

                int mainLabId = loggedInLab.LabReference == 0 ? labId : loggedInLab.LabReference;

                var branchIds = await _context.LabSignups
                    .Where(l => l.LabReference == mainLabId)
                    .Select(l => l.Id)
                    .ToListAsync();

                branchIds.Add(mainLabId);

                var user = await _context.LabSignups
                    .Where(l => (branchIds.Contains(l.Id) || l.Id == mainLabId) && l.Id == dto.Id && l.DeletedBy == 0) 
                    .FirstOrDefaultAsync();
                if (user == null)
                {
                    _logger.LogWarning("Profile update failed: Lab user with ID {LabId} not found.", dto.Id);
                    return NotFound(ApiResponseFactory.Fail($"Lab user with ID {dto.Id} not found."));
                }

                if (!string.IsNullOrEmpty(dto.Address))
                {
                    _logger.LogInformation("Updating address for Lab ID {LabId}.", dto.Id);
                    user.Address = dto.Address;
                }

                if (ProfilePhoto != null && ProfilePhoto.Length > 0)
                {
                    _logger.LogInformation("Received new profile photo for Lab ID {LabId}. Processing upload.", dto.Id);

                    string uploadsFolder = Path.Combine("wwwroot", "uploads");
                    Directory.CreateDirectory(uploadsFolder);

                    DateTime createdAt = DateTimeOffset.FromUnixTimeSeconds(user.CreatedAtEpoch).UtcDateTime;
                    string formattedTime = createdAt.ToString("dd-MM-yyyy-HH-mm-ss");

                    string fileName = $"{Path.GetFileNameWithoutExtension(ProfilePhoto.FileName)}_{formattedTime}{Path.GetExtension(ProfilePhoto.FileName)}";
                    string filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ProfilePhoto.CopyToAsync(stream);
                    }

                    _logger.LogInformation("Profile photo uploaded successfully for Lab ID {LabId}. File Name: {FileName}", dto.Id, fileName);
                    user.ProfilePhoto = fileName;
                }

                _context.LabSignups.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Profile updated successfully for Lab ID {LabId}. Returning response.", dto.Id);

                return Ok(ApiResponseFactory.Success(new
                {
                    LabId = user.Id,
                    UpdatedAddress = user.Address,
                    UpdatedProfilePhoto = user.ProfilePhoto
                }, "Profile updated successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Profile update failed due to an unexpected error for Lab ID {LabId}", dto.Id);
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }
    }
}
