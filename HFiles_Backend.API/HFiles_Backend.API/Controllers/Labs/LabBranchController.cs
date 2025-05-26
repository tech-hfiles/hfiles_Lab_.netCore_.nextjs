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



        // Create Branch
        [Authorize]
        [HttpPost("create")]
        public async Task<IActionResult> CreateBranch([FromBody] LabBranchDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.LabName) ||
                string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.PhoneNumber) ||
                string.IsNullOrWhiteSpace(dto.Pincode))
                return BadRequest("All fields are required.");

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim == null)
                return Unauthorized("Invalid token: missing UserId.");

            var parentId = int.Parse(userIdClaim.Value);

            if (await _context.LabSignupUsers.AnyAsync(u => u.Email == dto.Email))
                return BadRequest("Email already registered.");

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




        // Get All Lab Branches
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetLabBranches()
        {
            var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
            {
                return Unauthorized("Invalid or missing LabId claim.");
            }

            var loggedInLab = await _context.LabSignupUsers.FirstOrDefaultAsync(l => l.Id == labId);
            if (loggedInLab == null)
            {
                return NotFound($"Lab with ID {labId} not found.");
            }

            int mainLabId = loggedInLab.LabReference == 0 ? labId : loggedInLab.LabReference;

            var mainLab = await _context.LabSignupUsers.FirstOrDefaultAsync(l => l.Id == mainLabId);
            if (mainLab == null)
            {
                return NotFound($"Main lab with ID {mainLabId} not found.");
            }

            var branches = await _context.LabSignupUsers
                .Where(l => l.LabReference == mainLabId)
                .ToListAsync();

            var result = new List<object>();

            result.Add(new
            {
                labId = mainLab.Id,
                labName = mainLab.LabName,
                HFID = mainLab.HFID,
                Email = mainLab.Email,
                PhoneNumber = mainLab.PhoneNumber, 
                Address = mainLab.Address,
                ProfilePhoto = mainLab.ProfilePhoto,
                labType = "mainLab"
            });

            result.AddRange(branches.Select(branch => new
            {
                labId = branch.Id,
                labName = branch.LabName,
                HFID = branch.HFID,
                Email = branch.Email,
                Address = branch.Address,
                ProfilePhoto = branch.ProfilePhoto,
                labType = "branch"
            }));

            return Ok(result);
        }





        // Deletes Branch 
        [HttpDelete("delete/{branchId}")]
        [Authorize]
        public async Task<IActionResult> DeleteBranch(int branchId)
        {
            var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
            {
                return Unauthorized("Invalid or missing LabId claim.");
            }

            var branch = await _context.LabSignupUsers.FirstOrDefaultAsync(l => l.Id == branchId);
            if (branch == null)
            {
                return NotFound($"Branch with ID {branchId} not found.");
            }

            if (branch.LabReference == 0)
            {
                return BadRequest("Cannot delete the main lab. Only branches can be deleted.");
            }

            if (branch.LabReference != labId)
            {
                return Unauthorized("You do not have permission to delete this branch.");
            }

            branch.DeletedBy = labId;
            await _context.SaveChangesAsync();

            return Ok($"Branch '{branch.LabName}' (ID: {branchId}) successfully deleted.");
        }
    }
}
