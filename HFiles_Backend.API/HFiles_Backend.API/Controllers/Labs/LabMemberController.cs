using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HFiles_Backend.Infrastructure.Data;
using HFiles_Backend.Domain.Entities.Labs;
using HFiles_Backend.Application.DTOs.Labs;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using HFiles_Backend.API.Services;

namespace HFiles_Backend.API.Controllers.Labs
{
    [ApiController]
    [Route("api/")]
    public class LabMemberController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<LabMember> _passwordHasher;
        private readonly LabAuthorizationService _labAuthorizationService;

        public LabMemberController(AppDbContext context, IPasswordHasher<LabMember> passwordHasher, LabAuthorizationService labAuthorizationService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _labAuthorizationService = labAuthorizationService;
        }





        // Create Member
        [HttpPost("labs/members")]
        [Authorize]
        public async Task<IActionResult> AddMember([FromBody] CreateMemberDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
                return Unauthorized("Invalid or missing LabId claim.");

            if (!await _labAuthorizationService.IsLabAuthorized(labId, User))
                return Unauthorized("Permission denied. You can only create/modify/delete data for your main lab or its branches.");

            var userDetails = await _context.Set<UserDetails>()
                .FirstOrDefaultAsync(u => u.user_membernumber == dto.HFID);
            if (userDetails == null)
                return NotFound($"No user found with HFID {dto.HFID}.");

            var labEntry = await _context.LabSignupUsers
                .FirstOrDefaultAsync(l => l.Id == dto.BranchId); 
            if (labEntry == null)
                return NotFound($"No lab found with Branch ID {dto.BranchId}.");

            var createdByClaim = User.Claims.FirstOrDefault(c => c.Type == "LabAdminId");
            if (createdByClaim == null || !int.TryParse(createdByClaim.Value, out int createdBy))
                return Unauthorized("Invalid or missing LabAdminId in token.");

            var existingMember = await _context.LabMembers
                .FirstOrDefaultAsync(m => m.UserId == userDetails.user_id && m.LabId == dto.BranchId); 

            if (existingMember != null)
            {
                return BadRequest($"{userDetails.user_firstname} {userDetails.user_lastname}'s HFID {dto.HFID} already exists as {existingMember.Role} in Branch {dto.BranchId}.");
            }

            var newMember = new LabMember
            {
                UserId = userDetails.user_id,
                LabId = labEntry.Id,
                PasswordHash = _passwordHasher.HashPassword(null, dto.Password),
                CreatedBy = createdBy
            };

            _context.LabMembers.Add(newMember);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Member added successfully.",
                UserId = newMember.UserId,
                Name = $"{userDetails.user_firstname} {userDetails.user_lastname}",
                Email = userDetails.user_email,
                LabId = newMember.LabId,
                LabName = labEntry.LabName,
                CreatedBy = createdBy,
                Role = newMember.Role,
                EpochTime = newMember.EpochTime
            });
        }





        // Promote Members to Admins API
        [HttpPost("labs/members/promote")]
        [Authorize]
        public async Task<IActionResult> PromoteLabMembers([FromBody] PromoteMembersRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
                return Unauthorized("Invalid or missing LabId claim.");

            if (!await _labAuthorizationService.IsLabAuthorized(labId, User))
                return Unauthorized("Permission denied. You can only create/modify/delete data for your main lab or its branches.");

            if (dto.Ids == null || !dto.Ids.Any())
                return BadRequest("No member IDs provided for promotion.");

            var labAdminIdClaim = User.Claims.FirstOrDefault(c => c.Type == "LabAdminId");
            if (labAdminIdClaim == null || !int.TryParse(labAdminIdClaim.Value, out int labAdminId))
                return Unauthorized("Invalid or missing LabAdminId in token.");

            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == "Role");
            if (roleClaim == null)
                return Unauthorized("Invalid or missing Role in token.");

            string promotingRole = roleClaim.Value;

            var promotionResults = new List<PromoteMemberResultDto>();

            foreach (var memberId in dto.Ids)
            {
                var member = await _context.LabMembers.FirstOrDefaultAsync(m => m.Id == memberId && m.LabId == labId && m.DeletedBy == 0);
                if (member == null)
                {
                    promotionResults.Add(new PromoteMemberResultDto
                    {
                        Id = memberId,
                        Status = "Failed",
                        Reason = "Member not found"
                    });
                    continue;
                }

                if (member.Role == "Admin")
                {
                    promotionResults.Add(new PromoteMemberResultDto
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

                promotionResults.Add(new PromoteMemberResultDto
                {
                    Id = member.Id,
                    NewRole = "Admin",
                    Reason = "Promoted from Member to Admin",
                    PromotedBy = labAdminId,
                    PromotedByRole = promotingRole
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Member promotions completed.",
                Results = promotionResults
            });
        }





        // Delete member
        [HttpDelete("labs/members/{id}")]
        public async Task<IActionResult> DeleteLabMember(int id)
        {
            var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
                return Unauthorized("Invalid or missing LabId claim.");

            if (!await _labAuthorizationService.IsLabAuthorized(labId, User))
                return Unauthorized("Permission denied. You can only create/modify/delete data for your main lab or its branches.");

            var member = await _context.LabMembers.FirstOrDefaultAsync(m => m.Id == id && m.LabId == labId);

            if (member == null)
                return NotFound($"Lab member not found for this branch with the ID {id}");

            if (member.Role == "Admin")
                return BadRequest("Admin cannot be deleted.");

            var deletedByIdClaim = User.Claims.FirstOrDefault(c => c.Type == "LabAdminId");
            var deletedByRoleClaim = User.Claims.FirstOrDefault(c => c.Type == "Role");

            if (deletedByIdClaim == null || deletedByRoleClaim == null)
                return Unauthorized("Token is missing required information.");

            var deletedById = int.Parse(deletedByIdClaim.Value);
            var deletedByRole = deletedByRoleClaim.Value;

            member.DeletedBy = deletedById;
            _context.LabMembers.Update(member);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Member marked as deleted successfully.",
                MemberId = member.Id,
                DeletedBy = deletedById,
                DeletedByRole = deletedByRole 
            });
        }
    }
}
