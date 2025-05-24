using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using HFiles_Backend.Domain.Entities.Labs;
using HFiles_Backend.Application.DTOs.Labs;
using Microsoft.AspNetCore.Identity;
using HFiles_Backend.API.Services;
using HFiles_Backend.Infrastructure.Data;

namespace HFiles_Backend.Controllers
{
    [ApiController]
    [Route("api/")]
    public class LabAdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<LabAdmin> _passwordHasher;
        private readonly JwtTokenService _jwtTokenService;

        public LabAdminController(AppDbContext context, IPasswordHasher<LabAdmin> passwordHasher, JwtTokenService jwtTokenService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtTokenService = jwtTokenService;
        }

        // Create Lab Admin
        [HttpPost("LabAdmin/create")]
        public async Task<IActionResult> CreateLabAdmin([FromBody] LabAdminDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Retrieve UserId & Email stored from Lab login
            var userId = HttpContext.Session.GetInt32("UserId");
            var email = HttpContext.Session.GetString("Email");

            if (userId == null || string.IsNullOrEmpty(email))
                return BadRequest("UserId and Email not getting fetched from Session!");

            // Fetch Lab details using LabId
            var lab = await _context.LabSignupUsers.FirstOrDefaultAsync(l => l.Id == userId.Value);
            if (lab == null)
                return NotFound($"Lab with ID {userId.Value} not found.");

            // Check if this lab already has a Super Admin
            if (lab.IsSuperAdmin)
                return BadRequest($"A Super Admin already exists for the lab {lab.LabName}.");

            // Check if the lab is a branch (LabReference ≠ 0)
            if (lab.LabReference != 0)
            {
                // Fetch parent lab details
                var parentLab = await _context.LabSignupUsers.FirstOrDefaultAsync(l => l.Id == lab.LabReference);
                if (parentLab != null)
                {
                    return BadRequest($"{lab.LabName} is a branch of {parentLab.LabName} and cannot create a Super Admin.");
                }
            }

            // Fetch UserDetails using HFID
            var userDetails = await _context.Set<UserDetails>().FirstOrDefaultAsync(u => u.user_membernumber == dto.HFID);
            if (userDetails == null)
                return NotFound($"No user found with HFID {dto.HFID}.");

            var newAdmin = new LabAdmin
            {
                UserId = userDetails.user_id,
                LabId = userId.Value,
                PasswordHash = _passwordHasher.HashPassword(null, dto.Password),
                EpochTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                IsMain = 1
            };

            _context.LabAdmins.Add(newAdmin);

            // Update IsSuperAdmin to true after adding the admin
            lab.IsSuperAdmin = true;
            _context.LabSignupUsers.Update(lab);

            await _context.SaveChangesAsync();

            // Generate Token After Successful Registration
            var token = _jwtTokenService.GenerateToken(userId.Value, email, newAdmin.Id, dto.Role);

            return Ok(new
            {
                Message = "Lab admin created successfully, and lab IsSuperAdmin updated.",
                UserId = newAdmin.UserId,
                LabId = userId.Value,
                LabName = lab.LabName,
                LabEmail = email,
                LabAdminId = newAdmin.Id,
                Role = dto.Role,
                Token = token,
                IsSuperAdmin = lab.IsSuperAdmin 
            });
        }





        // Login Lab Admin and Lab Members
        [HttpPost("LabUser/login")]
        public async Task<IActionResult> LabAdminLogin([FromBody] LabAdminLoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Retrieve UserId & Email stored from Lab login
            var userId = HttpContext.Session.GetInt32("UserId");
            var email = HttpContext.Session.GetString("Email");

            if (userId == null || string.IsNullOrEmpty(email))
                return Unauthorized("Lab login details missing. Please login as Lab first.");

            // Fetch LabAdmin using HFID instead of session-stored UserId
            var userDetails = await _context.Set<UserDetails>()
                .FirstOrDefaultAsync(u => u.user_membernumber == dto.HFID);

            if (userDetails == null)
                return NotFound($"No Super Admin/Admin/Member found with HFID {dto.HFID}.");

            // Validate Super Admin Login
            if (dto.Role == "Super Admin")
            {
                var admin = await _context.LabAdmins.FirstOrDefaultAsync(a => a.UserId == userDetails.user_id);
                if (admin == null)
                    return Unauthorized($"The user with HFID: {dto.HFID} is not a Super Admin.");

                if (!_passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, dto.Password)
                    .Equals(PasswordVerificationResult.Success))
                    return Unauthorized("Invalid password.");

                // Generate Token After Successful Login
                var token = _jwtTokenService.GenerateToken(userId.Value, email, admin.Id, dto.Role);
                return Ok(new { Message = "Login successful.", Token = token });
            }

            // Validate Admin/Member Login
            else if (dto.Role == "Admin" || dto.Role == "Member")
            {
                var member = await _context.LabMembers.FirstOrDefaultAsync(m => m.UserId == userDetails.user_id);
                if (member == null)
                    return Unauthorized($"{dto.Role} not found. Please register first.");

                if (!_passwordHasher.VerifyHashedPassword(null, member.PasswordHash, dto.Password)
                    .Equals(PasswordVerificationResult.Success))
                    return Unauthorized("Invalid password.");


                // Generate Token After Successful Login
                var token = _jwtTokenService.GenerateToken(member.UserId, email, member.Id, dto.Role);
                return Ok(new { Message = "Login successful.", Token = token });
            }

            return BadRequest("Invalid role specified.");
        }





        [HttpGet("LabUsers")]
        public async Task<IActionResult> GetAllLabUsers()
        {
            // Extract LabId from JWT token
            var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
                return Unauthorized("Invalid or missing LabId in token.");

            // Fetch all Admins for the Lab
            var admins = await _context.LabAdmins
                .Where(a => a.LabId == labId)
                .Select(a => new
                {
                    AdminId = a.Id,
                    UserId = a.UserId,
                    LabId = a.LabId,
                    Role = "Super Admin",
                    CreatedAt = DateTimeOffset.FromUnixTimeSeconds(a.EpochTime).UtcDateTime.ToString("dd-MM-yyyy")
                })
                .ToListAsync();

            // Fetch all Members for the Lab
            var members = await _context.LabMembers
                .Where(m => m.LabId == labId)
                .Select(m => new
                {
                    MemberId = m.Id,
                    UserId = m.UserId,
                    LabId = m.LabId,
                    Role = "Member",
                    CreatedBy = m.CreatedBy,
                    DeletedBy = m.DeletedBy,
                    CreatedAt = DateTimeOffset.FromUnixTimeSeconds(m.EpochTime).UtcDateTime.ToString("dd-MM-yyyy")
                })
                .ToListAsync();

            if (!admins.Any() && !members.Any())
                return NotFound($"No admins or members found for LabId {labId}.");

            return Ok(new
            {
                Message = "Lab users fetched successfully.",
                LabId = labId,
                Admins = admins,
                Members = members
            });
        }


    }
}
