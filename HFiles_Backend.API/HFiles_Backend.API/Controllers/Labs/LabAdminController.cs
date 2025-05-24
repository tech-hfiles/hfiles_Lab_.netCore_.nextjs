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

            var userId = dto.UserId;
            var email = dto.Email;

            if (userId == 0 || string.IsNullOrEmpty(email))
                return BadRequest("UserId and Email are required in the payload.");

            var lab = await _context.LabSignupUsers.FirstOrDefaultAsync(l => l.Id == userId);
            if (lab == null)
                return NotFound($"Lab with ID {userId} not found.");

            if (lab.IsSuperAdmin)
                return BadRequest($"A Super Admin already exists for the lab {lab.LabName}.");

            if (lab.LabReference != 0)
            {
                var parentLab = await _context.LabSignupUsers.FirstOrDefaultAsync(l => l.Id == lab.LabReference);
                if (parentLab != null)
                    return BadRequest($"{lab.LabName} is a branch of {parentLab.LabName} and cannot create a Super Admin.");
            }

            var userDetails = await _context.Set<UserDetails>().FirstOrDefaultAsync(u => u.user_membernumber == dto.HFID);
            if (userDetails == null)
                return NotFound($"No user found with HFID {dto.HFID}.");

            var newAdmin = new LabAdmin
            {
                UserId = userDetails.user_id,
                LabId = userId,
                PasswordHash = _passwordHasher.HashPassword(null, dto.Password),
                EpochTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                IsMain = 1
            };

            _context.LabAdmins.Add(newAdmin);
            lab.IsSuperAdmin = true;
            _context.LabSignupUsers.Update(lab);

            await _context.SaveChangesAsync();

            var token = _jwtTokenService.GenerateToken(userId, email, newAdmin.Id, dto.Role);

            return Ok(new
            {
                Message = "Lab admin created successfully, and lab IsSuperAdmin updated.",
                Username = $"{userDetails.user_firstname} {userDetails.user_lastname}",
                Token = token            
            });
        }





        // Login Lab Admin and Lab Members
        [HttpPost("LabUser/login")]
        public async Task<IActionResult> LabAdminLogin([FromBody] LabAdminLoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.UserId == null || string.IsNullOrEmpty(dto.Email))
                return Unauthorized("User ID and Email must be provided.");

            var userDetails = await _context.Set<UserDetails>()
                .FirstOrDefaultAsync(u => u.user_membernumber == dto.HFID);

            if (userDetails == null)
                return NotFound($"No Super Admin/Admin/Member found with HFID {dto.HFID}.");

            string username = $"{userDetails.user_firstname} {userDetails.user_lastname}"; 

            if (dto.Role == "Super Admin")
            {
                var admin = await _context.LabAdmins.FirstOrDefaultAsync(a => a.UserId == userDetails.user_id);
                if (admin == null)
                    return Unauthorized($"The user with HFID: {dto.HFID} is not a Super Admin.");

                if (!_passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, dto.Password)
                    .Equals(PasswordVerificationResult.Success))
                    return Unauthorized("Invalid password.");

                var token = _jwtTokenService.GenerateToken(dto.UserId.Value, dto.Email, admin.Id, dto.Role);
                return Ok(new
                {
                    Message = "Login successful.",
                    Username = username,
                    Token = token
                    
                });
            }
            else if (dto.Role == "Admin" || dto.Role == "Member")
            {
                var member = await _context.LabMembers.FirstOrDefaultAsync(m => m.UserId == userDetails.user_id);
                if (member == null)
                    return Unauthorized($"{dto.Role} not found. Please register first.");

                if (!_passwordHasher.VerifyHashedPassword(null, member.PasswordHash, dto.Password)
                    .Equals(PasswordVerificationResult.Success))
                    return Unauthorized("Invalid password.");

                var token = _jwtTokenService.GenerateToken(member.UserId, dto.Email, member.Id, dto.Role);
                return Ok(new
                {
                    Message = "Login successful.",
                    Username = username,
                    Token = token
                    
                });
            }

            return BadRequest("Invalid role specified.");
        }






        // Get all users (Super Admin/Admin/Members)
        [HttpGet("LabUsers")]
        public async Task<IActionResult> GetAllLabUsers()
        {
            // Extract LabId from JWT token
            var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
                return Unauthorized("Invalid or missing LabId in token.");

            // Fetch Super Admins with HFID, Name, and Profile Photo
            var admins = await (from a in _context.LabAdmins
                                join u in _context.UserDetails on a.UserId equals u.user_id
                                where a.LabId == labId
                                select new
                                {
                                    AdminId = a.Id,
                                    UserId = a.UserId,
                                    LabId = a.LabId,
                                    HFID = u.user_membernumber,
                                    Name = $"{u.user_firstname} {u.user_lastname}",
                                    Role = "Super Admin",
                                    ProfilePhoto = string.IsNullOrEmpty(u.user_image) ? "No image preview available" : u.user_image, 
                                    CreatedAt = DateTimeOffset.FromUnixTimeSeconds(a.EpochTime).UtcDateTime.ToString("dd-MM-yyyy")
                                }).ToListAsync();

            // Fetch Members with HFID, Name, and Profile Photo
            var members = await (from m in _context.LabMembers
                                 join u in _context.UserDetails on m.UserId equals u.user_id
                                 where m.LabId == labId && m.DeletedBy == 0
                                 select new
                                 {
                                     MemberId = m.Id,
                                     UserId = m.UserId,
                                     LabId = m.LabId,
                                     HFID = u.user_membernumber,
                                     Name = $"{u.user_firstname} {u.user_lastname}",
                                     Role = m.Role,
                                     CreatedBy = m.CreatedBy,
                                     DeletedBy = m.DeletedBy,
                                     ProfilePhoto = string.IsNullOrEmpty(u.user_image) ? "No image preview available" : u.user_image, 
                                     CreatedAt = DateTimeOffset.FromUnixTimeSeconds(m.EpochTime).UtcDateTime.ToString("dd-MM-yyyy")
                                 }).ToListAsync();

            if (!admins.Any() && !members.Any())
                return NotFound($"No active admins or members found for LabId {labId}.");

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