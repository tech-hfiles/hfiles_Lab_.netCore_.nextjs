using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using HFiles_Backend.Application.DTOs.Labs;
using HFiles_Backend.Domain.Entities.Labs;
using HFiles_Backend.Infrastructure.Data;
using Newtonsoft.Json;
using HFiles_Backend.API.Services;

namespace HFiles_Backend.API.Controllers.Labs
{
    [ApiController]
    [Route("api/")]
    public class LabBranchController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly LabAuthorizationService _labAuthorizationService;

        public LabBranchController(AppDbContext context, LabAuthorizationService labAuthorizationService)
        {
            _context = context;
            _labAuthorizationService = labAuthorizationService;
        }

        // Method to fetch Area, City & State Based on Pincode
        private async Task<string> GetLocationDetails(string pincode)
        {
            if (string.IsNullOrWhiteSpace(pincode))
                return "Invalid pincode";

            using (var httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.GetAsync($"https://api.postalpincode.in/pincode/{pincode}");

                    if (!response.IsSuccessStatusCode)
                        return $"Failed to fetch location details (Status Code: {response.StatusCode})";

                    var jsonResponse = await response.Content.ReadAsStringAsync();

                    var postalData = JsonConvert.DeserializeObject<List<LocationDetailsResponseDto>>(jsonResponse);

                    if (postalData == null || postalData.Count == 0 || postalData[0].Status != "Success")
                        return "Location not found";

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
        }





        // Create Branch
        [HttpPost("labs/branches")]
        [Authorize]
        public async Task<IActionResult> CreateBranch([FromBody] BranchDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
                return Unauthorized("Invalid or missing LabId claim.");

            if (!await _labAuthorizationService.IsLabAuthorized(labId, User))
                return Unauthorized("Permission denied. You can only create/modify/delete data for your main lab or its branches.");

            var parentId = int.Parse(labIdClaim.Value);

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





        // Get All Labs (Main Lab + All Branches)
        [HttpGet("labs")]
        [Authorize]
        public async Task<IActionResult> GetLabBranches()
        {
            var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
                return Unauthorized("Invalid or missing LabId claim.");

            if (!await _labAuthorizationService.IsLabAuthorized(labId, User))
                return Unauthorized("Permission denied. You can only create/modify/delete data for your main lab or its branches.");

            var loggedInLab = await _context.LabSignupUsers.FirstOrDefaultAsync(l => l.Id == labId);
            if (loggedInLab == null)
                return NotFound($"Lab with ID {labId} not found.");

            int mainLabId = loggedInLab.LabReference == 0 ? labId : loggedInLab.LabReference;

            var mainLab = await _context.LabSignupUsers.FirstOrDefaultAsync(l => l.Id == mainLabId && l.DeletedBy == 0);
            if (mainLab == null)
                return NotFound($"Main lab with ID {mainLabId} not found.");

            var branches = await _context.LabSignupUsers
                .Where(l => l.LabReference == mainLabId && l.DeletedBy == 0)
                .ToListAsync();

            var result = new List<LabInfoDto>
            {
                new LabInfoDto
                {
                    LabId = mainLab.Id,
                    LabName = mainLab.LabName,
                    HFID = mainLab.HFID,
                    Email = mainLab.Email,
                    PhoneNumber = mainLab.PhoneNumber,
                    Pincode = mainLab.Pincode,
                    Location = await GetLocationDetails(mainLab.Pincode),
                    Address = mainLab.Address,
                    ProfilePhoto = mainLab.ProfilePhoto,
                    LabType = "mainLab"
                }
            };

            var branchTasks = branches.Select(async branch => new LabInfoDto
            {
                LabId = branch.Id,
                LabName = branch.LabName,
                HFID = branch.HFID,
                Email = branch.Email,
                PhoneNumber = branch.PhoneNumber,
                Pincode = branch.Pincode,
                Location = await GetLocationDetails(branch.Pincode),  
                Address = branch.Address,
                ProfilePhoto = branch.ProfilePhoto,
                LabType = "branch"
            });

            result.AddRange(await Task.WhenAll(branchTasks)); 

            return Ok(result);
        }






        // Deletes Branch 
        [HttpDelete("labs/branches/{branchId}")]
        [Authorize]
        public async Task<IActionResult> DeleteBranch(int branchId)
        {
            var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
                return Unauthorized("Invalid or missing LabId claim.");

            if (!await _labAuthorizationService.IsLabAuthorized(branchId, User))
                return Unauthorized("Permission denied. You can only create/modify/delete data for your main lab or its branches.");

            var loggedInLab = await _context.LabSignupUsers.FirstOrDefaultAsync(l => l.Id == labId);
            if (loggedInLab == null)
                return NotFound($"Lab with ID {labId} not found.");

            int mainLabId = loggedInLab.LabReference == 0 ? labId : loggedInLab.LabReference;

            var branch = await _context.LabSignupUsers.FirstOrDefaultAsync(l => l.Id == branchId);
            if (branch == null)
                return NotFound($"Branch with ID {branchId} not found.");

            if (branch.LabReference == 0)
                return BadRequest("Cannot delete the main lab. Permission denied");

            if (branch.LabReference != mainLabId)
                return Forbid($"Branch with ID {branchId} does not belong to the main lab. Permission denied.");

            if (branch.DeletedBy != 0)
                return BadRequest("This branch has already been deleted.");

            branch.DeletedBy = labId;
            await _context.SaveChangesAsync();

            return Ok($"Branch '{branch.LabName}' (ID: {branchId}) successfully deleted.");
        }

    }
}
