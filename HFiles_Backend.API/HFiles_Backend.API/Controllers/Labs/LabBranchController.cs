using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using HFiles_Backend.Application.DTOs.Labs;
using HFiles_Backend.Domain.Entities.Labs;
using HFiles_Backend.Infrastructure.Data;

namespace HFiles_Backend.API.Controllers.Labs
{
    [ApiController]
    [Route("api/[controller]")]
    public class LabBranchController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LabBranchController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize]
        [HttpPost("create")]
        public async Task<IActionResult> CreateBranch([FromBody] LabBranchDto dto)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(dto.LabName) ||
                string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.PhoneNumber) ||
                string.IsNullOrWhiteSpace(dto.Pincode))
                return BadRequest("All fields are required.");

            // Get user ID from token
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim == null)
                return Unauthorized("Invalid token: missing UserId.");

            var parentId = int.Parse(userIdClaim.Value);

            // Check if email already exists
            if (await _context.LabSignupUsers.AnyAsync(u => u.Email == dto.Email))
                return BadRequest("Email already registered.");

            // Fetch parent user to copy password
            var parentUser = await _context.LabSignupUsers.FirstOrDefaultAsync(u => u.Id == parentId);
            if (parentUser == null)
                return Unauthorized("Parent lab not found.");

            
            var epochTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var last6Epoch = epochTime % 1000000;
            var labPrefix = dto.LabName.Length >= 3 ? dto.LabName.Substring(0, 3).ToUpper() : dto.LabName.ToUpper();
            var randomDigits = new Random().Next(1000, 9999);
            var hfid = $"HF{last6Epoch}{labPrefix}{randomDigits}";

            var branchUser = new LabSignupUser
            {
                LabName = dto.LabName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Pincode = dto.Pincode,
                PasswordHash = parentUser.PasswordHash, 
                HFID = hfid,
                CreatedAtEpoch = epochTime,
                LabReference = parentId
            };

            _context.LabSignupUsers.Add(branchUser);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Branch lab created successfully.",
                Id = branchUser.Id,
                HFID = branchUser.HFID
            });
        }
    }
}
