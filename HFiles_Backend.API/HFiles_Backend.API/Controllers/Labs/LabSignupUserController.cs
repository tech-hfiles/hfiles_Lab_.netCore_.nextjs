using HFiles_Backend.API.Services;
using HFiles_Backend.Application.Common;
using HFiles_Backend.Application.DTOs.Labs;
using HFiles_Backend.Domain.Entities.Labs;
using HFiles_Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HFiles_Backend.API.Controllers.Labs
{
    [ApiController]
    [Route("api/")]
    public class LabSignupUserController(
        AppDbContext context,
        IPasswordHasher<LabSignup> passwordHasher,
        EmailService emailService,
        ILogger<LabSignupUserController> logger,
        LabAuthorizationService labAuthorizationService,
        IWebHostEnvironment env,
        S3StorageService s3Service) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly IPasswordHasher<LabSignup> _passwordHasher = passwordHasher;
        private readonly EmailService _emailService = emailService;
        private readonly ILogger<LabSignupUserController> _logger = logger;
        private readonly LabAuthorizationService _labAuthorizationService = labAuthorizationService;
        private readonly IWebHostEnvironment _env = env;
        private readonly S3StorageService _s3Service = s3Service;





        // Creates Lab 
        [HttpPost("labs")]
        public async Task<IActionResult> Signup([FromBody] Signup dto)
        {
            HttpContext.Items["Log-Category"] = "Lab Management";

            _logger.LogInformation("Signup attempt initiated for Email: {Email}", dto.Email);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                _logger.LogWarning("Model validation failed for Email: {Email}. Errors: {Errors}", dto.Email, string.Join(", ", errors));
                return BadRequest(ApiResponseFactory.Fail(errors));
            }

            try
            {
                if (await _context.LabSignups.AnyAsync(u => u.Email == dto.Email))
                {
                    _logger.LogWarning("Signup failed: Email already registered - {Email}", dto.Email);
                    return BadRequest(ApiResponseFactory.Fail("Email already registered."));
                }

                var otpEntry = await _context.LabOtpEntries
                    .Where(o => o.Email == dto.Email)
                    .OrderByDescending(o => o.CreatedAt)
                    .FirstOrDefaultAsync();

                if (otpEntry == null)
                {
                    _logger.LogWarning("Signup failed: OTP not found for Email: {Email}", dto.Email);
                    return BadRequest(ApiResponseFactory.Fail("OTP not generated for this email."));
                }

                if (otpEntry.ExpiryTime < DateTime.UtcNow)
                {
                    _logger.LogWarning("Signup failed: OTP expired for Email: {Email}", dto.Email);
                    return BadRequest(ApiResponseFactory.Fail("OTP has expired."));
                }

                if (otpEntry.OtpCode != dto.Otp)
                {
                    _logger.LogWarning("Signup failed: Invalid OTP entered for Email: {Email}", dto.Email);
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
                    IsSuperAdmin = false
                };

                user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

                _context.LabSignups.Add(user);
                _context.LabOtpEntries.Remove(otpEntry);
                await _context.SaveChangesAsync();

                _logger.LogInformation("New lab user registered successfully. Email: {Email}, HFID: {HFID}", dto.Email, hfid);

                var userEmailBody = $@" 
                            <html>
                            <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
                                <p>Hello <strong>{dto.LabName}</strong>,</p>
                                <p>Welcome to <strong>Hfiles</strong>!</p>
                                <p>Your registration has been successfully received. You will be contacted for further steps regarding your login. A representative from Hfiles will reach out to you soon.</p>
                                <p>If you have any questions, feel free to contact us at <a href='mailto:contact@hfiles.in'>contact@hfiles.in</a>.</p>
                                <p>Best regards,<br/> The Hfiles Team</p>
                                <p style='font-size: 0.85em; color: #888;'>If you did not sign up for Hfiles, please ignore this email.</p>
                            </body>
                            </html>";

                var adminEmailBody = $@" 
                            <html>
                            <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
                                <h2>New Lab Signup Received</h2>
                                <p><strong>Lab Name:</strong> {dto.LabName}<br/>
                                <strong>Email:</strong> {dto.Email}<br/>
                                <strong>Phone:</strong> {dto.PhoneNumber}<br/>
                                <strong>Pincode:</strong> {dto.Pincode}</p>
                                <p>Please follow up with the lab for onboarding.</p>
                                <p>— Hfiles System</p>
                            </body>
                            </html>";

                await _emailService.SendEmailAsync(dto.Email, "Hfiles Registration Received", userEmailBody);
                await _emailService.SendEmailAsync(HFilesEmail, "New Lab Signup", adminEmailBody);

                _logger.LogInformation("Welcome and admin emails sent for Email: {Email}", dto.Email);

                return Ok(ApiResponseFactory.Success(new { user.IsSuperAdmin }, "User registered successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Signup for Email: {Email}", dto.Email);
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }





        // Update Address and Profile Photo for Lab
        [HttpPatch("labs/update")]
        [Authorize]
        public async Task<IActionResult> UpdateLabUserProfile([FromForm] ProfileUpdate dto, IFormFile? ProfilePhoto)
        {
            HttpContext.Items["Log-Category"] = "Lab Management";

            _logger.LogInformation("UpdateLabUserProfile started for LabUserId: {Id}", dto.Id);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                _logger.LogWarning("Model validation failed for LabUserId: {Id}. Errors: {Errors}", dto.Id, string.Join(", ", errors));
                return BadRequest(ApiResponseFactory.Fail(errors));
            }

            try
            {
                var user = await _context.LabSignups.FirstOrDefaultAsync(l => l.Id == dto.Id);
                if (user == null)
                {
                    _logger.LogWarning("Lab user not found for ID: {Id}", dto.Id);
                    return NotFound(ApiResponseFactory.Fail($"Lab user with ID {dto.Id} not found."));
                }

                if (!string.IsNullOrEmpty(dto.Address))
                {
                    user.Address = dto.Address;
                    _logger.LogInformation("Updated address for LabUserId: {Id}", dto.Id);
                }

                if (ProfilePhoto != null && ProfilePhoto.Length > 0)
                {
                    string tempFolder = Path.Combine(_env.WebRootPath ?? "wwwroot", "temp-profiles");
                    if (!Directory.Exists(tempFolder)) Directory.CreateDirectory(tempFolder);

                    DateTime createdAt = DateTimeOffset.FromUnixTimeSeconds(user.CreatedAtEpoch).UtcDateTime;
                    string formattedTime = createdAt.ToString("dd-MM-yyyy_HH-mm-ss");
                    string fileName = $"{Path.GetFileNameWithoutExtension(ProfilePhoto.FileName)}_{formattedTime}{Path.GetExtension(ProfilePhoto.FileName)}";

                    var tempFilePath = Path.Combine(tempFolder, fileName);

                    using (var stream = new FileStream(tempFilePath, FileMode.Create))
                    {
                        await ProfilePhoto.CopyToAsync(stream);
                    }

                    var s3Key = $"profiles/{fileName}";
                    var s3Url = await _s3Service.UploadFileToS3(tempFilePath, s3Key);

                    if (System.IO.File.Exists(tempFilePath)) System.IO.File.Delete(tempFilePath);

                    user.ProfilePhoto = s3Url;
                    _logger.LogInformation("Profile photo updated for LabUserId: {Id}. File saved: {FileName}", dto.Id, fileName);
                }

                _context.LabSignups.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Profile update completed successfully for LabUserId: {Id}", dto.Id);

                return Ok(ApiResponseFactory.Success(new
                {
                    LabId = user.Id,
                    UpdatedAddress = user.Address,
                    UpdatedProfilePhoto = user.ProfilePhoto
                }, "Profile updated successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while updating profile for LabUserId: {Id}", dto.Id);
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }
    }
}
