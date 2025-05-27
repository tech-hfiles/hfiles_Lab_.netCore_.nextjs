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
    [Route("api/")]
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





        // Send email to reset password
        [HttpPost("labs/password-reset/request")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var labUser = await _context.LabSignupUsers.FirstOrDefaultAsync(l => l.Email == dto.Email);
            if (labUser == null)
                return NotFound(new { message = "No lab user found with this email." });

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

            try
            {
                await _emailService.SendEmailAsync(labUser.Email, $"Password Reset Request for {labUser.LabName}", emailBody);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error sending email. Please try again later." });
            }

            return Ok(new { message = $"Password reset link sent to your registered email for {labUser.LabName}." });
        }







        // Reset Password
        [HttpPut("labs/password-reset")]
        public async Task<IActionResult> ResetPassword([FromBody] PasswordResetDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var labUser = await _context.LabSignupUsers.FirstOrDefaultAsync(l => l.Email == dto.Email);
            if (labUser == null)
                return NotFound("No lab user found with this email.");

            var verificationResult = _passwordHasher.VerifyHashedPassword(null, labUser.PasswordHash, dto.NewPassword);
            if (verificationResult == PasswordVerificationResult.Success)
                return BadRequest("This password is already in use. Please choose a different one.");

            labUser.PasswordHash = _passwordHasher.HashPassword(null, dto.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password successfully reset." });
        }
    }
}
