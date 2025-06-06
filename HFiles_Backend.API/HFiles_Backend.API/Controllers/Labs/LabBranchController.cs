using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using HFiles_Backend.Application.DTOs.Labs;
using HFiles_Backend.Domain.Entities.Labs;
using HFiles_Backend.Infrastructure.Data;
using Newtonsoft.Json;
using HFiles_Backend.API.Services;
using HFiles_Backend.Application.Common;
using System.Net.Http;
using Org.BouncyCastle.Bcpg.Sig;

namespace HFiles_Backend.API.Controllers.Labs
{
    [ApiController]
    [Route("api/")]
    public class LabBranchController(AppDbContext context, LabAuthorizationService labAuthorizationService, HttpClient httpClient, LocationService locationService) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly LabAuthorizationService _labAuthorizationService = labAuthorizationService;
        private readonly HttpClient _httpClient = httpClient;
        private readonly LocationService _locationService = locationService;
        




        // Create Branch
        [HttpPost("labs/branches")]
        [Authorize]
        public async Task<IActionResult> CreateBranch([FromBody] Branch dto)
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

                var parentLabId = labId;

                bool emailExists = await _context.LabSignups.AnyAsync(u => u.Email == dto.Email);
                if (emailExists)
                    return BadRequest(ApiResponseFactory.Fail("Email already registered."));

                var parentUser = await _context.LabSignups.FirstOrDefaultAsync(u => u.Id == parentLabId);
                if (parentUser == null)
                    return Unauthorized(ApiResponseFactory.Fail("Parent lab not found."));

                var epochTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var last6Epoch = epochTime % 1_000_000;
                var labPrefix = dto.LabName.Length >= 3
                    ? dto.LabName[..3].ToUpperInvariant()
                    : dto.LabName.ToUpperInvariant();

                var randomDigits = new Random().Next(1000, 9999);
                var hfid = $"HF{last6Epoch}{labPrefix}{randomDigits}";

                var branchUser = new LabSignup
                {
                    LabName = dto.LabName,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    Pincode = dto.Pincode,
                    PasswordHash = parentUser.PasswordHash,
                    HFID = hfid,
                    CreatedAtEpoch = epochTime,
                    LabReference = parentLabId
                };

                _context.LabSignups.Add(branchUser);
                await _context.SaveChangesAsync();

                var response = new
                {
                    branchUser.Id,
                    branchUser.HFID
                };

                return Ok(ApiResponseFactory.Success(response, "Branch created successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }





        // Get All Labs (Main Lab + All Branches)
        [HttpGet("labs")]
        [Authorize]
        public async Task<IActionResult> GetLabBranches()
        {
            try
            {
                var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
                    return Unauthorized(ApiResponseFactory.Fail("Invalid or missing LabId claim."));

                if (!await _labAuthorizationService.IsLabAuthorized(labId, User))
                    return Unauthorized(ApiResponseFactory.Fail("Permission denied. You can only create/modify/delete data for your main lab or its branches."));

                var loggedInLab = await _context.LabSignups.FirstOrDefaultAsync(l => l.Id == labId);
                if (loggedInLab == null)
                    return NotFound(ApiResponseFactory.Fail($"Lab with ID {labId} not found."));

                int mainLabId = loggedInLab.LabReference == 0 ? labId : loggedInLab.LabReference;

                var mainLab = await _context.LabSignups.FirstOrDefaultAsync(l => l.Id == mainLabId && l.DeletedBy == 0);
                if (mainLab == null)
                    return NotFound(ApiResponseFactory.Fail($"Main lab with ID {mainLabId} not found."));

                var branches = await _context.LabSignups
                    .Where(l => l.LabReference == mainLabId && l.DeletedBy == 0)
                    .ToListAsync();

                var result = new List<LabInfo>
                {
                    new() {
                        LabId = mainLab.Id,
                        LabName = mainLab.LabName,
                        HFID = mainLab.HFID,
                        Email = mainLab.Email ?? "No email available",
                        PhoneNumber = mainLab.PhoneNumber ?? "No phone number available",
                        Pincode = mainLab.Pincode ?? "No pincode available",
                        Location = await _locationService.GetLocationDetails(mainLab.Pincode),
                        Address = mainLab.Address ?? "No address available",
                        ProfilePhoto = mainLab.ProfilePhoto ?? "No image preview available",
                        LabType = "mainLab"
                    }
                };

                var branchTasks = branches.Select(async branch => new LabInfo
                {
                    LabId = branch.Id,
                    LabName = branch.LabName,
                    HFID = branch.HFID,
                    Email = branch.Email ?? "No email available",
                    PhoneNumber = branch.PhoneNumber ?? "No phone number available",
                    Pincode = branch.Pincode ?? "No pincode available",
                    Location = await _locationService.GetLocationDetails(branch.Pincode),
                    Address = branch.Address ?? "No address available",
                    ProfilePhoto = branch.ProfilePhoto ?? "No image preview available",
                    LabType = "branch"
                });

                result.AddRange(await Task.WhenAll(branchTasks));

                return Ok(ApiResponseFactory.Success(result, "Lab branches fetched successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }





        // Delete Branch 
        [HttpDelete("labs/branches/{branchId}")]
        [Authorize]
        public async Task<IActionResult> DeleteBranch([FromRoute] int branchId)
        {
            try
            {
                var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
                    return Unauthorized(ApiResponseFactory.Fail("Invalid or missing LabId claim."));

                if (!await _labAuthorizationService.IsLabAuthorized(branchId, User))
                    return Unauthorized(ApiResponseFactory.Fail("Permission denied. You can only manage your main lab or its branches."));

                var loggedInLab = await _context.LabSignups.FirstOrDefaultAsync(l => l.Id == labId);
                if (loggedInLab == null)
                    return NotFound(ApiResponseFactory.Fail($"Lab with ID {labId} not found."));

                int mainLabId = loggedInLab.LabReference == 0 ? labId : loggedInLab.LabReference;

                var branch = await _context.LabSignups.FirstOrDefaultAsync(l => l.Id == branchId);
                if (branch == null)
                    return NotFound(ApiResponseFactory.Fail($"Branch with ID {branchId} not found."));

                if (branch.LabReference == 0)
                    return BadRequest(ApiResponseFactory.Fail("Cannot delete the main lab."));

                if (branch.LabReference != mainLabId)
                    return Unauthorized(ApiResponseFactory.Fail($"Branch with ID {branchId} does not belong to your lab."));

                if (branch.DeletedBy != 0)
                    return BadRequest(ApiResponseFactory.Fail("This branch has already been deleted."));

                branch.DeletedBy = labId;
                await _context.SaveChangesAsync();

                var response = new
                {
                    BranchId = branch.Id,
                    BranchName = branch.LabName,
                    DeletedBy = labId
                };

                return Ok(ApiResponseFactory.Success(response, $"Branch deleted successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }





        // Method to fetch Area, City & State Based on Pincode
        [HttpGet("labs/branches/{pincode}")]
        [Authorize]
        public async Task<IActionResult> GetLocation(string pincode)
        {
            var location = await _locationService.GetLocationDetails(pincode);

            return Ok(new { success = true, location });
        }
    }
}
