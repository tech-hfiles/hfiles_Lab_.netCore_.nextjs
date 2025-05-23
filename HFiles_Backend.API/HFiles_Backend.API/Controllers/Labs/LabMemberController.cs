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

        [HttpPost("create")]
        [Authorize]
        public async Task<IActionResult> AddMember([FromBody] LabMemberDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.HFID)) return BadRequest("HFID is required.");
            if (string.IsNullOrWhiteSpace(dto.BranchName)) return BadRequest("Branch name is required.");
            if (string.IsNullOrWhiteSpace(dto.Password)) return BadRequest("Password is required.");

            // Fetch UserId from UserDetails using HFID
            var userDetails = await _context.Set<UserDetails>().FirstOrDefaultAsync(u => u.user_membernumber == dto.HFID);
            if (userDetails == null)
                return NotFound($"No user found with HFID {dto.HFID}.");

            // Fetch LabId using BranchName from LabSignupUsers
            var labEntry = await _context.LabSignupUsers.FirstOrDefaultAsync(l => l.LabName == dto.BranchName);
            if (labEntry == null)
                return NotFound($"No lab found with Branch Name {dto.BranchName}.");

            // Fetch CreatedBy (LabAdminId) from JWT Token
            var createdByClaim = User.Claims.FirstOrDefault(c => c.Type == "LabAdminId");
            if (createdByClaim == null || !int.TryParse(createdByClaim.Value, out int createdBy))
                return Unauthorized("Invalid or missing LabAdminId in token.");

            // Create and store Member entity
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
    }
}
