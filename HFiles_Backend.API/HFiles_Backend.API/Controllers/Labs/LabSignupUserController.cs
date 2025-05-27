using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using HFiles_Backend.Infrastructure.Data;
using HFiles_Backend.API.Services;
using System.Threading.Tasks;
using System.Linq;
using HFiles_Backend.Application.DTOs.Labs;
using HFiles_Backend.Domain.Entities.Labs;
using Microsoft.AspNetCore.Authorization;

namespace HFiles_Backend.API.Controllers.Labs
{
    [ApiController]
    [Route("api/")]
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





        // Creates Lab 
        [HttpPost("labs")]
        public async Task<IActionResult> Signup([FromBody] SignupDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (await _context.LabSignupUsers.AnyAsync(u => u.Email == dto.Email))
                return BadRequest("Email already registered.");

            var otpEntry = await _context.LabOtpEntries
                .Where(o => o.Email == dto.Email)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpEntry == null)
                return BadRequest("OTP not generated for this email.");

            if (otpEntry.ExpiryTime < DateTime.UtcNow)
                return BadRequest("OTP has expired.");

            if (otpEntry.OtpCode != dto.Otp)
                return BadRequest("Invalid OTP.");

            const string HFilesEmail = "hfilessocial@gmail.com";
            const string HFIDPrefix = "HF";
            var epochTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var last6Epoch = epochTime % 1000000;
            var labPrefix = dto.LabName.Length >= 3 ? dto.LabName.Substring(0, 3).ToUpper() : dto.LabName.ToUpper();
            var randomDigits = new Random().Next(1000, 9999);
            var hfid = $"{HFIDPrefix}{last6Epoch}{labPrefix}{randomDigits}";

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

            _context.LabSignupUsers.Add(user);
            await _context.SaveChangesAsync();

            _context.LabOtpEntries.Remove(otpEntry);
            await _context.SaveChangesAsync();

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
                await _emailService.SendEmailAsync(HFilesEmail, "New Lab Signup", adminEmailBody);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"User created, but failed to send emails: {ex.Message}");
            }

            return Ok(new { message = "User registered successfully.", IsSuperAdmin = user.IsSuperAdmin });
        }





        // Update Address and Profile Photo for Lab
        [HttpPatch("labs/update")]
        [Authorize]
        public async Task<IActionResult> UpdateLabUserProfile([FromForm] ProfileUpdateDto dto, IFormFile? ProfilePhoto)
        {
            var user = await _context.LabSignupUsers.FirstOrDefaultAsync(l => l.Id == dto.Id);
            if (user == null)
                return NotFound($"Lab user with ID {dto.Id} not found.");

            if (!string.IsNullOrEmpty(dto.Address))
                user.Address = dto.Address;

            if (ProfilePhoto != null && ProfilePhoto.Length > 0)
            {
                string uploadsFolder = Path.Combine("wwwroot", "uploads");
                Directory.CreateDirectory(uploadsFolder);

                DateTime createdAt = DateTimeOffset.FromUnixTimeSeconds(user.CreatedAtEpoch).UtcDateTime;
                string formattedTime = createdAt.ToString("dd-MM-yyyy-HH-mm-ss");

                string fileName = $"{Path.GetFileNameWithoutExtension(ProfilePhoto.FileName)}_{formattedTime}{Path.GetExtension(ProfilePhoto.FileName)}";
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ProfilePhoto.CopyToAsync(stream);
                }

                user.ProfilePhoto = fileName; 
            }


            _context.LabSignupUsers.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Profile updated successfully.",
                LabId = user.Id,
                UpdatedAddress = user.Address,
                UpdatedProfilePhoto = user.ProfilePhoto
            });
        }
    }
}
