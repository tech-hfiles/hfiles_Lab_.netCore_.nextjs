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
using HFiles_Backend.Application.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace HFiles_Backend.Controllers
{
    [ApiController]
    [Route("api/")]
    public class LabAdminController(AppDbContext context, IPasswordHasher<LabSuperAdmin> passwordHasher, JwtTokenService jwtTokenService) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly IPasswordHasher<LabSuperAdmin> _passwordHasher = passwordHasher;
        private readonly JwtTokenService _jwtTokenService = jwtTokenService;

        public static class UserRoles
        {
            public const string SuperAdmin = "Super Admin";
            public const string Admin = "Admin";
            public const string Member = "Member";
        }





        // Create Lab Super Admin
        [HttpPost("labs/super-admins")]
        public async Task<IActionResult> CreateLabAdmin([FromBody] CreateSuperAdmin dto)
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
                if (dto.UserId == 0 || string.IsNullOrEmpty(dto.Email))
                    return BadRequest(ApiResponseFactory.Fail("UserId and Email are required in the payload."));

                var lab = await _context.LabSignups.FirstOrDefaultAsync(l => l.Id == dto.UserId && l.Email == dto.Email);
                if (lab == null)
                    return NotFound(ApiResponseFactory.Fail("Invalid Credentials."));

                if (lab.IsSuperAdmin)
                    return BadRequest(ApiResponseFactory.Fail($"A Super Admin already exists for the lab {lab.LabName}."));

                if (lab.LabReference != 0)
                {
                    var parentLab = await _context.LabSignups.FirstOrDefaultAsync(l => l.Id == lab.LabReference);
                    if (parentLab != null)
                        return BadRequest(ApiResponseFactory.Fail($"{lab.LabName} is a branch of {parentLab.LabName} and cannot create a Super Admin."));
                }

                var userDetails = await _context.Set<UserDetails>()
                    .FirstOrDefaultAsync(u => u.user_membernumber == dto.HFID);
                if (userDetails == null)
                    return NotFound(ApiResponseFactory.Fail($"No user found with HFID {dto.HFID}."));

                var existingAdmin = await _context.LabSuperAdmins
                    .FirstOrDefaultAsync(a => a.UserId == userDetails.user_id && a.IsMain == 1);

                if (existingAdmin != null)
                {
                    var existingLab = await _context.LabSignups.FirstOrDefaultAsync(l => l.Id == existingAdmin.LabId);
                    return BadRequest(ApiResponseFactory.Fail($"{userDetails.user_firstname} {userDetails.user_lastname}'s HFID {dto.HFID} already exists as Super Admin under {existingLab?.LabName}."));
                }

                var newAdmin = new LabSuperAdmin
                {
                    UserId = userDetails.user_id,
                    LabId = dto.UserId,
                    PasswordHash = _passwordHasher.HashPassword(null!, dto.Password),
                    EpochTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    IsMain = 1
                };

                _context.LabSuperAdmins.Add(newAdmin);
                lab.IsSuperAdmin = true;
                _context.LabSignups.Update(lab);

                await _context.SaveChangesAsync();

                var token = _jwtTokenService.GenerateToken(dto.UserId, dto.Email, newAdmin.Id, dto.Role);

                var responseData = new
                {
                    username = $"{userDetails.user_firstname} {userDetails.user_lastname}",
                    token
                };

                return Ok(ApiResponseFactory.Success(responseData, "Super Admin created successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }





        // Users (Super Admin/Admin/Member) Login
        [HttpPost("labs/users/login")]
        public async Task<IActionResult> LabAdminLogin([FromBody] UserLogin dto)
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
                var userDetails = await _context.Set<UserDetails>()
                    .FirstOrDefaultAsync(u => u.user_membernumber == dto.HFID);

                if (userDetails == null)
                    return NotFound(ApiResponseFactory.Fail($"No Super Admin/Admin/Member found with HFID {dto.HFID}."));

                string username = $"{userDetails.user_firstname} {userDetails.user_lastname}";

                var labSignup = await _context.LabSignups
                        .FirstOrDefaultAsync(l => l.Id == dto.UserId && l.Email == dto.Email);

                if (labSignup == null)
                    return NotFound(ApiResponseFactory.Fail($"Invalid Credentials."));

                if (dto.Role == "Super Admin")
                {
                    var admin = await _context.LabSuperAdmins.FirstOrDefaultAsync(a =>
                        a.UserId == userDetails.user_id &&
                        (a.LabId == dto.UserId || a.LabId == labSignup.LabReference)
                    );

                    if (admin == null)
                        return Unauthorized(ApiResponseFactory.Fail($"The user with HFID: {dto.HFID} is not a Super Admin."));

                    if (string.IsNullOrEmpty(admin.PasswordHash))
                        return Unauthorized(ApiResponseFactory.Fail($"Password is not set for Super Admin: {username}"));

                    var passwordCheck = _passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, dto.Password);
                    if (passwordCheck != PasswordVerificationResult.Success)
                        return Unauthorized(ApiResponseFactory.Fail("Invalid password."));

                    var token = _jwtTokenService.GenerateToken(dto.UserId, dto.Email, admin.Id, dto.Role);
                    var response = new
                    {
                        Username = username,
                        Token = token
                    };

                    return Ok(ApiResponseFactory.Success(response, $"{dto.Role} successfully logged in."));
                }
                else if (dto.Role == "Admin" || dto.Role == "Member")
                {
                    var member = await _context.LabMembers.FirstOrDefaultAsync(m => m.UserId == userDetails.user_id && m.LabId == dto.UserId);
                    if (member == null)
                        return Unauthorized(ApiResponseFactory.Fail($"{dto.Role} not found. Please register first."));

                    if (string.IsNullOrEmpty(member.PasswordHash))
                        return Unauthorized(ApiResponseFactory.Fail($"Password is not set for Member/Admin: {username}"));

                    var passwordCheck = _passwordHasher.VerifyHashedPassword(null!, member.PasswordHash, dto.Password);
                    if (passwordCheck != PasswordVerificationResult.Success)
                        return Unauthorized(ApiResponseFactory.Fail("Invalid password."));

                    var token = _jwtTokenService.GenerateToken(dto.UserId, dto.Email, member.Id, dto.Role);
                    var response = new
                    {
                        Username = username,
                        Token = token
                    };

                    return Ok(ApiResponseFactory.Success(response, $"{member.Role} successfully logged in."));
                }

                return BadRequest(ApiResponseFactory.Fail("Invalid role specified."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }





        // Get all users (Super Admin/Admin/Members)
        [HttpGet("labs/{labId}/users")]
        public async Task<IActionResult> GetAllLabUsers([FromRoute] int labId)
        {
            try
            {
                var labEntry = await _context.LabSignups.FirstOrDefaultAsync(l => l.Id == labId);
                if (labEntry == null)
                    return NotFound(ApiResponseFactory.Fail($"Lab with ID {labId} not found."));

                int mainLabId = labEntry.LabReference == 0 ? labId : labEntry.LabReference;

                var superAdmin = await (from a in _context.LabSuperAdmins
                                        join u in _context.UserDetails on a.UserId equals u.user_id
                                        where a.LabId == mainLabId && a.IsMain == 1
                                        select new User
                                        {
                                            MemberId = a.Id,
                                            HFID = u.user_membernumber ?? string.Empty,
                                            Name = $"{u.user_firstname} {u.user_lastname}",
                                            Email = u.user_email ?? string.Empty,
                                            Role = UserRoles.SuperAdmin,
                                            ProfilePhoto = string.IsNullOrEmpty(u.user_image) ? "No image preview available" : u.user_image
                                        }).FirstOrDefaultAsync();

                var membersList = await (from m in _context.LabMembers
                                         join u in _context.UserDetails on m.UserId equals u.user_id
                                         where m.LabId == labId && m.DeletedBy == 0
                                         select new
                                         {
                                             MemberId = m.Id,
                                             m.UserId,
                                             HFID = u.user_membernumber,
                                             Name = $"{u.user_firstname} {u.user_lastname}",
                                             Email = u.user_email,
                                             m.Role,
                                             m.CreatedBy,
                                             m.PromotedBy,
                                             ProfilePhoto = string.IsNullOrEmpty(u.user_image) ? "No image preview available" : u.user_image
                                         }).ToListAsync();

                var labAdmins = await _context.LabSuperAdmins
                    .Where(a => a.LabId == labId)
                    .ToDictionaryAsync(a => a.Id);

                var labMembers = await _context.LabMembers
                    .ToDictionaryAsync(m => m.Id);

                var userDetails = await _context.UserDetails
                    .ToDictionaryAsync(u => u.user_id);

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
                        promotedByName = promoterDetails.user_firstname ?? "Unknown";
                    }

                    if (labAdmins.ContainsKey(m.CreatedBy))
                    {
                        createdByName = "Main";
                    }
                    else if (labMembers.TryGetValue(m.CreatedBy, out var creatingMember) &&
                             userDetails.TryGetValue(creatingMember.UserId, out var creatorDetails))
                    {
                        createdByName = creatorDetails.user_firstname ?? "Unknown";
                    }

                    return new User
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
                    return NotFound(ApiResponseFactory.Fail($"No active admins or members found for Lab ID {labId}."));

                var response = new
                {
                    LabId = labId,
                    MainLabId = mainLabId,
                    SuperAdmin = superAdmin,
                    Members = memberDtos
                };

                return Ok(ApiResponseFactory.Success(response, "Users fetched successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }





        // Promotes Admin to Super Admin
        [HttpPost("labs/admin/promote")]
        [Authorize]
        public async Task<IActionResult> PromoteLabMemberToSuperAdmin([FromBody] PromoteAdmin dto)
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
                var labAdminIdClaim = User.Claims.FirstOrDefault(c => c.Type == "LabAdminId");
                var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");

                if (labAdminIdClaim == null || !int.TryParse(labAdminIdClaim.Value, out int labAdminId))
                    return Unauthorized(ApiResponseFactory.Fail("Invalid or missing Super Admin Id in token."));

                if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
                    return Unauthorized(ApiResponseFactory.Fail("Invalid or missing labId in token."));

                var loggedInLab = await _context.LabSignups.FirstOrDefaultAsync(l => l.Id == labId);
                if (loggedInLab == null) return BadRequest(ApiResponseFactory.Fail("Lab not found"));

                int mainLabId = loggedInLab.LabReference == 0 ? labId : loggedInLab.LabReference;

                var branchIds = await _context.LabSignups
                    .Where(l => l.LabReference == mainLabId)
                    .Select(l => l.Id)
                    .ToListAsync();

                branchIds.Add(mainLabId); // ✅ Ensure main lab is included in branch validation

                // ✅ Updated: Only allow promotion if the member belongs to a valid branch
                var member = await _context.LabMembers
                 .FirstOrDefaultAsync(m =>
                     m.Id == dto.MemberId &&
                     m.DeletedBy == 0 &&
                     (branchIds.Contains(m.LabId) || m.LabId == mainLabId)); // ✅ Includes both branches and main lab

                if (member == null)
                    return NotFound(ApiResponseFactory.Fail($"No lab member found or not eligible for promotion."));

                member.DeletedBy = labAdminId;
                _context.LabMembers.Update(member);

                var currentSuperAdmin = await _context.LabSuperAdmins
                    .FirstOrDefaultAsync(a => a.IsMain == 1 && a.LabId == mainLabId); // ✅ Updated: Use mainLabId for filtering

                if (currentSuperAdmin == null)
                    return NotFound(ApiResponseFactory.Fail($"No active Super Admin found."));

                currentSuperAdmin.IsMain = 0;
                _context.LabSuperAdmins.Update(currentSuperAdmin);

                long epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var existedSuperAdmin = await _context.LabSuperAdmins.FirstOrDefaultAsync(a => a.UserId == member.UserId && a.LabId == mainLabId && a.IsMain == 0); // ✅ Updated: Assign superadmins under mainLabId
                var existedMember = await _context.LabMembers
                    .FirstOrDefaultAsync(m =>
                        m.UserId == currentSuperAdmin.UserId &&
                        (branchIds.Contains(m.LabId) || m.LabId == currentSuperAdmin.LabId) && // ✅ Ensures filtering includes branch labs
                        m.DeletedBy != 0);

                LabSuperAdmin? newSuperAdmin = null;
                LabMember? newLabMember = null;

                if (existedSuperAdmin != null)
                {
                    existedSuperAdmin.IsMain = 1;
                    existedSuperAdmin.PasswordHash = member.PasswordHash;
                    existedSuperAdmin.EpochTime = epoch;
                    _context.LabSuperAdmins.Update(existedSuperAdmin);

                    newSuperAdmin = existedSuperAdmin;
                }
                else
                {
                    newSuperAdmin = new LabSuperAdmin
                    {
                        UserId = member.UserId,
                        LabId = mainLabId, // ✅ Assign under mainLabId
                        PasswordHash = member.PasswordHash,
                        EpochTime = epoch,
                        IsMain = 1
                    };

                    _context.LabSuperAdmins.Add(newSuperAdmin);
                }

                if (existedMember != null)
                {
                    existedMember.LabId = loggedInLab.Id;
                    existedMember.Role = "Admin";
                    existedMember.DeletedBy = 0;
                    existedMember.PromotedBy = newSuperAdmin.Id;
                    existedMember.PasswordHash = currentSuperAdmin.PasswordHash;
                    existedMember.EpochTime = epoch;
                    _context.LabMembers.Update(existedMember);

                    newLabMember = existedMember;
                }
                else
                {
                    newLabMember = new LabMember
                    {
                        UserId = currentSuperAdmin.UserId,
                        LabId = loggedInLab.Id,
                        Role = "Admin",
                        PasswordHash = currentSuperAdmin.PasswordHash,
                        CreatedBy = currentSuperAdmin.Id,
                        DeletedBy = 0,
                        PromotedBy = currentSuperAdmin.Id,
                        EpochTime = epoch
                    };

                    _context.LabMembers.Add(newLabMember);
                }

                await _context.SaveChangesAsync();

                var response = new
                {
                    NewSuperAdminId = newSuperAdmin?.Id,
                    OldSuperAdminId = currentSuperAdmin.Id,
                    NewMemberId = newLabMember?.Id,
                    OldMemberId = member.Id,
                    UpdatedDeletedBy = member.DeletedBy
                };

                return Ok(ApiResponseFactory.Success(response, $"{member.Role} promoted to Super Admin successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }

    }
}