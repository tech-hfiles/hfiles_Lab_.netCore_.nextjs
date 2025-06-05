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
    public class LabResetPasswordController(AppDbContext context, EmailService emailService, IPasswordHasher<LabSignupUser> passwordHasher, IPasswordHasher<LabAdmin> passwordHasher1, IPasswordHasher<LabMember> passwordHasher2) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly EmailService _emailService = emailService;
        private readonly IPasswordHasher<LabSignupUser> _passwordHasher = passwordHasher;
        private readonly IPasswordHasher<LabAdmin> _passwordHasher1 = passwordHasher1;
        private readonly IPasswordHasher<LabMember> _passwordHasher2 = passwordHasher2;




        // Send email to reset password for Labs
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

                var mainLab = labUser.LabReference == 0
                    ? labUser
                    : await _context.LabSignupUsers.FirstOrDefaultAsync(l => l.Id == labUser.LabReference);

                if (mainLab == null || string.IsNullOrWhiteSpace(mainLab.Email))
                    return StatusCode(500, ApiResponseFactory.Fail("Main lab email is missing."));

                string recipientEmail = mainLab.Email;
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
                    recipientEmail,
                    $"Password Reset Request for {labUser.LabName}",
                    emailBody
                );

                return Ok(ApiResponseFactory.Success(new
                {
                    RecipientEmail = recipientEmail,
                    labUser.LabName
                }, $"Password reset link sent to {recipientEmail}."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseFactory.Fail($"An error occurred while sending the reset link: {ex.Message}"));
            }
        }







        // Reset Password for Labs
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





        // Send email to reset password for Lab Users
        [HttpPost("labs/users/password-reset/request")]
        public async Task<IActionResult> UsersRequestPasswordReset([FromBody] UserPasswordResetRequest dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponseFactory.Fail(errors));
            }

            try
            {
                var userDetails = await _context.UserDetails
                    .FirstOrDefaultAsync(u => u.user_email == dto.Email);

                if (userDetails == null)
                    return NotFound(ApiResponseFactory.Fail("No user found with this email."));

                if (userDetails.user_email == null)
                    return NotFound(ApiResponseFactory.Fail("Email not registered by this user."));

                int userId = userDetails.user_id;
                string? recipientEmail = null;
                string? userRole = null;

                var superAdmin = await _context.LabAdmins
                    .FirstOrDefaultAsync(a => a.UserId == userId && a.IsMain == 1 && a.LabId == dto.LabId);

                if (superAdmin != null)
                {
                    recipientEmail = userDetails.user_email;
                    userRole = "Super Admin";
                }
                else
                {
                    var labMember = await _context.LabMembers
                        .FirstOrDefaultAsync(m => m.UserId == userId && m.DeletedBy == 0 && m.LabId == dto.LabId);

                    if (labMember != null)
                    {
                        recipientEmail = userDetails.user_email;
                        userRole = labMember.Role; 
                    }
                }

                if (recipientEmail == null)
                    return NotFound(ApiResponseFactory.Fail("No matching user found for password reset."));

                var lab = await _context.LabSignupUsers
                    .FirstOrDefaultAsync(lsu => lsu.Id == superAdmin!.LabId);

                string resetLink = "https://hfiles.co.in/forgot-password";
                string emailBody = $@"
                                    <html>
                                    <body style='font-family:Arial,sans-serif;'>
                                        <p>Hello <strong>{userDetails.user_firstname}</strong>,</p>
                                        <p>You have requested to reset your password for {lab!.LabName}. Click the button below to proceed:</p>
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

                await _emailService.SendEmailAsync(recipientEmail, $"Password Reset Request for {userDetails.user_firstname} {userDetails.user_lastname}", emailBody);

                return Ok(ApiResponseFactory.Success(new
                {
                    RecipientEmail = recipientEmail,
                    UserRole = userRole,
                    lab.LabName
                }, $"Password reset link sent to {recipientEmail} ({userRole})."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseFactory.Fail($"An error occurred while sending the reset link: {ex.Message}"));
            }
        }





        // Reset Password for Lab Users
        [HttpPut("labs/users/password-reset")]
        public async Task<IActionResult> UsersResetPassword([FromBody] UserPasswordReset dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponseFactory.Fail(errors));
            }

            try
            {
                var userDetails = await _context.UserDetails
                    .FirstOrDefaultAsync(u => u.user_email == dto.Email);

                if (userDetails == null)
                    return NotFound(ApiResponseFactory.Fail("No user found with this email."));

                if (string.IsNullOrWhiteSpace(userDetails.user_email))
                    return NotFound(ApiResponseFactory.Fail("Email not registered by this user."));

                int userId = userDetails.user_id;
                string? userRole = null;
                string? existingPasswordHash = null;

                var superAdmin = await _context.LabAdmins
                    .FirstOrDefaultAsync(a => a.UserId == userId && a.IsMain == 1 && a.LabId == dto.LabId);

                if (superAdmin != null)
                {
                    userRole = "Super Admin";
                    existingPasswordHash = superAdmin.PasswordHash;

                    if(existingPasswordHash == null)
                        return BadRequest(ApiResponseFactory.Fail("Password not registered by this user."));

                    if (_passwordHasher1.VerifyHashedPassword(superAdmin, existingPasswordHash, dto.NewPassword) == PasswordVerificationResult.Success)
                        return BadRequest(ApiResponseFactory.Fail("This password is already in use. Please choose a different one."));

                    superAdmin.PasswordHash = _passwordHasher1.HashPassword(superAdmin, dto.NewPassword);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    var labMember = await _context.LabMembers
                        .FirstOrDefaultAsync(m => m.UserId == userId && m.DeletedBy == 0 && m.LabId == dto.LabId);

                    if (labMember != null)
                    {
                        userRole = labMember.Role ?? "Member";
                        existingPasswordHash = labMember.PasswordHash;

                        if (_passwordHasher2.VerifyHashedPassword(labMember, existingPasswordHash!, dto.NewPassword) == PasswordVerificationResult.Success)
                            return BadRequest(ApiResponseFactory.Fail("This password is already in use. Please choose a different one."));

                        labMember.PasswordHash = _passwordHasher2.HashPassword(labMember, dto.NewPassword);
                        await _context.SaveChangesAsync();
                    }
                }

                if (existingPasswordHash == null)
                    return NotFound(ApiResponseFactory.Fail("No matching user found for password reset."));

                return Ok(ApiResponseFactory.Success(new
                {
                    dto.Email,
                    UserRole = userRole
                }, "Password successfully updated."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }
    }
}
