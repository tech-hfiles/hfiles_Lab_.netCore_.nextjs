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


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetLabBranches()
        {
            // Extract LabId from JWT token
            var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
            {
                return Unauthorized("Invalid or missing LabId claim.");
            }

            // Fetch details of logged-in lab
            var loggedInLab = await _context.LabSignupUsers.FirstOrDefaultAsync(l => l.Id == labId);
            if (loggedInLab == null)
            {
                return NotFound($"Lab with ID {labId} not found.");
            }

            // Determine the main lab ID
            int mainLabId = loggedInLab.LabReference == 0 ? labId : loggedInLab.LabReference;

            // Fetch the main lab details
            var mainLab = await _context.LabSignupUsers.FirstOrDefaultAsync(l => l.Id == mainLabId);
            if (mainLab == null)
            {
                return NotFound($"Main lab with ID {mainLabId} not found.");
            }

            // Retrieve all branches belonging to the main lab
            var branches = await _context.LabSignupUsers
                .Where(l => l.LabReference == mainLabId)
                .ToListAsync();

            // Build flat response list
            var result = new List<object>();

            // Add main lab
            result.Add(new
            {
                labId = mainLab.Id,
                labName = mainLab.LabName,
                labType = "mainLab"
            });

            // Add branches
            result.AddRange(branches.Select(branch => new
            {
                labId = branch.Id,
                labName = branch.LabName,
                labType = "branch"
            }));

            return Ok(result);
        }

    }
}
