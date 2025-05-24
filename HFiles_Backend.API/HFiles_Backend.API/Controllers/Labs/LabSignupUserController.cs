using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using HFiles_Backend.Infrastructure.Data;
using HFiles_Backend.API.Services;
using System.Threading.Tasks;
using System.Linq;
using HFiles_Backend.Application.DTOs.Labs;
using HFiles_Backend.Domain.Entities.Labs;

namespace HFiles_Backend.API.Controllers.Labs
{
    [ApiController]
    [Route("api/[controller]")]
    public class LabSignupUserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<LabSignupUser> _passwordHasher;
        private readonly EmailService _emailService;

        public LabSignupUserController(
            AppDbContext context,
            IPasswordHasher<LabSignupUser> passwordHasher,
            EmailService emailService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
        }

        [HttpPost("labsignup")]
        public async Task<IActionResult> Signup(LabSignupDto dto)
        {
            // Input Validations
            if (string.IsNullOrWhiteSpace(dto.LabName)) return BadRequest("Lab name is required.");
            if (string.IsNullOrWhiteSpace(dto.Email)) return BadRequest("Email is required.");
            if (string.IsNullOrWhiteSpace(dto.PhoneNumber)) return BadRequest("Phone number is required.");
            if (string.IsNullOrWhiteSpace(dto.Pincode)) return BadRequest("Pincode is required.");
            if (string.IsNullOrWhiteSpace(dto.Password)) return BadRequest("Password is required.");
            if (string.IsNullOrWhiteSpace(dto.ConfirmPassword)) return BadRequest("Confirm password is required.");
            if (string.IsNullOrWhiteSpace(dto.Otp)) return BadRequest("OTP is required.");
            if (dto.Password != dto.ConfirmPassword) return BadRequest("Passwords do not match.");
            if (await _context.LabSignupUsers.AnyAsync(u => u.Email == dto.Email))
                return BadRequest("Email already registered.");

            // OTP Validations
            var otpEntry = await _context.LabOtpEntries
                .Where(o => o.Email == dto.Email)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpEntry == null) return BadRequest("OTP not generated for this email.");
            if (otpEntry.ExpiryTime < DateTime.UtcNow) return BadRequest("OTP has expired.");
            if (otpEntry.OtpCode != dto.Otp) return BadRequest("Invalid OTP.");

            // Creates user in database
            var epochTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var last6Epoch = epochTime % 1000000;
            var labPrefix = dto.LabName.Length >= 3 ? dto.LabName.Substring(0, 3).ToUpper() : dto.LabName.ToUpper();
            var randomDigits = new Random().Next(1000, 9999);
            var hfid = $"HF{last6Epoch}{labPrefix}{randomDigits}";

            var user = new LabSignupUser
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

            // After user is added and saved
            _context.LabSignupUsers.Add(user);
            await _context.SaveChangesAsync();

            // Store UserId & Email temporarily (session storage) 
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Email", user.Email);

            // Delete used OTP entry
            _context.LabOtpEntries.Remove(otpEntry);
            await _context.SaveChangesAsync();

            // Send Emails
            var userEmailBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
                  <p>Hello <strong>{dto.LabName}</strong>,</p>

                  <p>Welcome to <strong>Hfiles</strong>!</p>

                  <p>Your registration has been successfully received. You will be contacted for further steps regarding your login. 
                     A representative from Hfiles will reach out to you soon.</p>

                  <p>If you have any questions, feel free to contact us at 
                     <a href='mailto:contact@hfiles.in'>contact@hfiles.in</a>.</p>

                  <p>Best regards,<br/>
                  The Hfiles Team</p>

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

            try
            {
                await _emailService.SendEmailAsync(dto.Email, "Hfiles Registration Received", userEmailBody);
                await _emailService.SendEmailAsync("hfilessocial@gmail.com", "New Lab Signup", adminEmailBody);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"User created, but failed to send emails: {ex.Message}");
            }

            return Ok(new { message = "User registered successfully.", IsSuperAdmin = user.IsSuperAdmin });
        }
    }
}
