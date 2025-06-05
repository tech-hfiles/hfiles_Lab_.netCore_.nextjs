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
    public class LabMemberController(AppDbContext context, IPasswordHasher<LabMember> passwordHasher, LabAuthorizationService labAuthorizationService) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly IPasswordHasher<LabMember> _passwordHasher = passwordHasher;
        private readonly LabAuthorizationService _labAuthorizationService = labAuthorizationService;





        // Create Member
        [HttpPost("labs/members")]
        [Authorize]
        public async Task<IActionResult> AddMember([FromBody] CreateMember dto)
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
                var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
                    return Unauthorized(ApiResponseFactory.Fail("Invalid or missing LabId claim."));

                if (!await _labAuthorizationService.IsLabAuthorized(labId, User))
                    return Unauthorized(ApiResponseFactory.Fail("Permission denied. You can only create/modify/delete data for your main lab or its branches."));

                var userDetails = await _context.Set<UserDetails>()
                    .FirstOrDefaultAsync(u => u.user_membernumber == dto.HFID);
                if (userDetails == null)
                    return NotFound(ApiResponseFactory.Fail($"No user found with HFID {dto.HFID}."));

                var labEntry = await _context.LabSignups
                    .FirstOrDefaultAsync(l => l.Id == dto.BranchId);
                if (labEntry == null)
                    return NotFound(ApiResponseFactory.Fail($"No lab found with Branch ID {dto.BranchId}."));

                int mainLabId = labEntry.LabReference == 0 ? labId : labEntry.LabReference;

                var createdByClaim = User.Claims.FirstOrDefault(c => c.Type == "LabAdminId");
                if (createdByClaim == null || !int.TryParse(createdByClaim.Value, out int createdBy))
                    return Unauthorized(ApiResponseFactory.Fail("Invalid or missing LabAdminId in token."));

                var existingMember = await _context.LabMembers
                    .FirstOrDefaultAsync(m => m.UserId == userDetails.user_id && (m.LabId == dto.BranchId || m.LabId == labEntry.LabReference || m.LabId == mainLabId));  
                if (existingMember != null)
                {
                    string fullName = $"{userDetails.user_firstname} {userDetails.user_lastname}";
                    return BadRequest(ApiResponseFactory.Fail($"{fullName}'s HFID {dto.HFID} already exists as {existingMember.Role} in Branch {existingMember.LabId}."));
                }

                var superAdmin = await _context.LabSuperAdmins
                    .FirstOrDefaultAsync(a => a.UserId == userDetails.user_id && a.IsMain == 1&& a.LabId == labId);

                if (superAdmin != null)
                {
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

                return Ok(ApiResponseFactory.Success(responseData, "Member added successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }





        // Promote Members to Admins API
        [HttpPost("labs/members/promote")]
        [Authorize]
        public async Task<IActionResult> PromoteLabMembers([FromBody] PromoteMembersRequest dto)
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
                var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
                    return Unauthorized(ApiResponseFactory.Fail("Invalid or missing LabId claim."));

                if (!await _labAuthorizationService.IsLabAuthorized(labId, User))
                    return Unauthorized(ApiResponseFactory.Fail("Permission denied. You can only manage data for your main lab or its branches."));

                if (dto.Ids == null || !dto.Ids.Any())
                    return BadRequest(ApiResponseFactory.Fail("No member IDs provided for promotion."));

                var labAdminIdClaim = User.Claims.FirstOrDefault(c => c.Type == "LabAdminId");
                if (labAdminIdClaim == null || !int.TryParse(labAdminIdClaim.Value, out int labAdminId))
                    return Unauthorized(ApiResponseFactory.Fail("Invalid or missing LabAdminId in token."));

                var roleClaim = User.Claims.FirstOrDefault(c => c.Type == "Role");
                if (roleClaim == null)
                    return Unauthorized(ApiResponseFactory.Fail("Invalid or missing Role in token."));

                string promotingRole = roleClaim.Value;

                var successResults = new List<PromoteMemberResult>();
                var failedOrSkippedResults = new List<PromoteMemberResult>();

                foreach (var memberId in dto.Ids)
                {
                    var member = await _context.LabMembers
                        .FirstOrDefaultAsync(m => m.Id == memberId && m.LabId == labId && m.DeletedBy == 0);

                    if (member == null)
                    {
                        failedOrSkippedResults.Add(new PromoteMemberResult
                        {
                            Id = memberId,
                            Status = "Failed",
                            Reason = "Member not found"
                        });
                        continue;
                    }

                    if (member.Role == "Admin")
                    {
                        failedOrSkippedResults.Add(new PromoteMemberResult
                        {
                            Id = member.Id,
                            Status = "Skipped",
                            Reason = "Already an Admin"
                        });
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
                    return Ok(ApiResponseFactory.Success(allResults, "Member successfully promoted to Admin."));
                }

                return BadRequest(ApiResponseFactory.Fail([.. failedOrSkippedResults.Select(r => $"{r.Reason}")]));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }





        // Delete member
        [HttpDelete("labs/members/{memberId}")]
        [Authorize]
        public async Task<IActionResult> DeleteLabMember([FromRoute] int memberId)
        {
            try
            {
                var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
                    return Unauthorized(ApiResponseFactory.Fail("Invalid or missing LabId claim."));

                if (!await _labAuthorizationService.IsLabAuthorized(labId, User))
                    return Unauthorized(ApiResponseFactory.Fail("Permission denied. You can only manage data for your main lab or its branches."));

                var member = await _context.LabMembers
                    .FirstOrDefaultAsync(m => m.Id == memberId && m.LabId == labId && m.DeletedBy == 0);

                if (member == null)
                    return NotFound(ApiResponseFactory.Fail($"Lab member not found."));

                var deletedByIdClaim = User.Claims.FirstOrDefault(c => c.Type == "LabAdminId");
                var deletedByRoleClaim = User.Claims.FirstOrDefault(c => c.Type == "Role");

                if (deletedByIdClaim == null || deletedByRoleClaim == null ||
                    !int.TryParse(deletedByIdClaim.Value, out int deletedById))
                {
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

                return Ok(ApiResponseFactory.Success(response, $"{member.Role} deleted successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }
    }
}
