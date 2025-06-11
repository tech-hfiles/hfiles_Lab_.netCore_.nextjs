using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HFiles_Backend.Infrastructure.Data;
using HFiles_Backend.Domain.Entities.Labs;
using HFiles_Backend.Application.DTOs.Labs;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using HFiles_Backend.API.Services;
using HFiles_Backend.Application.Common;

namespace HFiles_Backend.API.Controllers.Labs
{
    [ApiController]
    [Route("api/")]
    public class LabMemberController(AppDbContext context, IPasswordHasher<LabMember> passwordHasher, LabAuthorizationService labAuthorizationService, ILogger<LabMemberController> logger) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly IPasswordHasher<LabMember> _passwordHasher = passwordHasher;
        private readonly LabAuthorizationService _labAuthorizationService = labAuthorizationService;
        private readonly ILogger<LabMemberController> _logger = logger;





        // Create Member
        [HttpPost("labs/members")]
        [Authorize]
        public async Task<IActionResult> AddMember([FromBody] CreateMember dto)
        {
            HttpContext.Items["Log-Category"] = "User Management";

            _logger.LogInformation("Received request to create new lab member. HFID: {HFID}, Branch ID: {BranchId}", dto.HFID, dto.BranchId);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("Validation failed: {@Errors}", errors);
                return BadRequest(ApiResponseFactory.Fail(errors));
            }

