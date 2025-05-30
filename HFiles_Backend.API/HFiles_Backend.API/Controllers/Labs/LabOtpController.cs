using HFiles_Backend.Domain.Entities;
using HFiles_Backend.Infrastructure.Data;
using HFiles_Backend.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using HFiles_Backend.Application.DTOs.Labs;
using HFiles_Backend.Domain.Entities.Labs;

namespace HFiles_Backend.API.Controllers.Labs
{
    [ApiController]
    [Route("api/")]
    public class LabOtpController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;
        private readonly IWhatsappService _whatsappService;

        public LabOtpController(AppDbContext context, EmailService emailService, IWhatsappService whatsappService)
        {
            _context = context;
            _emailService = emailService;
            _whatsappService = whatsappService;
        }





        // Generates OTP for Signup
        [HttpPost("labs/signup/otp")]
        public async Task<IActionResult> GenerateOtp([FromBody] OtpRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var otp = new Random().Next(100000, 999999).ToString();
            var now = DateTime.UtcNow;

            var expiredOtps = await _context.LabOtpEntries
                .Where(x => x.Email == dto.Email && x.ExpiryTime < now)
                .ToListAsync();
            _context.LabOtpEntries.RemoveRange(expiredOtps);

            var otpEntry = new LabOtpEntry
            {
                Email = dto.Email,
                OtpCode = otp,
                CreatedAt = now,
                ExpiryTime = now.AddMinutes(5)
            };

            _context.LabOtpEntries.Add(otpEntry);
            await _context.SaveChangesAsync();

            var subject = "Complete Your Hfiles Lab Registration";
            var body = $@"
                       <p>Hello <strong>{dto.LabName}</strong>,</p>
                       <p>Welcome to Hfiles!</p>
                       <p>To complete your registration, please use the following One-Time Password (OTP):</p>
                       <h2>{otp}</h2>
                       <p>Please keep this OTP secure and use it to finalize your registration process.</p>
                       <p>If you have any questions or need assistance, feel free to reach out to our support team at <a href='mailto:contact@hfiles.in'>contact@hfiles.in</a>.</p>
                       <p>Thank you for joining us!</p>
                       <p>Best regards,<br>The Hfiles Team</p>
                       <p style='font-size:small; color:gray;'>If you did not sign up for Hfiles, please disregard this email.</p>
                       ";

            try
            {
                await _emailService.SendEmailAsync(dto.Email, subject, body);
                await _whatsappService.SendOtpAsync(otpEntry.OtpCode, dto.PhoneNumber);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"User created, but failed to send notification: {ex.Message}");
            }

            return Ok(new
            {
                message = "OTP has been sent to your email and phonenumber.",
                expiresInMinutes = 5
            });
        }
    }
}
