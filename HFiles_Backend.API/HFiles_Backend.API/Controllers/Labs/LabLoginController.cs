using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using HFiles_Backend.Infrastructure.Data;
using HFiles_Backend.Domain.Entities.Labs;
using HFiles_Backend.API.Services;
using HFiles_Backend.Application.DTOs.Labs;
using System.Threading.Tasks;
using HFiles_Backend.Application.Common;
using System.Security.Cryptography;

namespace HFiles_Backend.API.Controllers.Labs
{
    [ApiController]
    [Route("api/")]
    public class LabLoginController(
        AppDbContext context,
        EmailService emailService,
        IPasswordHasher<LabSignup> passwordHasher) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly EmailService _emailService = emailService;
        private readonly IPasswordHasher<LabSignup> _passwordHasher = passwordHasher;
        private const int OtpValidityMinutes = 5;





        // Sends OTP
        [HttpPost("labs/otp")]
        public async Task<IActionResult> SendOtp([FromBody] EmailRequest dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponseFactory.Fail(errors));
            }

            try
            {
                var user = await _context.LabSignups.FirstOrDefaultAsync(u => u.Email == dto.Email);
                if (user == null)
                    return BadRequest(ApiResponseFactory.Fail("Email not registered."));

                var otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

                var otpEntry = new LabOtpEntry
                {
                    Email = dto.Email,
                    OtpCode = otp,
                    CreatedAt = DateTime.UtcNow,
                    ExpiryTime = DateTime.UtcNow.AddMinutes(OtpValidityMinutes)
                };

                await _context.LabOtpEntries.AddAsync(otpEntry);
                await _context.SaveChangesAsync();

                var body = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
                            <p>Hello,</p>
                            <p>Your OTP for <strong>Hfiles</strong> login is:</p>
                            <h2 style='color: #333;'>{otp}</h2>
                            <p>This OTP is valid for <strong>{OtpValidityMinutes} minutes</strong>.</p>
                            <p>If you didn’t request this, you can ignore this email.</p>
                            <br/>
                            <p>Best regards,<br/>The Hfiles Team</p>
                        </body>
                        </html>";

                await _emailService.SendEmailAsync(dto.Email, "Your Hfiles Login OTP", body);

                return Ok(ApiResponseFactory.Success("OTP sent successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }





        // Login via Email + OTP
        [HttpPost("labs/login/otp")]
        public async Task<IActionResult> LoginViaOtp([FromBody] OtpLogin dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponseFactory.Fail(errors));
            }

            try
            {
                var user = await _context.LabSignups.FirstOrDefaultAsync(u => u.Email == dto.Email);
                if (user == null)
                    return BadRequest(ApiResponseFactory.Fail("Email not registered."));

                var otpEntry = await _context.LabOtpEntries
                    .Where(o => o.Email == dto.Email)
                    .OrderByDescending(o => o.CreatedAt)
                    .FirstOrDefaultAsync();

                if (otpEntry == null || otpEntry.ExpiryTime < DateTime.UtcNow)
                    return BadRequest(ApiResponseFactory.Fail("OTP expired or not found."));

                if (otpEntry.OtpCode != dto.Otp)
                    return BadRequest(ApiResponseFactory.Fail("Invalid OTP."));

                var responseData = new
                {
                    UserId = user.Id,
                    user.Email,
                    user.IsSuperAdmin
                };

                return Ok(ApiResponseFactory.Success(responseData, "Lab login successful, proceed to LabAdmin login."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }





        // Login via Email + Password
        [HttpPost("labs/login/password")]
        public async Task<IActionResult> LoginViaPassword([FromBody] PasswordLogin dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponseFactory.Fail(errors));
            }

            try
            {
                var user = await _context.LabSignups.FirstOrDefaultAsync(u => u.Email == dto.Email);
                if (user == null)
                    return BadRequest(ApiResponseFactory.Fail("Email not registered."));

                if (string.IsNullOrEmpty(user.PasswordHash))
                    return BadRequest(ApiResponseFactory.Fail("Password is not set for this account."));

                var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
                if (result == PasswordVerificationResult.Failed)
                    return BadRequest(ApiResponseFactory.Fail("Incorrect password."));

                var responseData = new
                {
                    UserId = user.Id,
                    user.Email,
                    user.IsSuperAdmin
                };

                return Ok(ApiResponseFactory.Success(responseData, "Lab login successful, proceed to LabAdmin login."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }
    }
}