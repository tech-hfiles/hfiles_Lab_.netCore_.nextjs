using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HFiles_Backend.API.Services;
using HFiles_Backend.Application.DTOs.Labs;
using HFiles_Backend.Domain.Entities.Labs;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading.Tasks;
using HFiles_Backend.Infrastructure.Data;

namespace HFiles_Backend.API.Controllers.Labs
{
    [ApiController]
    [Route("api/[controller]")]
    public class LabResetPasswordController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;
        private readonly IPasswordHasher<LabSignupUser> _passwordHasher;

        public LabResetPasswordController(AppDbContext context, EmailService emailService, IPasswordHasher<LabSignupUser> passwordHasher)
        {
            _context = context;
            _emailService = emailService;
            _passwordHasher = passwordHasher;
        }

        // Request Password Reset (Send Reset Link)
        [HttpPost("request")]
        public async Task<IActionResult> RequestPasswordReset([FromQuery] string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest("Email is required.");

            var labUser = await _context.LabSignupUsers.FirstOrDefaultAsync(l => l.Email == email);
            if (labUser == null)
                return NotFound("No lab user found with this email.");

            string resetLink = $"https://hfiles.co.in/forgot-password";
            string emailBody = $@"
                <html>
                <body style='font-family:Arial,sans-serif;'>
                    <p>Hello <strong>{labUser.LabName}</strong>,</p>
                    <p>You have requested to reset the password for your lab account. Click the button below to proceed:</p>
                    <a href='{resetLink}' style='background-color:#0331B5;color:white;padding:10px 20px;text-decoration:none;font-weight:bold;'>Reset Password</a>
                    <p>If you did not request this, please ignore this email.</p>
                </body>
                </html>";

            await _emailService.SendEmailAsync(labUser.Email, $"Password Reset Request for {labUser.LabName}", emailBody);

            return Ok($"Password reset link sent to your registered email for {labUser.LabName}.");
        }



        // Reset Password (Save New Password)
        [HttpPost("update")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.NewPassword) || string.IsNullOrEmpty(dto.ConfirmPassword))
                return BadRequest("All fields are required.");

            if (dto.NewPassword != dto.ConfirmPassword)
                return BadRequest("New Password and Confirm Password must match.");

            var labUser = await _context.LabSignupUsers.FirstOrDefaultAsync(l => l.Email == dto.Email);
            if (labUser == null)
                return NotFound("No lab user found with this email.");

            var passwordVerification = _passwordHasher.VerifyHashedPassword(null, labUser.PasswordHash, dto.NewPassword);

            if (passwordVerification == PasswordVerificationResult.Success)
                return BadRequest("This password is already registered. Please proceed to Lab Login, or if you wish to change your password, enter a different one.");

            labUser.PasswordHash = _passwordHasher.HashPassword(null, dto.NewPassword);
            await _context.SaveChangesAsync();

            return Ok("Password successfully reset.");
        }

    }
}
