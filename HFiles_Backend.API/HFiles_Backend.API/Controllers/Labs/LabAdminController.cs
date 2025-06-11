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

namespace HFiles_Backend.API.Controllers.Labs
{
    [ApiController]
    [Route("api/")]
    public class LabAdminController(
        AppDbContext context,
        IPasswordHasher<LabSuperAdmin> passwordHasher,
        JwtTokenService jwtTokenService,
        ILogger<LabAdminController> logger,
        IServiceProvider serviceProvider) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly IPasswordHasher<LabSuperAdmin> _passwordHasher = passwordHasher;
        private readonly JwtTokenService _jwtTokenService = jwtTokenService;
        private readonly ILogger<LabAdminController> _logger = logger;
        private readonly IServiceProvider _serviceProvider = serviceProvider;

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
            HttpContext.Items["Log-Category"] = "User Management";

            _logger.LogInformation("Received request to create Super Admin. Payload: {@dto}", dto);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("Validation failed: {@Errors}", errors);
                return BadRequest(ApiResponseFactory.Fail(errors));
            }

            try
            {
                if (dto.UserId == 0 || string.IsNullOrEmpty(dto.Email))
                {
                    _logger.LogWarning("UserId or Email missing in request.");
                    return BadRequest(ApiResponseFactory.Fail("UserId and Email are required in the payload."));
                }

                var lab = await _context.LabSignups.FirstOrDefaultAsync(l => l.Id == dto.UserId && l.Email == dto.Email);
                if (lab == null)
                {
                    _logger.LogWarning("Invalid credentials provided: UserId={UserId}, Email={Email}", dto.UserId, dto.Email);
                    return NotFound(ApiResponseFactory.Fail("Invalid Credentials."));
                }

                if (lab.IsSuperAdmin)
                {
                    _logger.LogWarning("Super Admin already exists for lab {LabName}.", lab.LabName);
                    return BadRequest(ApiResponseFactory.Fail($"A Super Admin already exists for the lab {lab.LabName}."));
                }

                var userDetails = await _context.Set<UserDetails>().FirstOrDefaultAsync(u => u.user_membernumber == dto.HFID);
                if (userDetails == null)
                {
                    _logger.LogWarning("No user found with HFID {HFID}.", dto.HFID);
                    return NotFound(ApiResponseFactory.Fail($"No user found with HFID {dto.HFID}."));
                }

                _logger.LogInformation("Creating new Super Admin for user: {UserId}, Lab: {LabId}", userDetails.user_id, dto.UserId);

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

                var tokenData = _jwtTokenService.GenerateToken(dto.UserId, dto.Email, newAdmin.Id, dto.Role);

                _logger.LogInformation("Super Admin created successfully. Token Issued: {Token}, Session ID: {SessionId}", tokenData.Token, tokenData.SessionId);

                var responseData = new
                {
                    username = $"{userDetails.user_firstname} {userDetails.user_lastname}",
                    token = tokenData.Token,
                    sessionId = tokenData.SessionId
                };

                return Ok(ApiResponseFactory.Success(responseData, "Super Admin created successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Super Admin creation failed due to an unexpected error.");
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }





        // Users (Super Admin/Admin/Member) Login
        [HttpPost("labs/users/login")]
        public async Task<IActionResult> LabAdminLogin([FromBody] UserLogin dto)
        {
            HttpContext.Items["Log-Category"] = "Authentication";

            _logger.LogInformation("Received login request for HFID: {HFID}, Role: {Role}", dto.HFID, dto.Role);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("Validation failed: {@Errors}", errors);
                return BadRequest(ApiResponseFactory.Fail(errors));
            }

            try
            {
                var userDetails = await _context.Set<UserDetails>()
                    .FirstOrDefaultAsync(u => u.user_membernumber == dto.HFID);

                if (userDetails == null)
                {
                    _logger.LogWarning("Login failed: No user found with HFID {HFID}", dto.HFID);
                    return NotFound(ApiResponseFactory.Fail($"No Super Admin/Admin/Member found with HFID {dto.HFID}."));
                }

                string username = $"{userDetails.user_firstname} {userDetails.user_lastname}";

                var labSignup = await _context.LabSignups
                    .FirstOrDefaultAsync(l => l.Id == dto.UserId && l.Email == dto.Email);

                if (labSignup == null)
                {
                    _logger.LogWarning("Login failed: Invalid credentials for UserId={UserId}, Email={Email}", dto.UserId, dto.Email);
                    return NotFound(ApiResponseFactory.Fail($"Invalid Credentials."));
                }

                object response = null!;
                int? recordId = labSignup.Id;

                if (dto.Role == "Super Admin")
                {
                    var admin = await _context.LabSuperAdmins.FirstOrDefaultAsync(a =>
                        a.UserId == userDetails.user_id &&
                        (a.LabId == dto.UserId || a.LabId == labSignup.LabReference) && a.IsMain == 1
                    );

                    if (admin == null)
                    {
                        _logger.LogWarning("Login failed: User with HFID {HFID} is not a Super Admin", dto.HFID);
                        return Unauthorized(ApiResponseFactory.Fail($"The user with HFID: {dto.HFID} is not a Super Admin."));
                    }

                    if (string.IsNullOrEmpty(admin.PasswordHash))
                    {
                        _logger.LogWarning("Login failed: Password not set for Super Admin {Username}", username);
                        return Unauthorized(ApiResponseFactory.Fail($"Password is not set for Super Admin: {username}"));
                    }

                    var passwordCheck = _passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, dto.Password);
                    if (passwordCheck != PasswordVerificationResult.Success)
                    {
                        _logger.LogWarning("Login failed: Invalid password for Super Admin {Username}", username);
                        return Unauthorized(ApiResponseFactory.Fail("Invalid password."));
                    }

                    var (Token, SessionId) = _jwtTokenService.GenerateToken(dto.UserId, dto.Email, admin.Id, dto.Role);

                    _logger.LogInformation("Super Admin Login successful: {Username} | Session ID: {SessionId}", username, SessionId);

                    response = new
                    {
                        Username = username,
                        Token,
                        SessionId
                    };

                    return Ok(ApiResponseFactory.Success(response, $"{dto.Role} successfully logged in."));
                }
                else if (dto.Role == "Admin" || dto.Role == "Member")
                {
                    var member = await _context.LabMembers.FirstOrDefaultAsync(m => m.UserId == userDetails.user_id && m.LabId == dto.UserId && m.DeletedBy == 0);

                    if (member == null)
                    {
                        _logger.LogWarning("Login failed: {Role} not found for HFID {HFID}", dto.Role, dto.HFID);
                        return Unauthorized(ApiResponseFactory.Fail($"{dto.Role} not found. Please register first."));
                    }

                    if (string.IsNullOrEmpty(member.PasswordHash))
                    {
                        _logger.LogWarning("Login failed: Password not set for {Role} {Username}", dto.Role, username);
                        return Unauthorized(ApiResponseFactory.Fail($"Password is not set for Member/Admin: {username}"));
                    }

                    var passwordCheck = _passwordHasher.VerifyHashedPassword(null!, member.PasswordHash, dto.Password);
                    if (passwordCheck != PasswordVerificationResult.Success)
                    {
                        _logger.LogWarning("Login failed: Invalid password for {Role} {Username}", dto.Role, username);
                        return Unauthorized(ApiResponseFactory.Fail("Invalid password."));
                    }

                    var tokenData = _jwtTokenService.GenerateToken(dto.UserId, dto.Email, member.Id, dto.Role);

                    _logger.LogInformation("User Login successful: {Username} | Role: {Role} | Session ID: {SessionId}", username, dto.Role, tokenData.SessionId);

                    response = new
                    {
                        Username = username,
                        tokenData.Token,
                        tokenData.SessionId
                    };

                    return Ok(ApiResponseFactory.Success(response, $"{dto.Role} successfully logged in."));
                }

                _logger.LogWarning("Login failed: Invalid role specified {Role}", dto.Role);
                return BadRequest(ApiResponseFactory.Fail("Invalid role specified."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login Error: Unexpected failure occurred.");
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }





        // Get all users (Super Admin/Admin/Members)
        [HttpGet("labs/{labId}/users")]
        public async Task<IActionResult> GetAllLabUsers([FromRoute] int labId)
        {
            HttpContext.Items["Log-Category"] = "User Retrieval";

            _logger.LogInformation("Received request to fetch all users for Lab ID: {LabId}", labId);

            try
            {
                var labEntry = await _context.LabSignups.FirstOrDefaultAsync(l => l.Id == labId);
                if (labEntry == null)
                {
                    _logger.LogWarning("Lab with ID {LabId} not found.", labId);
                    return NotFound(ApiResponseFactory.Fail($"Lab with ID {labId} not found."));
                }

                int mainLabId = labEntry.LabReference == 0 ? labId : labEntry.LabReference;

                _logger.LogInformation("Fetching Super Admin details for Main Lab ID: {MainLabId}", mainLabId);
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

                _logger.LogInformation("Fetching members for Lab ID: {LabId}", labId);
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

                _logger.LogInformation("Total Members found: {MemberCount}", membersList.Count);

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
                {
                    _logger.LogWarning("No active admins or members found for Lab ID {LabId}.", labId);
                    return NotFound(ApiResponseFactory.Fail($"No active admins or members found for Lab ID {labId}."));
                }

                var response = new
                {
                    LabId = labId,
                    MainLabId = mainLabId,
                    UserCounts = membersList.Count + 1,
                    SuperAdmin = superAdmin,
                    Members = memberDtos
                };

                _logger.LogInformation("Successfully fetched users for Lab ID {LabId}. Returning response.", labId);
                return Ok(ApiResponseFactory.Success(response, "Users fetched successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching users for Lab ID {LabId}.", labId);
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }





        // Promotes Admin to Super Admin
        [HttpPost("labs/admin/promote")]
        [Authorize]
        public async Task<IActionResult> PromoteLabMemberToSuperAdmin([FromBody] PromoteAdmin dto)
        {
            HttpContext.Items["Log-Category"] = "Role Management";

            _logger.LogInformation("Received promotion request for Member ID: {MemberId}", dto.MemberId);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("Validation failed: {@Errors}", errors);
                return BadRequest(ApiResponseFactory.Fail(errors));
            }

            try
            {
                var labAdminIdClaim = User.Claims.FirstOrDefault(c => c.Type == "LabAdminId");
                var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");

                if (labAdminIdClaim == null || !int.TryParse(labAdminIdClaim.Value, out int labAdminId))
                {
                    _logger.LogWarning("Promotion failed: Invalid or missing Super Admin ID in token.");
                    return Unauthorized(ApiResponseFactory.Fail("Invalid or missing Super Admin Id in token."));
                }

                if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
                {
                    _logger.LogWarning("Promotion failed: Invalid or missing lab ID in token.");
                    return Unauthorized(ApiResponseFactory.Fail("Invalid or missing labId in token."));
                }

                var loggedInLab = await _context.LabSignups.FirstOrDefaultAsync(l => l.Id == labId);
                if (loggedInLab == null)
                {
                    _logger.LogWarning("Promotion failed: Lab ID {LabId} not found.", labId);
                    return BadRequest(ApiResponseFactory.Fail("Lab not found"));
                }

                int mainLabId = loggedInLab.LabReference == 0 ? labId : loggedInLab.LabReference;

                var branchIds = await _context.LabSignups
                    .Where(l => l.LabReference == mainLabId)
                    .Select(l => l.Id)
                    .ToListAsync();

                branchIds.Add(mainLabId);

                var member = await _context.LabMembers.FirstOrDefaultAsync(m =>
                    m.Id == dto.MemberId && m.DeletedBy == 0 && (branchIds.Contains(m.LabId) || m.LabId == mainLabId));

                if (member == null)
                {
                    _logger.LogWarning("Promotion failed: Member ID {MemberId} not found or not eligible.", dto.MemberId);
                    return NotFound(ApiResponseFactory.Fail($"No lab member found or not eligible for promotion."));
                }

                _logger.LogInformation("Promoting Member ID {MemberId} to Super Admin.", dto.MemberId);

                member.DeletedBy = labAdminId;
                _context.LabMembers.Update(member);

                var currentSuperAdmin = await _context.LabSuperAdmins.FirstOrDefaultAsync(a => a.IsMain == 1 && a.LabId == mainLabId);

                if (currentSuperAdmin == null)
                {
                    _logger.LogWarning("Promotion failed: No active Super Admin found.");
                    return NotFound(ApiResponseFactory.Fail($"No active Super Admin found."));
                }

                currentSuperAdmin.IsMain = 0;
                _context.LabSuperAdmins.Update(currentSuperAdmin);

                long epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var existedSuperAdmin = await _context.LabSuperAdmins.FirstOrDefaultAsync(a => a.UserId == member.UserId && a.LabId == mainLabId && a.IsMain == 0);
                var existedMember = await _context.LabMembers.FirstOrDefaultAsync(m =>
                    m.UserId == currentSuperAdmin.UserId && (branchIds.Contains(m.LabId) || m.LabId == currentSuperAdmin.LabId) && m.DeletedBy != 0);

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
                        LabId = mainLabId,
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

                _logger.LogInformation("Promotion completed successfully. New Super Admin ID: {NewSuperAdminId}, Old Super Admin ID: {OldSuperAdminId}", newSuperAdmin?.Id, currentSuperAdmin.Id);

                return Ok(ApiResponseFactory.Success(response, $"{member.Role} promoted to Super Admin successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Promotion failed due to an unexpected error.");
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }
    }
}