using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using HFiles_Backend.Domain.Entities.Labs;
using HFiles_Backend.Application.DTOs.Labs;
using Microsoft.AspNetCore.Identity;
using HFiles_Backend.API.Services;
using HFiles_Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

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
        public static class UserRoles
        {
            public const string SuperAdmin = "Super Admin";
            public const string Admin = "Admin";
            public const string Member = "Member";
        }





        // Create Lab Admin
        [HttpPost("labs/super-admins")]
        public async Task<IActionResult> CreateLabAdmin([FromBody] CreateSuperAdminDto dto)
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

            var userDetails = await _context.Set<UserDetails>()
                .FirstOrDefaultAsync(u => u.user_membernumber == dto.HFID);
            if (userDetails == null)
                return NotFound($"No user found with HFID {dto.HFID}.");

            var existingAdmin = await _context.LabAdmins
                .FirstOrDefaultAsync(a => a.UserId == userDetails.user_id && a.IsMain == 1);

            if (existingAdmin != null)
            {
                var existingLab = await _context.LabSignupUsers.FirstOrDefaultAsync(l => l.Id == existingAdmin.LabId);
                return BadRequest($"{userDetails.user_firstname} {userDetails.user_lastname}'s HFID {dto.HFID} already exists as Super Admin under {existingLab?.LabName}.");
            }

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
        [HttpPost("labs/users/login")]
        public async Task<IActionResult> LabAdminLogin([FromBody] UserLoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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

                if (admin.IsMain != 1)
                    return Unauthorized($"User {username} with role Super Admin has no longer access to login.");

                if (!_passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, dto.Password)
                    .Equals(PasswordVerificationResult.Success))
                    return Unauthorized("Invalid password.");

                var token = _jwtTokenService.GenerateToken(dto.UserId, dto.Email, admin.Id, dto.Role);
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

                var token = _jwtTokenService.GenerateToken(dto.UserId, dto.Email, member.Id, dto.Role);
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
        [HttpGet("labs/users")]
        public async Task<IActionResult> GetAllLabUsers([FromQuery] int labId)
        {
            var labEntry = await _context.LabSignupUsers.FirstOrDefaultAsync(l => l.Id == labId);
            if (labEntry == null)
                return NotFound($"Lab with ID {labId} not found.");

            int mainLabId = labEntry.LabReference == 0 ? labId : labEntry.LabReference;

            var superAdmin = await (from a in _context.LabAdmins
                                    join u in _context.UserDetails on a.UserId equals u.user_id
                                    where a.LabId == mainLabId && a.IsMain == 1
                                    select new UserDto
                                    {
                                        MemberId = a.Id,
                                        HFID = u.user_membernumber,
                                        Name = $"{u.user_firstname} {u.user_lastname}",
                                        Email = u.user_email,
                                        Role = UserRoles.SuperAdmin,
                                        ProfilePhoto = string.IsNullOrEmpty(u.user_image) ? "No image preview available" : u.user_image
                                    }).FirstOrDefaultAsync();

            var membersList = await (from m in _context.LabMembers
                                     join u in _context.UserDetails on m.UserId equals u.user_id
                                     where m.LabId == labId && m.DeletedBy == 0
                                     select new
                                     {
                                         MemberId = m.Id,
                                         UserId = m.UserId,
                                         HFID = u.user_membernumber,
                                         Name = $"{u.user_firstname} {u.user_lastname}",
                                         Email = u.user_email,
                                         Role = m.Role,
                                         CreatedBy = m.CreatedBy,
                                         PromotedBy = m.PromotedBy,
                                         ProfilePhoto = string.IsNullOrEmpty(u.user_image) ? "No image preview available" : u.user_image
                                     }).ToListAsync();

            var labAdmins = await _context.LabAdmins
                .Where(a => a.LabId == labId)
                .ToDictionaryAsync(a => a.Id, a => a);

            var labMembers = await _context.LabMembers
                .ToDictionaryAsync(m => m.Id, m => m);

            var userDetails = await _context.UserDetails
                .ToDictionaryAsync(u => u.user_id, u => u);

            var memberDtos = membersList.Select(m =>
            {
                string promotedByName = "Not Promoted Yet";
                string createdByName = "Unknown";

                if (labAdmins.ContainsKey(m.PromotedBy))
                {
                    promotedByName = "Main";
                }
                else if (labMembers.TryGetValue(m.PromotedBy, out var promotingMember) &&
                         userDetails.TryGetValue(promotingMember.UserId, out var promoterDetails))
                {
                    promotedByName = promoterDetails.user_firstname;
                }

                if (labAdmins.ContainsKey(m.CreatedBy))
                {
                    createdByName = "Main";
                }
                else if (labMembers.TryGetValue(m.CreatedBy, out var creatingMember) &&
                         userDetails.TryGetValue(creatingMember.UserId, out var creatorDetails))
                {
                    createdByName = creatorDetails.user_firstname;
                }

                return new UserDto
                {
                    MemberId = m.MemberId,
                    HFID = m.HFID,
                    Name = m.Name,
                    Email = m.Email,
                    Role = m.Role,
                    CreatedByName = createdByName,
                    PromotedByName = promotedByName,
                    ProfilePhoto = m.ProfilePhoto
                };
            }).ToList();

            if (superAdmin == null && !memberDtos.Any())
                return NotFound($"No active admins or members found for LabId {labId}.");

            return Ok(new
            {
                Message = "Lab users fetched successfully.",
                LabId = labId,
                MainLabId = mainLabId,
                SuperAdmin = superAdmin,
                Members = memberDtos
            });
        }





        // Promotes Admin to Super Admin
        [HttpPost("labs/admin/promote")]
        [Authorize]
        public async Task<IActionResult> PromoteLabMemberToSuperAdmin([FromBody] PromoteAdminDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.MemberId <= 0)
                return BadRequest("Valid MemberId is required.");

            var labAdminIdClaim = User.Claims.FirstOrDefault(c => c.Type == "LabAdminId");
            var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (labAdminIdClaim == null || !int.TryParse(labAdminIdClaim.Value, out int labAdminId))
                return Unauthorized("Invalid or missing Super Admin Id in token.");
            if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
                return Unauthorized("Invalid or missing labId in token.");

            var member = await _context.LabMembers.FirstOrDefaultAsync(m => m.Id == dto.MemberId);
            if (member == null)
                return NotFound($"No lab member found with ID {dto.MemberId}.");

            member.DeletedBy = labAdminId;
            _context.LabMembers.Update(member);

            var currentSuperAdmin = await _context.LabAdmins.FirstOrDefaultAsync(a => a.IsMain == 1 && a.LabId == labId);

            if (currentSuperAdmin == null)
                return NotFound($"No active Super Admin found for Lab ID {labId}.");

            if (currentSuperAdmin != null)
            {
                currentSuperAdmin.IsMain = 0; 
                _context.LabAdmins.Update(currentSuperAdmin);
            }

            long epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var newSuperAdmin = new LabAdmin
            {
                UserId = member.UserId,
                LabId = member.LabId,
                PasswordHash = member.PasswordHash,
                EpochTime = epoch,
                IsMain = 1
            };

            _context.LabAdmins.Add(newSuperAdmin);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Lab Admin promoted to Super Admin successfully.",
                NewSuperAdminId = newSuperAdmin.Id,
                OldSuperAdminId = currentSuperAdmin?.Id,
                UpdatedDeletedBy = member.DeletedBy
            });
        }

    }
}