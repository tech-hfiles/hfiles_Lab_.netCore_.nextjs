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

namespace HFiles_Backend.API.Controllers.Labs
{
    [ApiController]
    [Route("api/")]
    public class LabBranchController(AppDbContext context, LabAuthorizationService labAuthorizationService) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly LabAuthorizationService _labAuthorizationService = labAuthorizationService;

        // Method to fetch Area, City & State Based on Pincode
        private static async Task<string> GetLocationDetails(string? pincode)
        {
            if (string.IsNullOrWhiteSpace(pincode))
                return "Invalid pincode";

            using var httpClient = new HttpClient();
            try
            {
                var response = await httpClient.GetAsync($"https://api.postalpincode.in/pincode/{pincode}");

                if (!response.IsSuccessStatusCode)
                    return $"Failed to fetch location details (Status Code: {response.StatusCode})";

                var jsonResponse = await response.Content.ReadAsStringAsync();

                var postalData = JsonConvert.DeserializeObject<List<LocationDetailsResponse>>(jsonResponse);

                if (postalData == null || postalData.Count == 0 || postalData[0].Status != "Success")
                    return $"Location not found for pincode {pincode}";

                var postOfficeList = postalData[0].PostOffice;

                if (postOfficeList == null || !postOfficeList.Any())
                    return "Location not found";

                var locationDetails = postOfficeList.FirstOrDefault();

                return locationDetails != null
                    ? $"{locationDetails.Name}, {locationDetails.District}, {locationDetails.State}"
                    : "Location not found";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching location details: {ex.Message}");
                return "Error retrieving location data";
            }
        }





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

                bool emailExists = await _context.LabSignupUsers.AnyAsync(u => u.Email == dto.Email);
                if (emailExists)
                    return BadRequest(ApiResponseFactory.Fail("Email already registered."));

                var parentUser = await _context.LabSignupUsers.FirstOrDefaultAsync(u => u.Id == parentLabId);
                if (parentUser == null)
                    return Unauthorized(ApiResponseFactory.Fail("Parent lab not found."));

                var epochTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var last6Epoch = epochTime % 1_000_000;
                var labPrefix = dto.LabName.Length >= 3
                    ? dto.LabName[..3].ToUpperInvariant()
                    : dto.LabName.ToUpperInvariant();

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
                    LabReference = parentLabId
                };

                _context.LabSignupUsers.Add(branchUser);
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

                var loggedInLab = await _context.LabSignupUsers.FirstOrDefaultAsync(l => l.Id == labId);
                if (loggedInLab == null)
                    return NotFound(ApiResponseFactory.Fail($"Lab with ID {labId} not found."));

                int mainLabId = loggedInLab.LabReference == 0 ? labId : loggedInLab.LabReference;

                var mainLab = await _context.LabSignupUsers.FirstOrDefaultAsync(l => l.Id == mainLabId && l.DeletedBy == 0);
                if (mainLab == null)
                    return NotFound(ApiResponseFactory.Fail($"Main lab with ID {mainLabId} not found."));

                var branches = await _context.LabSignupUsers
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
                        Location = await GetLocationDetails(mainLab.Pincode),
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
                    Location = await GetLocationDetails(branch.Pincode),
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

                var loggedInLab = await _context.LabSignupUsers.FirstOrDefaultAsync(l => l.Id == labId);
                if (loggedInLab == null)
                    return NotFound(ApiResponseFactory.Fail($"Lab with ID {labId} not found."));

                int mainLabId = loggedInLab.LabReference == 0 ? labId : loggedInLab.LabReference;

                var branch = await _context.LabSignupUsers.FirstOrDefaultAsync(l => l.Id == branchId);
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
    }
}
