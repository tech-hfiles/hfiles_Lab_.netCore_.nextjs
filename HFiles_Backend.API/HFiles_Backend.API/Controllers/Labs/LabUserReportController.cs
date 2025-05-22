using HFiles_Backend.API.DTOs.Labs;
using HFiles_Backend.Domain.Entities.Labs;
using HFiles_Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Newtonsoft.Json;


namespace HFiles_Backend.API.Controllers.Labs
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LabUserReportController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public LabUserReportController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private int GetReportTypeValue(string? reportType)
        {
            return reportType?.ToLower() switch
            {
                "lab report" => 3,
                "dental report" => 4,
                "immunization" => 5,
                "medications/prescription" => 6,
                "radiology" => 7,
                "opthalmology" => 8,
                "special report" => 9,
                "invoices/mediclaim insurance" => 10,
                _ => 0
            };
        }

        private string ReverseReportTypeMapping(int reportTypeId)
        {
            return reportTypeId switch
            {
                3 => "Lab Report",
                4 => "Dental Report",
                5 => "Immunization",
                6 => "Medications/Prescription",
                7 => "Radiology",
                8 => "Ophthalmology",
                9 => "Special Report",
                10 => "Invoices/Mediclaim Insurance",
                _ => "Unknown Report Type"
            };
        }


        // Save Lab Reports to Database
        [HttpPost("upload-batch")]
        public async Task<IActionResult> UploadReports([FromForm] LabUserReportBatchUploadDTO dto)
        {

            Console.WriteLine($"Received Payload: {JsonConvert.SerializeObject(dto, Formatting.Indented)}");


            // Get LabId (UserId) from JWT token
            var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
            {
                return Unauthorized("Invalid or missing LabId (UserId) claim.");
            }

            // DEBUG: Return for testing
            Console.WriteLine("Logged-in LabId: " + labId);

            if (dto.Entries == null || dto.Entries.Count == 0)
                return BadRequest("No entries provided.");

            string uploadsFolder = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var entryResults = new List<object>();
            int successfulUploads = 0;

            foreach (var entry in dto.Entries)
            {
                if (entry.ReportFiles == null || entry.ReportFiles.Count == 0)
                {
                    entryResults.Add(new
                    {
                        HFID = entry.HFID,
                        Email = entry.Email,
                        Status = "Failed",
                        Reason = "No report files provided"
                    });
                    continue;
                }

                var userDetails = await _context.Set<UserDetails>()
                    .FirstOrDefaultAsync(u => u.user_membernumber == entry.HFID && u.user_email == entry.Email);

                if (userDetails == null)
                {
                    string reason = $"Mismatch: HFID = {entry.HFID}, Email = {entry.Email}";
                    Console.WriteLine(reason);
                    entryResults.Add(new
                    {
                        HFID = entry.HFID,
                        Email = entry.Email,
                        Status = "Failed",
                        Reason = "HFID and Email do not match any user"
                    });
                    continue;
                }

                int userId = userDetails.user_id;
                string uploadType = userDetails.user_reference == "0" ? "independent" : "dependent";
                int reportTypeValue = GetReportTypeValue(entry.ReportType);
                long epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var uploadedFiles = new List<string>();

                foreach (var file in entry.ReportFiles)
                {
                    if (file == null || file.Length == 0)
                        continue;

                    var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}-{DateTime.Now:MM-dd-yyyy-HH-mm-ss}{Path.GetExtension(file.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var userReport = new UserReports
                    {
                        UserId = userId,
                        ReportName = Path.GetFileNameWithoutExtension(file.FileName),
                        MemberId = "0",
                        ReportUrl = fileName,
                        ReportId = reportTypeValue,
                        IsActive = "0",
                        CreatedDate = DateTime.UtcNow,
                        AccessMappingId = null,
                        FileSize = Math.Round(file.Length / 1024.0, 2),
                        UploadType = uploadType,
                        NewIsActive = null,
                        UploadedBy = "Lab",
                        LabId = labId
                    };


                    var labUserReport = new LabUserReports
                    {
                        UserId = userId,
                        LabId = labId,
                        BranchId = entry.BranchId, // From frontend
                        Name = entry.Name,
                        EpochTime = epoch
                    };

                    

                    _context.UserReports.Add(userReport);
                    _context.LabUserReports.Add(labUserReport);
                    uploadedFiles.Add(fileName);
                }

                if (uploadedFiles.Any())
                {
                    successfulUploads++;
                    entryResults.Add(new
                    {
                        HFID = entry.HFID,
                        Email = entry.Email,
                        Status = "Success",
                        UploadedFiles = uploadedFiles
                    });
                }
                else
                {
                    entryResults.Add(new
                    {
                        HFID = entry.HFID,
                        Email = entry.Email,
                        Status = "Failed",
                        Reason = "Valid user, but no report files were uploaded"
                    });
                }
            }

            await _context.SaveChangesAsync();
           
            if (successfulUploads == 0)
            {
                return BadRequest(new
                {
                    Message = "No reports were uploaded. All entries failed.",
                    Results = entryResults
                });
            }

            if (successfulUploads < dto.Entries.Count)
            {
                return StatusCode(202, new
                {
                    Message = "Some reports uploaded successfully. Others failed.",
                    Results = entryResults
                });
            }

            return Ok(new
            {
                Message = "All reports uploaded successfully.",
                Results = entryResults
            });

            
        }

        
        // Fetch All Reports of Selected User
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetLabUserReportsByUserId(int userId)
        {
            var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
            {
                return Unauthorized("Invalid or missing LabId (UserId) claim.");
            }

            // Fetch current lab details
            var currentLab = await _context.LabSignupUsers.FirstOrDefaultAsync(lsu => lsu.Id == labId);
            if (currentLab == null)
                return NotFound($"LabId {labId} not found.");

            // Find related labs
            List<int> relatedLabIds;
            if (currentLab.LabReference == 0)
            {
                // If main lab, get all its branches
                relatedLabIds = await _context.LabSignupUsers
                    .Where(lsu => lsu.LabReference == labId)
                    .Select(lsu => lsu.Id)
                    .ToListAsync();
                relatedLabIds.Add(labId); 
            }
            else
            {
                // If branch, get all other branches + main lab
                relatedLabIds = await _context.LabSignupUsers
                    .Where(lsu => lsu.LabReference == currentLab.LabReference)
                    .Select(lsu => lsu.Id)
                    .ToListAsync();
                relatedLabIds.Add(currentLab.LabReference); 
            }

            // Fetch reports for all related labs
            var userReports = await _context.UserReports
                .Where(ur => ur.UserId == userId && relatedLabIds.Contains(ur.LabId) && ur.UploadedBy == "Lab")
                .ToListAsync();

            var labUserReports = await _context.LabUserReports
                .Where(lur => lur.UserId == userId && relatedLabIds.Contains(lur.LabId))
                .OrderBy(lur => lur.EpochTime)
                .ToListAsync();

            if (!userReports.Any() && !labUserReports.Any())
                return NotFound($"No reports found for UserId {userId} in related labs.");

            // Map response
            var responseData = userReports.Select((userReport, index) =>
            {
                var matchedLabReport = labUserReports.ElementAtOrDefault(index);

                int branchId = matchedLabReport?.LabId ?? 0;
                long epochTime = matchedLabReport?.EpochTime ?? 0;

                var branchEntry = _context.LabSignupUsers.FirstOrDefault(lsu => lsu.Id == branchId);
                string branchName = branchEntry?.LabName ?? "Unknown";

                string createdDate = epochTime > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(epochTime).UtcDateTime.ToString("dd-MM-yyyy")
                    : "";

                return new
                {
                    filename = userReport.ReportName,
                    fileURL = userReport.ReportUrl,
                    labName = currentLab.LabName,
                    branchName = branchName,
                    epochTime = epochTime,
                    createdDate = createdDate
                };
            }).ToList();

            return Ok(responseData);
        }



        // Fetch All Distinct Users 
        [HttpGet("lab/reports")]
        public async Task<IActionResult> GetLabUserReports()
        {
            var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
            {
                return Unauthorized("Invalid or missing LabId (UserId) claim.");
            }

            // Fetch latest reports per user for the specified LabId
            var latestReports = await _context.LabUserReports
                .Where(lur => lur.LabId == labId)
                .GroupBy(lur => lur.UserId)
                .Select(group => group.OrderByDescending(lur => lur.EpochTime).First())
                .ToListAsync();

            if (!latestReports.Any())
                return NotFound($"No reports found for LabId {labId}.");

            // Collect all UserIds
            var userIds = latestReports.Select(lr => lr.UserId).ToList();

            // Fetch user details in a single query (avoiding per-loop execution)
            var userDetailsDict = await _context.Set<UserDetails>()
                .Where(u => userIds.Contains(u.user_id))
                .ToDictionaryAsync(u => u.user_id, u => new
                {
                    HFID = u.user_membernumber,
                    Name = $"{u.user_firstname} {u.user_lastname}"
                });

            // Fetch latest ReportId per UserId from `UserReports`
            var reportIdsDict = await _context.UserReports
                .Where(ur => userIds.Contains(ur.UserId) && ur.LabId == labId)
                .GroupBy(ur => ur.UserId)
                .Select(g => new { UserId = g.Key, ReportId = g.OrderByDescending(ur => ur.CreatedDate).First().ReportId }) 
                .ToDictionaryAsync(x => x.UserId, x => x.ReportId);


            // Prepare the response
            var responseData = latestReports.Select(report =>
            {
                var userDetail = userDetailsDict.GetValueOrDefault(report.UserId);
                if (userDetail == null) return null;

                var reportId = reportIdsDict.GetValueOrDefault(report.UserId, 0); 

                return new
                {
                    HFID = userDetail.HFID,
                    Name = userDetail.Name,
                    ReportType = ReverseReportTypeMapping(reportId), 
                    Date = DateTimeOffset.FromUnixTimeSeconds(report.EpochTime).UtcDateTime.ToString("dd-MM-yyyy")
                };
            }).Where(res => res != null).ToList();

            return Ok(responseData);
        }
    }

}

