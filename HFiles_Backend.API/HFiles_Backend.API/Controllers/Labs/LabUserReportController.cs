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

                    // Step 1: Create and save UserReport
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

                    _context.UserReports.Add(userReport);
                    await _context.SaveChangesAsync(); // Save to generate userReport.Id

                    // Step 2: Create and save LabUserReport
                    var labUserReport = new LabUserReports
                    {
                        UserId = userId,
                        LabId = labId,
                        BranchId = entry.BranchId,
                        Name = entry.Name,
                        EpochTime = epoch
                    };

                    _context.LabUserReports.Add(labUserReport);
                    await _context.SaveChangesAsync(); // Save to generate labUserReport.Id

                    // Step 3: Link LabUserReportId back to UserReport
                    userReport.LabUserReportId = labUserReport.Id;
                    _context.UserReports.Update(userReport); // EF tracks this but safe to explicitly update
                    await _context.SaveChangesAsync();

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

            // Fetch UserDetails 
            var userDetails = await _context.Set<UserDetails>()
                .Select(u => new { u.user_id, u.user_membernumber, u.user_firstname, u.user_lastname, u.user_email, u.user_image })
                .FirstOrDefaultAsync(u => u.user_id == userId);

            if (userDetails == null)
                return NotFound($"User details not found for UserId {userId}.");

            // Generate Full Name
            string fullName = $"{userDetails.user_firstname} {userDetails.user_lastname}".Trim();

            // Find related labs
            List<int> relatedLabIds;
            if (currentLab.LabReference == 0)
            {
                relatedLabIds = await _context.LabSignupUsers
                    .Where(lsu => lsu.LabReference == labId)
                    .Select(lsu => lsu.Id)
                    .ToListAsync();
                relatedLabIds.Add(labId);
            }
            else
            {
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
            var responseData = userReports.Select(userReport =>
            {
                var matchedLabReport = labUserReports
                    .FirstOrDefault(lur => lur.UserId == userReport.UserId && lur.LabId == userReport.LabId);

                int branchId = matchedLabReport?.LabId ?? 0;
                long epochTime = matchedLabReport?.EpochTime ?? 0;
                int userReportId = userReport.Id;
                int labUserReportId = userReport.LabUserReportId ?? 0;

                var branchEntry = _context.LabSignupUsers.FirstOrDefault(lsu => lsu.Id == branchId);
                string branchName = branchEntry?.LabName ?? "Unknown";

                string createdDate = epochTime > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(epochTime).UtcDateTime.ToString("dd-MM-yyyy")
                    : "";

                return new
                {
                    Id = userReportId,
                    filename = userReport.ReportName,
                    fileURL = userReport.ReportUrl,
                    labName = currentLab.LabName,
                    branchName = branchName,
                    epochTime = epochTime,
                    createdDate = createdDate,
                    LabUserReportId = labUserReportId
                };
            }).ToList();

            // Structured response 
            return Ok(new
            {
                Message = "Reports fetched successfully.",

                UserDetails = new
                {
                    UserId = userId,
                    HFID = userDetails.user_membernumber,
                    FullName = fullName,
                    Email = userDetails.user_email,
                    UserImage = string.IsNullOrEmpty(userDetails.user_image) ? "No Image Available" : userDetails.user_image
                },

                Reports = responseData
            });
        }





        // Fetch All Distinct Users for All Dates
        [HttpGet("lab/all-reports")]
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
                    Name = $"{u.user_firstname} {u.user_lastname}",
                    UserId = u.user_id
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
                    UserId = userDetail.UserId,
                    ReportType = ReverseReportTypeMapping(reportId),
                    Date = DateTimeOffset.FromUnixTimeSeconds(report.EpochTime).UtcDateTime.ToString("dd-MM-yyyy")
                };
            }).Where(res => res != null).ToList();

            return Ok(responseData);
        }





        // Fetch All Distinct Users Based on Selection of Date
        [HttpGet("lab/reports")]
        public async Task<IActionResult> GetLabUserReports([FromQuery] string? startDate, [FromQuery] string? endDate)
        {
            var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
            {
                return Unauthorized("Invalid or missing LabId (UserId) claim.");
            }

            long startEpoch, endEpoch;

            // Validate and convert start & end date to epoch range
            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
            {
                if (!DateTime.TryParseExact(startDate, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var selectedStartDate) ||
                    !DateTime.TryParseExact(endDate, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var selectedEndDate))
                {
                    return BadRequest("Invalid date format. Use dd/MM/yyyy for both start and end dates.");
                }

                startEpoch = new DateTimeOffset(selectedStartDate.Date).ToUnixTimeSeconds();
                endEpoch = new DateTimeOffset(selectedEndDate.Date.AddDays(1).AddTicks(-1)).ToUnixTimeSeconds();
            }
            else
            {
                // Default to current and previous day if no date range is provided
                var today = DateTime.UtcNow.Date;
                var yesterday = today.AddDays(-1);
                startEpoch = new DateTimeOffset(yesterday).ToUnixTimeSeconds();
                endEpoch = new DateTimeOffset(today.AddDays(1).AddTicks(-1)).ToUnixTimeSeconds();
            }

            // Fetch latest report per user within the selected date range
            var latestReports = await _context.LabUserReports
                .Where(lur => lur.LabId == labId && lur.EpochTime >= startEpoch && lur.EpochTime <= endEpoch)
                .GroupBy(lur => lur.UserId)
                .Select(g => g.OrderByDescending(r => r.EpochTime).First())
                .ToListAsync();

            if (!latestReports.Any())
                return NotFound($"No reports found for LabId {labId} within the selected date range.");

            var userIds = latestReports.Select(lr => lr.UserId).ToList();

            var userDetailsDict = await _context.Set<UserDetails>()
                .Where(u => userIds.Contains(u.user_id))
                .ToDictionaryAsync(u => u.user_id, u => new
                {
                    HFID = u.user_membernumber,
                    Name = $"{u.user_firstname} {u.user_lastname}",
                    UserId = u.user_id
                });

            var reportIdsDict = await _context.UserReports
                .Where(ur => userIds.Contains(ur.UserId) && ur.LabId == labId)
                .GroupBy(ur => ur.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    ReportId = g.OrderByDescending(ur => ur.CreatedDate).First().ReportId
                })
                .ToDictionaryAsync(x => x.UserId, x => x.ReportId);

            var responseData = latestReports.Select(report =>
            {
                var userDetail = userDetailsDict.GetValueOrDefault(report.UserId);
                if (userDetail == null) return null;

                var reportId = reportIdsDict.GetValueOrDefault(report.UserId, 0);

                return new
                {
                    HFID = userDetail.HFID,
                    Name = userDetail.Name,
                    UserId = userDetail.UserId,
                    ReportType = ReverseReportTypeMapping(reportId),
                    Date = DateTimeOffset.FromUnixTimeSeconds(report.EpochTime).UtcDateTime.ToString("dd-MM-yyyy")
                };
            }).Where(x => x != null).ToList();

            return Ok(responseData);
        }





        // Resend Reports using LabUserReportID
        [HttpPost("resend-report")]
        public async Task<IActionResult> ResendReport([FromBody] LabResendReportDto dto)
        {
            Console.WriteLine($"Received Payload: {JsonConvert.SerializeObject(dto, Formatting.Indented)}");

            // Get LabId from JWT token
            var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
            {
                return Unauthorized("Invalid or missing LabId (UserId) claim.");
            }

            if (dto.Ids == null || dto.Ids.Count == 0)
                return BadRequest("No LabUserReport IDs provided for resending.");

            var updatedReports = new List<object>();

            foreach (var labUserReportId in dto.Ids)
            {
                var labUserReport = await _context.LabUserReports
                    .FirstOrDefaultAsync(lur => lur.Id == labUserReportId && lur.LabId == labId);

                if (labUserReport == null)
                {
                    updatedReports.Add(new
                    {
                        Id = labUserReportId,
                        Status = "Failed",
                        Reason = $"LabUserReport entry not found for Id {labUserReportId} in LabId {labId}."
                    });
                    continue;
                }

                // Increment resend count
                labUserReport.Resend += 1;
                _context.LabUserReports.Update(labUserReport);

                updatedReports.Add(new
                {
                    Id = labUserReportId,
                    Status = "Success",
                    NewResendCount = labUserReport.Resend,
                    EpochTime = labUserReport.EpochTime
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Selected LabUserReports successfully updated.",
                Results = updatedReports
            });
        }


    }

}

