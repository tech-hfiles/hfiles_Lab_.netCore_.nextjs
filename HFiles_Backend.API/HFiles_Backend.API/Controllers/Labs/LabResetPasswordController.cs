using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HFiles_Backend.API.Services;
using HFiles_Backend.Application.DTOs.Labs;
using HFiles_Backend.Domain.Entities.Labs;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading.Tasks;
using HFiles_Backend.Infrastructure.Data;
using HFiles_Backend.Application.Common;
using Microsoft.AspNetCore.Authorization;

namespace HFiles_Backend.API.Controllers.Labs
{
    [ApiController]
    [Route("api/")]
    public class LabResetPasswordController(AppDbContext context, EmailService emailService, IPasswordHasher<LabSignupUser> passwordHasher) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly EmailService _emailService = emailService;
        private readonly IPasswordHasher<LabSignupUser> _passwordHasher = passwordHasher;





        // Send email to reset password
        [HttpPost("labs/password-reset/request")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequest dto)
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
                var labUser = await _context.LabSignupUsers
                    .FirstOrDefaultAsync(l => l.Email == dto.Email);

                if (labUser == null)   
                    return NotFound(ApiResponseFactory.Fail("No lab user found with this email."));
                

                if (string.IsNullOrWhiteSpace(labUser.Email))
                    return StatusCode(500, ApiResponseFactory.Fail("Lab user email is missing."));

                string resetLink = "https://hfiles.co.in/forgot-password";
                string emailBody = $@"
                                <html>
                                <body style='font-family:Arial,sans-serif;'>
                                    <p>Hello <strong>{labUser.LabName}</strong>,</p>
                                    <p>You have requested to reset your password for your lab account. Click the button below to proceed:</p>
                                    <p>
                                        <a href='{resetLink}' 
                                           style='background-color:#0331B5;color:white;padding:10px 20px;text-decoration:none;font-weight:bold;'>
                                           Reset Password
                                        </a>
                                    </p>
                                    <p>If you did not request this, please ignore this email.</p>
                                    <br />
                                    <p>Best regards,<br>The Hfiles Team</p>
                                </body>
                                </html>";

                await _emailService.SendEmailAsync(
                    labUser.Email,
                    $"Password Reset Request for {labUser.LabName}",
                    emailBody
                );

                return Ok(ApiResponseFactory.Success(new
                {
                    labUser.Email,
                    labUser.LabName
                }, $"Password reset link sent to your registered email."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseFactory.Fail($"An error occurred while sending the reset link: {ex.Message}"));
            }
        }





        // Reset Password
        [HttpPut("labs/password-reset")]
        public async Task<IActionResult> ResetPassword([FromBody] PasswordReset dto)
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
                var labUser = await _context.LabSignupUsers.FirstOrDefaultAsync(l => l.Email == dto.Email);
                if (labUser == null)
                    return NotFound(ApiResponseFactory.Fail("No lab user found with this email."));

                if (!string.IsNullOrEmpty(labUser.PasswordHash))
                {
                    var verificationResult = _passwordHasher.VerifyHashedPassword(labUser, labUser.PasswordHash, dto.NewPassword);
                    if (verificationResult == PasswordVerificationResult.Success)
                        return BadRequest(ApiResponseFactory.Fail("This password is already in use. Please choose a different one."));
                }

                labUser.PasswordHash = _passwordHasher.HashPassword(labUser, dto.NewPassword);
                await _context.SaveChangesAsync();

                return Ok(ApiResponseFactory.Success(message: "Password successfully reset."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }
    }
}
