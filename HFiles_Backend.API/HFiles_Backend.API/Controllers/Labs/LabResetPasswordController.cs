using System.Security.Cryptography;
using HFiles_Backend.API.Services;
using HFiles_Backend.Application.Common;
using HFiles_Backend.Application.DTOs.Labs;
using HFiles_Backend.Domain.Entities.Labs;
using HFiles_Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HFiles_Backend.API.Controllers.Labs
{
    [ApiController]
    [Route("api/")]
    public class LabResetPasswordController(AppDbContext context, EmailService emailService, IPasswordHasher<LabSignup> passwordHasher, IPasswordHasher<LabSuperAdmin> passwordHasher1, IPasswordHasher<LabMember> passwordHasher2, ILogger<LabResetPasswordController> logger) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly EmailService _emailService = emailService;
        private readonly IPasswordHasher<LabSignup> _passwordHasher = passwordHasher;
        private readonly IPasswordHasher<LabSuperAdmin> _passwordHasher1 = passwordHasher1;
        private readonly IPasswordHasher<LabMember> _passwordHasher2 = passwordHasher2;
        private readonly ILogger<LabResetPasswordController> _logger = logger;
        private const int OtpValidityMinutes = 5;





        // Sends Email to Main Lab for password reset
        [HttpPost("labs/password-reset/request")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequest dto)
        {
            HttpContext.Items["Log-Category"] = "Authentication";

            _logger.LogInformation("Received password reset request for Email: {Email}", dto.Email);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("Validation failed: {@Errors}", errors);
                return BadRequest(ApiResponseFactory.Fail(errors));
            }

            try
            {
                var labUser = await _context.LabSignups.FirstOrDefaultAsync(l => l.Email == dto.Email);

                if (labUser == null)
                {
                    _logger.LogWarning("Password reset failed: No lab user found with Email {Email}.", dto.Email);
                    return NotFound(ApiResponseFactory.Fail("No lab user found with this email."));
                }

                if (string.IsNullOrWhiteSpace(labUser.Email))
                {
                    _logger.LogWarning("Password reset failed: Lab user Email is missing for Email {Email}.", dto.Email);
                    return StatusCode(500, ApiResponseFactory.Fail("Lab user email is missing."));
                }

                var mainLab = labUser.LabReference == 0
                    ? labUser
                    : await _context.LabSignups.FirstOrDefaultAsync(l => l.Id == labUser.LabReference);

                if (mainLab == null || string.IsNullOrWhiteSpace(mainLab.Email))
                {
                    _logger.LogWarning("Password reset failed: Main lab Email is missing for Lab ID {LabId}.", labUser.LabReference);
                    return StatusCode(500, ApiResponseFactory.Fail("Main lab email is missing."));
                }

                var labResetPasswordOtp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
                var otpEntry = new LabOtpEntry
                {
                    Email = dto.Email,
                    OtpCode = labResetPasswordOtp,
                    CreatedAt = DateTime.UtcNow,
                    ExpiryTime = DateTime.UtcNow.AddMinutes(OtpValidityMinutes)
                };

                await _context.LabOtpEntries.AddAsync(otpEntry);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Password reset OTP generated for Email {Email}. OTP: {Otp}, Expiry Time: {ExpiryTime}", dto.Email, labResetPasswordOtp, otpEntry.ExpiryTime);

                string recipientEmail = mainLab.Email;
                string resetLink = "https://hfiles.co.in/forgot-password";

                string emailBody = $@"
                                 <html>
                                 <body style='font-family:Arial,sans-serif;'>
                                     <p>Hello <strong>{labUser.LabName}</strong>,</p>
                                     <p>Your OTP for Lab Reset Password is:</p>
                                     <h2 style='color: #333;'>{labResetPasswordOtp}</h2>
                                     <p>This OTP is valid for <strong>{OtpValidityMinutes} minutes</strong>.</p>
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

                await _emailService.SendEmailAsync(recipientEmail, $"Password Reset Request for {labUser.LabName}", emailBody);

                _logger.LogInformation("Password reset link sent successfully to Email {Email}.", recipientEmail);

                return Ok(ApiResponseFactory.Success(new
                {
                    RecipientEmail = recipientEmail,
                    labUser.LabName
                }, $"Password reset link sent to {recipientEmail}."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password reset request failed due to an unexpected error for Email {Email}", dto.Email);
                return StatusCode(500, ApiResponseFactory.Fail($"An error occurred while sending the reset link: {ex.Message}"));
            }
        }





        // Reset Password for Labs
        [HttpPut("labs/password-reset")]
        public async Task<IActionResult> ResetPassword([FromBody] PasswordReset dto, [FromServices] OtpVerificationStore otpStore)
        {
            HttpContext.Items["Log-Category"] = "Authentication";

            _logger.LogInformation("Received password reset request for Email: {Email}", dto.Email);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("Validation failed: {@Errors}", errors);
                return BadRequest(ApiResponseFactory.Fail(errors));
            }

            if (!otpStore.Consume(dto.Email, "password_reset"))
            {
                _logger.LogWarning("Lab Reset Password failed: OTP not verified or already used for Email {Email}", dto.Email);
                return Unauthorized(ApiResponseFactory.Fail("OTP not verified or already used. Please verify again."));
            }

            try
            {
                var labUser = await _context.LabSignups.FirstOrDefaultAsync(l => l.Email == dto.Email);

                if (labUser == null)
                {
                    _logger.LogWarning("Password reset failed: No lab user found with Email {Email}.", dto.Email);
                    return NotFound(ApiResponseFactory.Fail("No lab user found with this email."));
                }

                if (!string.IsNullOrEmpty(labUser.PasswordHash))
                {
                    var verificationResult = _passwordHasher.VerifyHashedPassword(labUser, labUser.PasswordHash, dto.NewPassword);
                    if (verificationResult == PasswordVerificationResult.Success)
                    {
                        _logger.LogWarning("Password reset failed: New password matches the existing password for Email {Email}.", dto.Email);
                        return BadRequest(ApiResponseFactory.Fail("This password is already in use. Please choose a different one."));
                    }
                }

                labUser.PasswordHash = _passwordHasher.HashPassword(labUser, dto.NewPassword);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Password reset successful for Email {Email}.", dto.Email);

                return Ok(ApiResponseFactory.Success(message: "Password successfully reset."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password reset request failed due to an unexpected error for Email {Email}", dto.Email);
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }





        // Sends Email to Lab Users for password reset
        [HttpPost("labs/users/password-reset/request")]
        public async Task<IActionResult> UsersRequestPasswordReset([FromBody] UserPasswordResetRequest dto)
        {
            HttpContext.Items["Log-Category"] = "Authentication";

            _logger.LogInformation("Received password reset request for User Email: {Email}, Lab ID: {LabId}", dto.Email, dto.LabId);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("Validation failed: {@Errors}", errors);
                return BadRequest(ApiResponseFactory.Fail(errors));
            }

            try
            {
                var userDetails = await _context.UserDetails
                    .FirstOrDefaultAsync(u => u.user_email == dto.Email);

                if (userDetails == null)
                {
                    _logger.LogWarning("Password reset failed: No user found with Email {Email}.", dto.Email);
                    return NotFound(ApiResponseFactory.Fail("No user found with this email."));
                }

                if (string.IsNullOrWhiteSpace(userDetails.user_email))
                {
                    _logger.LogWarning("Password reset failed: Email not registered by user {UserId}.", userDetails.user_id);
                    return NotFound(ApiResponseFactory.Fail("Email not registered by this user."));
                }

                int userId = userDetails.user_id;
                string? recipientEmail = null;
                string? userRole = null;

                var superAdmin = await _context.LabSuperAdmins
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
                {
                    _logger.LogWarning("Password reset failed: No matching user found for Lab ID {LabId}.", dto.LabId);
                    return NotFound(ApiResponseFactory.Fail("No matching user found for password reset."));
                }

                var labIdToUse = superAdmin != null ? superAdmin.LabId : dto.LabId;

                var lab = await _context.LabSignups.FirstOrDefaultAsync(lsu => lsu.Id == labIdToUse);

                if (lab == null)
                {
                    _logger.LogWarning("Password reset failed: Lab not found for Lab ID {LabId}.", labIdToUse);
                    return NotFound(ApiResponseFactory.Fail("Lab not found."));
                }


                var userResetPasswordOtp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

                var otpEntry = new LabOtpEntry
                {
                    Email = dto.Email,
                    OtpCode = userResetPasswordOtp,
                    CreatedAt = DateTime.UtcNow,
                    ExpiryTime = DateTime.UtcNow.AddMinutes(OtpValidityMinutes)
                };

                await _context.LabOtpEntries.AddAsync(otpEntry);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Password reset OTP generated for Email {Email}. OTP: {Otp}, Expiry Time: {ExpiryTime}", dto.Email, userResetPasswordOtp, otpEntry.ExpiryTime);

                string resetLink = "https://hfiles.co.in/forgot-password";
                string emailBody = $@"
                            <html>
                            <body style='font-family:Arial,sans-serif;'>
                                <p>Hello <strong>{userDetails.user_firstname}</strong>,</p>
                                <p>Your OTP for Reset Password is:</p>
                                <h2 style='color: #333;'>{userResetPasswordOtp}</h2>
                                <p>This OTP is valid for <strong>{OtpValidityMinutes} minutes</strong>.</p>
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

                _logger.LogInformation("Password reset link sent successfully to Email {Email}.", recipientEmail);

                return Ok(ApiResponseFactory.Success(new
                {
                    RecipientEmail = recipientEmail,
                    UserRole = userRole,
                    lab.LabName
                }, $"Password reset link sent to {recipientEmail} ({userRole})."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password reset request failed due to an unexpected error for Email {Email}", dto.Email);
                return StatusCode(500, ApiResponseFactory.Fail($"An error occurred while sending the reset link: {ex.Message}"));
            }
        }





        // Reset Password for Lab Users
        [HttpPut("labs/users/password-reset")]
        public async Task<IActionResult> UsersResetPassword([FromBody] UserPasswordReset dto, [FromServices] OtpVerificationStore otpStore)
        {
            HttpContext.Items["Log-Category"] = "Authentication";

            _logger.LogInformation("Received password reset request for User Email: {Email}, Lab ID: {LabId}", dto.Email, dto.LabId);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("Validation failed: {@Errors}", errors);
                return BadRequest(ApiResponseFactory.Fail(errors));
            }

            if (!otpStore.Consume(dto.Email, "password_reset"))
            {
                _logger.LogWarning("User Reset Password failed: OTP not verified or already used for Email {Email}", dto.Email);
                return Unauthorized(ApiResponseFactory.Fail("OTP not verified or already used. Please verify again."));
            }

            try
            {
                var userDetails = await _context.UserDetails
                    .FirstOrDefaultAsync(u => u.user_email == dto.Email);

                if (userDetails == null)
                {
                    _logger.LogWarning("Password reset failed: No user found with Email {Email}.", dto.Email);
                    return NotFound(ApiResponseFactory.Fail("No user found with this email."));
                }

                if (string.IsNullOrWhiteSpace(userDetails.user_email))
                {
                    _logger.LogWarning("Password reset failed: Email not registered by user {UserId}.", userDetails.user_id);
                    return NotFound(ApiResponseFactory.Fail("Email not registered by this user."));
                }

                int userId = userDetails.user_id;
                string? userRole = null;
                string? existingPasswordHash = null;

                var superAdmin = await _context.LabSuperAdmins
                    .FirstOrDefaultAsync(a => a.UserId == userId && a.IsMain == 1 && a.LabId == dto.LabId);

                if (superAdmin != null)
                {
                    userRole = "Super Admin";
                    existingPasswordHash = superAdmin.PasswordHash;

                    if (existingPasswordHash == null)
                    {
                        _logger.LogWarning("Password reset failed: Password not registered for Super Admin {Email}.", dto.Email);
                        return BadRequest(ApiResponseFactory.Fail("Password not registered by this user."));
                    }

                    if (_passwordHasher1.VerifyHashedPassword(superAdmin, existingPasswordHash, dto.NewPassword) == PasswordVerificationResult.Success)
                    {
                        _logger.LogWarning("Password reset failed: New password matches existing password for Super Admin {Email}.", dto.Email);
                        return BadRequest(ApiResponseFactory.Fail("This password is already in use. Please choose a different one."));
                    }

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
                        {
                            _logger.LogWarning("Password reset failed: New password matches existing password for Member {Email}.", dto.Email);
                            return BadRequest(ApiResponseFactory.Fail("This password is already in use. Please choose a different one."));
                        }

                        labMember.PasswordHash = _passwordHasher2.HashPassword(labMember, dto.NewPassword);
                        await _context.SaveChangesAsync();
                    }
                }

                if (existingPasswordHash == null)
                {
                    _logger.LogWarning("Password reset failed: No matching user found for Email {Email}.", dto.Email);
                    return NotFound(ApiResponseFactory.Fail("No matching user found for password reset."));
                }

                _logger.LogInformation("Password reset successful for Email {Email}, Role {Role}.", dto.Email, userRole);

                return Ok(ApiResponseFactory.Success(new
                {
                    dto.Email,
                    UserRole = userRole
                }, "Password successfully updated."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password reset request failed due to an unexpected error for Email {Email}", dto.Email);
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }





        // Verify OTP for password reset
        [HttpPost("labs/password-reset/verify/otp")]
        public async Task<IActionResult> BranchVerifyOTP([FromBody] OtpLogin dto, [FromServices] OtpVerificationStore otpStore)
        {
            HttpContext.Items["Log-Category"] = "Authentication";

            _logger.LogInformation("Received OTP verification request for Email: {Email}", dto.Email);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("Validation failed: {@Errors}", errors);
                return BadRequest(ApiResponseFactory.Fail(errors));
            }

            try
            {
                var now = DateTime.UtcNow;

                var otpEntry = await _context.LabOtpEntries
                    .Where(o => o.Email == dto.Email)
                    .OrderByDescending(o => o.CreatedAt)
                    .FirstOrDefaultAsync();

                if (otpEntry == null)
                {
                    _logger.LogWarning("OTP verification failed: No OTP entry found for Email {Email}", dto.Email);
                    return BadRequest(ApiResponseFactory.Fail("OTP expired or not found."));
                }

                if (otpEntry.ExpiryTime < DateTime.UtcNow)
                {
                    _logger.LogWarning("OTP verification failed: OTP expired for Email {Email}", dto.Email);
                    return BadRequest(ApiResponseFactory.Fail("OTP expired."));
                }

                if (otpEntry.OtpCode != dto.Otp)
                {
                    _logger.LogWarning("OTP verification failed: Invalid OTP provided for Email {Email}", dto.Email);
                    return BadRequest(ApiResponseFactory.Fail("Invalid OTP."));
                }

                var expiredOtps = await _context.LabOtpEntries
                 .Where(x => x.Email == dto.Email && x.ExpiryTime < now)
                 .ToListAsync();
                _context.LabOtpEntries.RemoveRange(expiredOtps);
                _context.LabOtpEntries.Remove(otpEntry);
                await _context.SaveChangesAsync();

                otpStore.StoreVerifiedOtp(dto.Email, "  ");

                _logger.LogInformation("OTP verification successful for Email {Email}", dto.Email);
                return Ok(ApiResponseFactory.Success("OTP successfully verified."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OTP verification failed due to an unexpected error for Email {Email}", dto.Email);
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }
    }
}
