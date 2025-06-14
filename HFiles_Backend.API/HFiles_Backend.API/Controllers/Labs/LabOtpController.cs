using System.Security.Cryptography;
using HFiles_Backend.API.Services;
using HFiles_Backend.Application.Common;
using HFiles_Backend.Application.DTOs.Labs;
using HFiles_Backend.Domain.Entities.Labs;
using HFiles_Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HFiles_Backend.API.Controllers.Labs
{
    [ApiController]
    [Route("api/")]
    public class LabOtpController(AppDbContext context, EmailService emailService, IWhatsappService whatsappService, ILogger<LabOtpController> logger) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly EmailService _emailService = emailService;
        private readonly IWhatsappService _whatsappService = whatsappService;
        private readonly ILogger<LabOtpController> _logger = logger;





        // Generates OTP for Signup
        [HttpPost("labs/signup/otp")]
        public async Task<IActionResult> GenerateOtp([FromBody] OtpRequest dto)
        {
            HttpContext.Items["Log-Category"] = "Authentication";

            _logger.LogInformation("Received OTP generation request for Email: {Email}, Phone Number: {PhoneNumber}", dto.Email, dto.PhoneNumber);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("Validation failed: {@Errors}", errors);
                return BadRequest(ApiResponseFactory.Fail(errors));
            }

            try
            {
                var otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
                var now = DateTime.UtcNow;

                var otpEntry = new LabOtpEntry
                {
                    Email = dto.Email,
                    OtpCode = otp,
                    CreatedAt = now,
                    ExpiryTime = now.AddMinutes(5)
                };

                _context.LabOtpEntries.Add(otpEntry);
                await _context.SaveChangesAsync();

                var expiredOtps = await _context.LabOtpEntries
                   .Where(x => x.Email == dto.Email && x.ExpiryTime < now)
                   .ToListAsync();
                _context.LabOtpEntries.RemoveRange(expiredOtps);
                _context.LabOtpEntries.Remove(otpEntry);
                await _context.SaveChangesAsync();

                _logger.LogInformation("OTP generated for Email {Email}. OTP: {Otp}, Expiry Time: {ExpiryTime}", dto.Email, otp, otpEntry.ExpiryTime);

                var subject = "Complete Your Hfiles Lab Registration";
                var body = $@"
                <p>Hello <strong>{dto.LabName}</strong>,</p>
                <p>Welcome to Hfiles!</p>
                <p>To complete your registration, please use the following One-Time Password (OTP):</p>
                <h2>{otp}</h2>
                <p>This OTP will expire in 5 minutes.</p>
                <p>For support, contact us at <a href='mailto:contact@hfiles.in'>contact@hfiles.in</a>.</p>
                <p>Best regards,<br>The Hfiles Team</p>
                <p style='font-size:small; color:gray;'>If you did not sign up for Hfiles, please disregard this email.</p>
                ";

                await _emailService.SendEmailAsync(dto.Email, subject, body);
                await _whatsappService.SendOtpAsync(otp, dto.PhoneNumber);

                _logger.LogInformation("OTP sent successfully to Email {Email} and Phone Number {PhoneNumber}.", dto.Email, dto.PhoneNumber);

                var result = new
                {
                    dto.Email,
                    dto.PhoneNumber,
                    ExpiresInMinutes = 5
                };

                return Ok(ApiResponseFactory.Success(result, "OTP has been sent to your email and phone number."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OTP generation failed due to an unexpected error for Email {Email}", dto.Email);
                return StatusCode(500, ApiResponseFactory.Fail($"OTP generated but failed to send notification: {ex.Message}"));
            }
        }
    }
}
