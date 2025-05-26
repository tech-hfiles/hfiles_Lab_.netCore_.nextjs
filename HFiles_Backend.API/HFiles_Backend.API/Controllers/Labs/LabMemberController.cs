using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HFiles_Backend.Infrastructure.Data;
using HFiles_Backend.Domain.Entities.Labs;
using HFiles_Backend.Application.DTOs.Labs;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace HFiles_Backend.API.Controllers.Labs
{
    [ApiController]
    [Route("api/[controller]")]
    public class LabMemberController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<LabMember> _passwordHasher;

        public LabMemberController(AppDbContext context, IPasswordHasher<LabMember> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }


        // Create Members
        [HttpPost("create")]
        [Authorize]
        public async Task<IActionResult> AddMember([FromBody] LabMemberDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.HFID)) return BadRequest("HFID is required.");
            if (string.IsNullOrWhiteSpace(dto.BranchName)) return BadRequest("Branch name is required.");
            if (string.IsNullOrWhiteSpace(dto.Password)) return BadRequest("Password is required.");

            var userDetails = await _context.Set<UserDetails>().FirstOrDefaultAsync(u => u.user_membernumber == dto.HFID);
            if (userDetails == null)
                return NotFound($"No user found with HFID {dto.HFID}.");

            var labEntry = await _context.LabSignupUsers.FirstOrDefaultAsync(l => l.LabName == dto.BranchName);
            if (labEntry == null)
                return NotFound($"No lab found with Branch Name {dto.BranchName}.");

            var createdByClaim = User.Claims.FirstOrDefault(c => c.Type == "LabAdminId");
            if (createdByClaim == null || !int.TryParse(createdByClaim.Value, out int createdBy))
                return Unauthorized("Invalid or missing LabAdminId in token."); 

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





        // Promote members to Admins API
        [HttpPost("promote")]
        public async Task<IActionResult> PromoteMembers([FromBody] LabPromoteMembersDto dto)
        {
            if (dto.Ids == null || !dto.Ids.Any())
                return BadRequest("No member IDs provided for promotion.");

            var labAdminIdClaim = User.Claims.FirstOrDefault(c => c.Type == "LabAdminId");
            if (labAdminIdClaim == null || !int.TryParse(labAdminIdClaim.Value, out int labAdminId))
                return Unauthorized("Invalid or missing LabAdminId in token.");

            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == "Role");
            if (roleClaim == null)
                return Unauthorized("Invalid or missing Role in token.");

            string promotingRole = roleClaim.Value; 

            var promotedMembers = new List<object>();

            foreach (var memberId in dto.Ids)
            {
                var member = await _context.LabMembers.FirstOrDefaultAsync(m => m.Id == memberId);
                if (member == null)
                {
                    promotedMembers.Add(new { Id = memberId, Status = "Failed", Reason = "Member not found" });
                    continue;
                }

                member.Role = "Admin";
                member.PromotedBy = labAdminId; 
                _context.LabMembers.Update(member);

                promotedMembers.Add(new
                {
                    Id = member.Id,
                    UserId = member.UserId,
                    LabId = member.LabId,
                    NewRole = member.Role,
                    PromotedBy = labAdminId,
                    PromotedByRole = promotingRole 
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Members promoted successfully.",
                PromotedMembers = promotedMembers
            });
        }




        // Delete member
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteLabMember(int id)
        {
            var member = await _context.LabMembers.FirstOrDefaultAsync(m => m.Id == id);

            if (member == null)
                return NotFound("Lab member not found.");

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