            try
            {
                var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
                {
                    _logger.LogWarning("Member creation failed: Invalid or missing LabId claim.");
                    return Unauthorized(ApiResponseFactory.Fail("Invalid or missing LabId claim."));
                }

                if (!await _labAuthorizationService.IsLabAuthorized(labId, User))
                {
                    _logger.LogWarning("Member creation failed: Unauthorized access for Lab ID {LabId}", labId);
                    return Unauthorized(ApiResponseFactory.Fail("Permission denied. You can only create/modify/delete data for your main lab or its branches."));
                }

                var userDetails = await _context.Set<UserDetails>()
                    .FirstOrDefaultAsync(u => u.user_membernumber == dto.HFID);
                if (userDetails == null)
                {
                    _logger.LogWarning("Member creation failed: No user found with HFID {HFID}.", dto.HFID);
                    return NotFound(ApiResponseFactory.Fail($"No user found with HFID {dto.HFID}."));
                }

                var labEntry = await _context.LabSignups.FirstOrDefaultAsync(l => l.Id == dto.BranchId);
                if (labEntry == null)
                {
                    _logger.LogWarning("Member creation failed: No lab found with Branch ID {BranchId}.", dto.BranchId);
                    return NotFound(ApiResponseFactory.Fail($"No lab found with Branch ID {dto.BranchId}."));
                }

                int mainLabId = labEntry.LabReference == 0 ? labId : labEntry.LabReference;

                var createdByClaim = User.Claims.FirstOrDefault(c => c.Type == "LabAdminId");
                if (createdByClaim == null || !int.TryParse(createdByClaim.Value, out int createdBy))
                {
                    _logger.LogWarning("Member creation failed: Invalid or missing LabAdminId in token.");
                    return Unauthorized(ApiResponseFactory.Fail("Invalid or missing LabAdminId in token."));
                }

                var existingMember = await _context.LabMembers
                    .FirstOrDefaultAsync(m => m.UserId == userDetails.user_id &&
                                              (m.LabId == dto.BranchId || m.LabId == labEntry.LabReference || m.LabId == mainLabId));
                if (existingMember != null)
                {
                    string fullName = $"{userDetails.user_firstname} {userDetails.user_lastname}";
                    _logger.LogWarning("Member creation failed: User {FullName} with HFID {HFID} already exists in Branch {BranchId}.", fullName, dto.HFID, existingMember.LabId);
                    return BadRequest(ApiResponseFactory.Fail($"{fullName}'s HFID {dto.HFID} already exists as {existingMember.Role} in Branch {existingMember.LabId}."));
                }

                var superAdmin = await _context.LabSuperAdmins
                    .FirstOrDefaultAsync(a => a.UserId == userDetails.user_id && a.IsMain == 1 && a.LabId == labId);
                if (superAdmin != null)
                {
                    _logger.LogWarning("Member creation failed: User with HFID {HFID} is already a registered Super Admin.", dto.HFID);
                    return BadRequest(ApiResponseFactory.Fail("User is already a registered Super Admin."));
                }

                var newMember = new LabMember
                {
                    UserId = userDetails.user_id,
                    LabId = labEntry.Id,
                    PasswordHash = _passwordHasher.HashPassword(null!, dto.Password),
                    CreatedBy = createdBy
                };

                _context.LabMembers.Add(newMember);
                await _context.SaveChangesAsync();

                var responseData = new
                {
                    newMember.UserId,
                    Name = $"{userDetails.user_firstname} {userDetails.user_lastname}",
                    Email = userDetails.user_email,
                    newMember.LabId,
                    labEntry.LabName,
                    CreatedBy = createdBy,
                    newMember.Role,
                    newMember.EpochTime
                };

                _logger.LogInformation("New lab member created successfully. User ID: {UserId}, Lab ID: {LabId}.", newMember.UserId, newMember.LabId);
                return Ok(ApiResponseFactory.Success(responseData, "Member added successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Member creation failed due to an unexpected error.");
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }






        // Promote Members to Admins API
        [HttpPost("labs/members/promote")]
        [Authorize]
        public async Task<IActionResult> PromoteLabMembers([FromBody] PromoteMembersRequest dto)
        {
            HttpContext.Items["Log-Category"] = "Role Management";

            _logger.LogInformation("Received request to promote lab members. Promoting Role: {Role}, Member IDs: {Ids}",
                User.Claims.FirstOrDefault(c => c.Type == "Role")?.Value, dto.Ids);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("Validation failed: {@Errors}", errors);
                return BadRequest(ApiResponseFactory.Fail(errors));
            }

            try
            {
                var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
                {
                    _logger.LogWarning("Promotion failed: Invalid or missing LabId claim.");
                    return Unauthorized(ApiResponseFactory.Fail("Invalid or missing LabId claim."));
                }

                if (!await _labAuthorizationService.IsLabAuthorized(labId, User))
                {
                    _logger.LogWarning("Promotion failed: Unauthorized access for Lab ID {LabId}", labId);
                    return Unauthorized(ApiResponseFactory.Fail("Permission denied. You can only manage data for your main lab or its branches."));
                }

                if (dto.Ids == null || !dto.Ids.Any())
                {
                    _logger.LogWarning("Promotion failed: No member IDs provided.");
                    return BadRequest(ApiResponseFactory.Fail("No member IDs provided for promotion."));
                }

                var labAdminIdClaim = User.Claims.FirstOrDefault(c => c.Type == "LabAdminId");
                if (labAdminIdClaim == null || !int.TryParse(labAdminIdClaim.Value, out int labAdminId))
                {
                    _logger.LogWarning("Promotion failed: Invalid or missing LabAdminId.");
                    return Unauthorized(ApiResponseFactory.Fail("Invalid or missing LabAdminId in token."));
                }

                var roleClaim = User.Claims.FirstOrDefault(c => c.Type == "Role");
                if (roleClaim == null)
                {
                    _logger.LogWarning("Promotion failed: Invalid or missing Role in token.");
                    return Unauthorized(ApiResponseFactory.Fail("Invalid or missing Role in token."));
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

                string promotingRole = roleClaim.Value;

                var successResults = new List<PromoteMemberResult>();
                var failedOrSkippedResults = new List<PromoteMemberResult>();

                foreach (var memberId in dto.Ids)
                {

                    var member = await _context.LabMembers
                        .FirstOrDefaultAsync(m => m.Id == memberId && (branchIds.Contains(m.LabId) || m.LabId == mainLabId) && m.DeletedBy == 0);

                    if (member == null)
                    {
                        _logger.LogWarning("Promotion failed: Member ID {MemberId} not found.", memberId);
                        failedOrSkippedResults.Add(new PromoteMemberResult { Id = memberId, Status = "Failed", Reason = "Member not found" });
                        continue;
                    }

                    if (member.Role == "Admin")
                    {
                        _logger.LogWarning("Promotion skipped: Member ID {MemberId} is already an Admin.", member.Id);
                        failedOrSkippedResults.Add(new PromoteMemberResult { Id = member.Id, Status = "Skipped", Reason = "Already an Admin" });
                        continue;
                    }

                    member.Role = "Admin";
                    member.PromotedBy = labAdminId;
                    _context.LabMembers.Update(member);

                    successResults.Add(new PromoteMemberResult
                    {
                        Id = member.Id,
                        Status = "Success",
                        NewRole = "Admin",
                        PromotedBy = labAdminId,
                        PromotedByRole = promotingRole
                    });
                }

                await _context.SaveChangesAsync();

                var allResults = successResults.Concat(failedOrSkippedResults).ToList();

                if (successResults.Count != 0)
                {
                    _logger.LogInformation("Promotion successful for {Count} members. Promoted By: {LabAdminId}", successResults.Count, labAdminId);
                    return Ok(ApiResponseFactory.Success(allResults, "Member successfully promoted to Admin."));
                }

                _logger.LogWarning("Promotion failed: No members were successfully promoted.");
                return BadRequest(ApiResponseFactory.Fail([.. failedOrSkippedResults.Select(r => $"{r.Reason}")]));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Promotion failed due to an unexpected error.");
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }






        // Delete member
        [HttpDelete("labs/members/{memberId}")]
        [Authorize]
        public async Task<IActionResult> DeleteLabMember([FromRoute] int memberId)
        {
            HttpContext.Items["Log-Category"] = "User Management";
            HttpContext.Items["MemberId"] = memberId;

            _logger.LogInformation("Received request to delete lab member. Member ID: {MemberId}", memberId);

            try
            {
                var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
                {
                    _logger.LogWarning("Member deletion failed: Invalid or missing LabId claim.");
                    return Unauthorized(ApiResponseFactory.Fail("Invalid or missing LabId claim."));
                }

                if (!await _labAuthorizationService.IsLabAuthorized(labId, User))
                {
                    _logger.LogWarning("Member deletion failed: Unauthorized access for Lab ID {LabId}", labId);
                    return Unauthorized(ApiResponseFactory.Fail("Permission denied. You can only manage data for your main lab or its branches."));
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

                var member = await _context.LabMembers
                    .FirstOrDefaultAsync(m => m.Id == memberId && (branchIds.Contains(m.LabId) || m.LabId == mainLabId) && m.DeletedBy == 0);

                if (member == null)
                {
                    _logger.LogWarning("Member deletion failed: Member ID {MemberId} not found in Lab ID {LabId}.", memberId, labId);
                    return NotFound(ApiResponseFactory.Fail($"Lab member not found."));
                }

                var deletedByIdClaim = User.Claims.FirstOrDefault(c => c.Type == "LabAdminId");
                var deletedByRoleClaim = User.Claims.FirstOrDefault(c => c.Type == "Role");

                if (deletedByIdClaim == null || deletedByRoleClaim == null ||
                    !int.TryParse(deletedByIdClaim.Value, out int deletedById))
                {
                    _logger.LogWarning("Member deletion failed: Missing or invalid deletion claims (LabAdminId/Role).");
                    return Unauthorized(ApiResponseFactory.Fail("Missing or invalid deletion claims (LabAdminId/Role)."));
                }

                member.DeletedBy = deletedById;
                _context.LabMembers.Update(member);
                await _context.SaveChangesAsync();

                var response = new
                {
                    MemberId = member.Id,
                    DeletedBy = deletedById,
                    DeletedByRole = deletedByRoleClaim.Value
                };

                _logger.LogInformation("Lab member deletion successful. Member ID: {MemberId}, Deleted By: {DeletedBy}, Role: {DeletedByRole}.",
                    member.Id, deletedById, deletedByRoleClaim.Value);

                return Ok(ApiResponseFactory.Success(response, $"{member.Role} deleted successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Member deletion failed due to an unexpected error for Member ID {MemberId}", memberId);
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }
    }
}
