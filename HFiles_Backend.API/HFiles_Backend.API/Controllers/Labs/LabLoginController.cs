using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using HFiles_Backend.Infrastructure.Data;
using HFiles_Backend.Domain.Entities.Labs;
using HFiles_Backend.API.Services;
using HFiles_Backend.Application.DTOs.Labs;
using System.Threading.Tasks;

namespace HFiles_Backend.API.Controllers.Labs
{
    [ApiController]
    [Route("api/[controller]")]
    public class LabLoginController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;
        private readonly IPasswordHasher<LabSignupUser> _passwordHasher;

        public LabLoginController(
            AppDbContext context,
            EmailService emailService,
            IPasswordHasher<LabSignupUser> passwordHasher)
        {
            _context = context;
            _emailService = emailService;
            _passwordHasher = passwordHasher;
        }

        // Sends OTP
        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] EmailRequestDto dto)
        {
            var user = await _context.LabSignupUsers.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null) return BadRequest("Email not registered.");

            var otp = new Random().Next(100000, 999999).ToString();

            var otpEntry = new LabOtpEntry
            {
                Email = dto.Email,
                OtpCode = otp,
                CreatedAt = DateTime.UtcNow,
                ExpiryTime = DateTime.UtcNow.AddMinutes(5)
            };

            await _context.LabOtpEntries.AddAsync(otpEntry);
            await _context.SaveChangesAsync();

            var body = $@"
                <html>
                  <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
                    <p>Hello,</p>
                    <p>Your OTP for <strong>Hfiles</strong> login is:</p>
                    <h2 style='color: #333;'>{otp}</h2>
                    <p>This OTP is valid for <strong>5 minutes</strong>.</p>
                    <p>If you didn’t request this, you can ignore this email.</p>
                    <br/>
                    <p>Best regards,<br/>The Hfiles Team</p>
                  </body>
                </html>";

            await _emailService.SendEmailAsync(dto.Email, "Your Hfiles Login OTP", body);

            return Ok(new { message = "OTP sent successfully." });
        }



        // Login via Email + OTP
        [HttpPost("login-otp")]
        public async Task<IActionResult> LoginViaOtp([FromBody] OtpLoginDto dto)
        {
            var user = await _context.LabSignupUsers.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null) return BadRequest("Email not registered.");

            var otpEntry = await _context.LabOtpEntries
                .Where(o => o.Email == dto.Email)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpEntry == null || otpEntry.ExpiryTime < DateTime.UtcNow)
                return BadRequest("OTP expired or not found.");

            if (otpEntry.OtpCode != dto.Otp)
                return BadRequest("Invalid OTP.");

            return Ok(new
            {
                message = "Lab login successful, proceed to LabAdmin login.",
                UserId = user.Id,
                Email = user.Email,
                IsSuperAdmin = user.IsSuperAdmin
            });
        }



        // Login via Email + Password
        [HttpPost("login-password")]
        public async Task<IActionResult> LoginViaPassword([FromBody] PasswordLoginDto dto)
        {
            var user = await _context.LabSignupUsers.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null) return BadRequest("Email not registered.");

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed)
                return BadRequest("Incorrect password.");

            return Ok(new
            {
                message = "Lab login successful, proceed to LabAdmin login.",
                UserId = user.Id,
                Email = user.Email,
                IsSuperAdmin = user.IsSuperAdmin
            });
        }
    }
}