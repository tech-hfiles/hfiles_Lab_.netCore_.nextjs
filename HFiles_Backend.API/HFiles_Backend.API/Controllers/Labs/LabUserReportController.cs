using HFiles_Backend.API.DTOs.Labs;
using HFiles_Backend.Domain.Entities.Labs;
using HFiles_Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Newtonsoft.Json;
using HFiles_Backend.API.Services;
using System.Linq;
using System.Globalization;
using HFiles_Backend.Application.Common;


namespace HFiles_Backend.API.Controllers.Labs
{
    [Route("api/")]
    [ApiController]
    [Authorize]
    public class LabUserReportController(AppDbContext context, IWebHostEnvironment env, LabAuthorizationService labAuthorizationService) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly IWebHostEnvironment _env = env;
        private readonly LabAuthorizationService _labAuthorizationService = labAuthorizationService;

        private static int GetReportTypeValue(string? reportType)
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

        private static string ReverseReportTypeMapping(int reportTypeId)
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





        // Upload single/batch lab reports of muliple users
        [HttpPost("labs/reports/upload")]
        public async Task<IActionResult> UploadReports([FromForm] UserReportBatchUploadDTO dto)
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

                if (dto.Entries == null || dto.Entries.Count == 0)
                    return BadRequest(ApiResponseFactory.Fail("No entries provided in the payload."));

                string uploadsFolder = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var entryResults = new List<object>();
                int successfulUploads = 0;

                foreach (var entry in dto.Entries)
                {
                    if (entry.ReportFiles == null || entry.ReportTypes == null)
                    {
                        entryResults.Add(new { entry.HFID, entry.Email, Status = "Failed", Reason = "ReportFiles or ReportTypes missing" });
                        continue;
                    }

                    if (entry.ReportFiles.Count != entry.ReportTypes.Count)
                    {
                        entryResults.Add(new { entry.HFID, entry.Email, Status = "Failed", Reason = $"Mismatch between files ({entry.ReportFiles.Count}) and report types ({entry.ReportTypes.Count})" });
                        continue;
                    }

                    if (entry.ReportFiles.Count == 0)
                    {
                        entryResults.Add(new { entry.HFID, entry.Email, Status = "Failed", Reason = "No report files provided" });
                        continue;
                    }

                    var userDetails = await _context.Set<UserDetails>()
                        .FirstOrDefaultAsync(u => u.user_membernumber == entry.HFID && u.user_email == entry.Email);

                    if (userDetails == null)
                    {
                        entryResults.Add(new { entry.HFID, entry.Email, Status = "Failed", Reason = "HFID and Email do not match any user" });
                        continue;
                    }

                    int userId = userDetails.user_id;
                    string uploadType = userDetails.user_reference == "0" ? "independent" : "dependent";
                    long epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    var uploadedFiles = new List<object>();

                    for (int i = 0; i < entry.ReportFiles.Count; i++)
                    {
                        var file = entry.ReportFiles[i];
                        var reportType = entry.ReportTypes[i];

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
                            ReportId = GetReportTypeValue(reportType),
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
                        await _context.SaveChangesAsync();

                        var labUserReport = new LabUserReports
                        {
                            UserId = userId,
                            LabId = labId,
                            BranchId = entry.BranchId,
                            Name = entry.Name,
                            EpochTime = epoch
                        };

                        _context.LabUserReports.Add(labUserReport);
                        await _context.SaveChangesAsync();

                        userReport.LabUserReportId = labUserReport.Id;
                        _context.UserReports.Update(userReport);
                        await _context.SaveChangesAsync();

                        uploadedFiles.Add(new { FileName = fileName, ReportType = reportType });
                    }

                    if (uploadedFiles.Any())
                    {
                        successfulUploads++;
                        entryResults.Add(new { entry.HFID, entry.Email, Status = "Success", UploadedFiles = uploadedFiles });
                    }
                    else
                    {
                        entryResults.Add(new { entry.HFID, entry.Email, Status = "Failed", Reason = "Valid user, but no report files were uploaded" });
                    }
                }

                if (successfulUploads == 0)
                {
                    return BadRequest(ApiResponseFactory.Fail(entryResults, "No reports were uploaded. All entries failed."));
                }

                if (successfulUploads < dto.Entries.Count)
                {
                    return StatusCode(202, ApiResponseFactory.PartialSuccess(entryResults, "Some reports uploaded successfully. Others failed."));
                }

                return Ok(ApiResponseFactory.Success(entryResults, "All reports uploaded successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }





        // Fetch All Reports of Selected User
        [HttpGet("labs/reports/{userId}")]
        public async Task<IActionResult> GetLabUserReportsByUserId(int userId)
        {
            try
            {
                var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
                    return Unauthorized(ApiResponseFactory.Fail("Invalid or missing LabId claim."));

                if (!await _labAuthorizationService.IsLabAuthorized(labId, User))
                    return Unauthorized(ApiResponseFactory.Fail("Permission denied. You can only create/modify/delete data for your main lab or its branches."));

                var currentLab = await _context.LabSignupUsers.FirstOrDefaultAsync(lsu => lsu.Id == labId);
                if (currentLab == null)
                    return NotFound(ApiResponseFactory.Fail($"LabId {labId} not found."));

                var userDetails = await _context.Set<UserDetails>()
                    .Select(u => new
                    {
                        u.user_id,
                        u.user_membernumber,
                        u.user_firstname,
                        u.user_lastname,
                        u.user_email,
                        u.user_image
                    })
                    .FirstOrDefaultAsync(u => u.user_id == userId);

                if (userDetails == null)
                    return NotFound(ApiResponseFactory.Fail($"User details not found for UserId {userId}."));

                string fullName = $"{userDetails.user_firstname} {userDetails.user_lastname}".Trim();

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

                var userReports = await _context.UserReports
                    .Where(ur => ur.UserId == userId
                                 && ur.LabId != null
                                 && relatedLabIds.Contains(ur.LabId.Value)
                                 && ur.UploadedBy == "Lab")
                    .ToListAsync();

                var labUserReportsDict = await _context.LabUserReports
                    .Where(lur => lur.UserId == userId && relatedLabIds.Contains(lur.LabId))
                    .ToDictionaryAsync(lur => lur.Id, lur => lur);

                if (userReports.Count == 0 && labUserReportsDict.Count == 0)
                    return NotFound(ApiResponseFactory.Fail($"No reports found for UserId {userId} in related labs."));

                var allBranchIds = labUserReportsDict.Values.Select(l => l.BranchId).Distinct().ToList();

                var branchNamesDict = await _context.LabSignupUsers
                    .Where(lsu => allBranchIds.Contains(lsu.Id))
                    .ToDictionaryAsync(lsu => lsu.Id, lsu => lsu.LabName);

                var responseData = userReports.Select(userReport =>
                {
                    int labUserReportId = userReport.LabUserReportId ?? 0;

                    labUserReportsDict.TryGetValue(labUserReportId, out var matchedLabReport);
                    int branchId = matchedLabReport?.BranchId ?? 0;
                    long epochTime = matchedLabReport?.EpochTime ?? 0;

                    string createdDate = epochTime > 0
                        ? DateTimeOffset.FromUnixTimeSeconds(epochTime).UtcDateTime.ToString("dd-MM-yyyy")
                        : "";

                    string branchName = branchNamesDict.TryGetValue(branchId, out string? value)
                                        ? value ?? "Unknown Branch"
                                        : currentLab.LabName ?? "Unknown Lab";

                    return new
                    {
                        userReport.Id,
                        filename = userReport.ReportName,
                        fileURL = userReport.ReportUrl,
                        labName = currentLab.LabName,
                        branchName,
                        epochTime,
                        createdDate,
                        LabUserReportId = labUserReportId
                    };
                }).ToList();

                return Ok(ApiResponseFactory.Success(new
                {
                    UserDetails = new
                    {
                        UserId = userId,
                        HFID = userDetails.user_membernumber,
                        FullName = fullName,
                        Email = userDetails.user_email,
                        UserImage = string.IsNullOrEmpty(userDetails.user_image) ? "No Image Available" : userDetails.user_image
                    },
                    Reports = responseData
                }, "Reports fetched successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }





        // Fetch All Distinct Users for All Dates (Currently we do not use this API in Frontend)
        [HttpGet("labs/reports/all")]
        public async Task<IActionResult> GetLabUserReports()
        {
            try
            {
                var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
                    return Unauthorized(ApiResponseFactory.Fail("Invalid or missing LabId claim."));

                if (!await _labAuthorizationService.IsLabAuthorized(labId, User))
                    return Unauthorized(ApiResponseFactory.Fail("Permission denied. You can only access data for your main lab or its branches."));

                var latestReports = await _context.LabUserReports
                    .Where(lur => lur.LabId == labId)
                    .GroupBy(lur => lur.UserId)
                    .Select(group => group.OrderByDescending(lur => lur.EpochTime).First())
                    .ToListAsync();

                if (latestReports.Count == 0)
                    return NotFound(ApiResponseFactory.Fail($"No reports found for LabId {labId}."));

                var userIds = latestReports.Select(lr => lr.UserId).ToList();

                var userDetailsDict = await _context.Set<UserDetails>()
                    .Where(u => userIds.Contains(u.user_id))
                    .ToDictionaryAsync(u => u.user_id, u => new
                    {
                        HFID = u.user_membernumber,
                        Name = $"{u.user_firstname} {u.user_lastname}".Trim(),
                        UserId = u.user_id
                    });

                var reportIdsDict = await _context.UserReports
                    .Where(ur => userIds.Contains(ur.UserId) && ur.LabId == labId)
                    .GroupBy(ur => ur.UserId)
                    .Select(g => new { UserId = g.Key, g.OrderByDescending(ur => ur.CreatedDate).First().ReportId })
                    .ToDictionaryAsync(x => x.UserId, x => x.ReportId);

                var responseData = latestReports.Select(report =>
                {
                    if (!userDetailsDict.TryGetValue(report.UserId, out var userDetail))
                        return null;

                    reportIdsDict.TryGetValue(report.UserId, out int reportId);

                    return new
                    {
                        userDetail.HFID,
                        userDetail.Name,
                        userDetail.UserId,
                        ReportType = ReverseReportTypeMapping(reportId),
                        Date = DateTimeOffset.FromUnixTimeSeconds(report.EpochTime).UtcDateTime.ToString("dd-MM-yyyy")
                    };
                }).Where(x => x != null).ToList();

                return Ok(ApiResponseFactory.Success(responseData, "Reports fetched successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }





        // Fetch All Distinct Users Based on Selection of Date
        [HttpGet("labs/{labId}/reports")]
        public async Task<IActionResult> GetLabUserReports([FromRoute] int labId, [FromQuery] string? startDate, [FromQuery] string? endDate)
        {
            try
            {
                if (labId <= 0)
                    return BadRequest(ApiResponseFactory.Fail("Invalid LabId."));

                var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int loggedInLabId))
                    return Unauthorized(ApiResponseFactory.Fail("Invalid or missing LabId claim."));

                if (!await _labAuthorizationService.IsLabAuthorized(labId, User))
                    return Unauthorized(ApiResponseFactory.Fail("Permission denied. You can only create/modify/delete data for your main lab or its branches."));

                long startEpoch, endEpoch;

                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                {
                    if (!DateTime.TryParseExact(startDate, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var selectedStartDate) ||
                        !DateTime.TryParseExact(endDate, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var selectedEndDate))
                    {
                        return BadRequest(ApiResponseFactory.Fail("Invalid date format. Use dd/MM/yyyy for both start and end dates."));
                    }

                    startEpoch = new DateTimeOffset(selectedStartDate.Date).ToUnixTimeSeconds();
                    endEpoch = new DateTimeOffset(selectedEndDate.Date.AddDays(1).AddTicks(-1)).ToUnixTimeSeconds();
                }
                else
                {
                    var today = DateTime.UtcNow.Date;
                    var yesterday = today.AddDays(-1);
                    startEpoch = new DateTimeOffset(yesterday).ToUnixTimeSeconds();
                    endEpoch = new DateTimeOffset(today.AddDays(1).AddTicks(-1)).ToUnixTimeSeconds();
                }

                var filteredReports = await _context.LabUserReports
                    .Where(lur => (lur.LabId == labId && lur.BranchId == 0) || lur.BranchId == labId)
                    .Where(lur => lur.EpochTime >= startEpoch && lur.EpochTime <= endEpoch)
                    .GroupBy(lur => lur.UserId)
                    .Select(g => g.OrderByDescending(r => r.EpochTime).First())
                    .ToListAsync();

                if (filteredReports.Count == 0)
                    return NotFound(ApiResponseFactory.Fail($"No reports found for LabId {labId}"));

                var userIds = filteredReports.Select(lr => lr.UserId).ToList();

                var userDetailsDict = await _context.Set<UserDetails>()
                    .Where(u => userIds.Contains(u.user_id))
                    .ToDictionaryAsync(u => u.user_id, u => new
                    {
                        HFID = u.user_membernumber,
                        Name = $"{u.user_firstname} {u.user_lastname}".Trim(),
                        UserId = u.user_id
                    });

                var reportIdsDict = await _context.UserReports
                    .Where(ur => userIds.Contains(ur.UserId) && ur.LabId == labId)
                    .GroupBy(ur => ur.UserId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        g.OrderByDescending(ur => ur.CreatedDate).First().ReportId
                    })
                    .ToDictionaryAsync(x => x.UserId, x => x.ReportId);

                var responseData = filteredReports.Select(report =>
                {
                    if (!userDetailsDict.TryGetValue(report.UserId, out var userDetail))
                        return null;

                    reportIdsDict.TryGetValue(report.UserId, out int reportId);

                    return new
                    {
                        report.Id,
                        userDetail.HFID,
                        userDetail.Name,
                        userDetail.UserId,
                        ReportType = ReverseReportTypeMapping(reportId),
                        Date = DateTimeOffset.FromUnixTimeSeconds(report.EpochTime).UtcDateTime.ToString("dd-MM-yyyy")
                    };
                }).Where(x => x != null).ToList();

                return Ok(ApiResponseFactory.Success(responseData, "Reports fetched successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }






        // Resend Reports using LabUserReportID
        [HttpPost("labs/reports/resend")]
        public async Task<IActionResult> ResendReport([FromBody] ResendReportDto dto)
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

                if (dto.Ids == null || dto.Ids.Count == 0)
                    return BadRequest(ApiResponseFactory.Fail("No LabUserReport IDs provided for resending."));

                var successReports = new List<object>();
                var failedReports = new List<object>();

                foreach (var labUserReportId in dto.Ids)
                {
                    var labUserReport = await _context.LabUserReports
                        .FirstOrDefaultAsync(lur => lur.Id == labUserReportId && lur.LabId == labId);

                    if (labUserReport == null)
                    {
                        failedReports.Add(new
                        {
                            Id = labUserReportId,
                            Status = "Failed",
                            Reason = $"LabUserReport entry not found for Id {labUserReportId} in LabId {labId}."
                        });
                        continue;
                    }

                    labUserReport.Resend += 1;
                    _context.LabUserReports.Update(labUserReport);

                    successReports.Add(new
                    {
                        Id = labUserReportId,
                        Status = "Success",
                        NewResendCount = labUserReport.Resend,
                        labUserReport.EpochTime
                    });
                }

                await _context.SaveChangesAsync();

                var result = new
                {
                    Success = successReports,
                    Failed = failedReports
                };

                if (failedReports.Count == 0)
                    return Ok(ApiResponseFactory.Success(result, "All reports resent successfully."));

                if (successReports.Count == 0)
                    return BadRequest(ApiResponseFactory.Fail(result, "All reports resend operations failed."));

                return Ok(ApiResponseFactory.PartialSuccess(result, "Some reports were resent successfully, others failed."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }





        // Fetch Daily/Weekly/Monthly/Custom Dates Notifications
        [HttpGet("labs/{labId}/notifications")]
        public async Task<IActionResult> GetLabNotifications(
        [FromRoute] int labId,
        [FromQuery] int? timeframe,
        [FromQuery] string? startDate,
        [FromQuery] string? endDate)
        {
            try
            {
                if (!await _labAuthorizationService.IsLabAuthorized(labId, User))
                    return Unauthorized(ApiResponseFactory.Fail("Permission denied. You can only create/modify/delete data for your main lab or its branches."));

                var labAdminIdClaim = User.Claims.FirstOrDefault(c => c.Type == "LabAdminId");
                var roleClaim = User.Claims.FirstOrDefault(c => c.Type == "Role");

                if (labAdminIdClaim == null || !int.TryParse(labAdminIdClaim.Value, out int labAdminId))
                    return Unauthorized(ApiResponseFactory.Fail("Invalid or missing LabAdminId claim."));

                if (roleClaim == null)
                    return Unauthorized(ApiResponseFactory.Fail("Invalid or missing Role claim."));

                string role = roleClaim.Value;
                int? userId = null;

                if (role == "Super Admin")
                {
                    userId = await _context.LabAdmins
                        .Where(a => a.Id == labAdminId)
                        .Select(a => (int?)a.UserId)
                        .FirstOrDefaultAsync();
                }
                else if (role == "Admin" || role == "Member")
                {
                    userId = await _context.LabMembers
                        .Where(m => m.LabId == labId && m.Id == labAdminId)
                        .Select(m => (int?)m.UserId)
                        .FirstOrDefaultAsync();
                }

                if (userId == null)
                    return Unauthorized(ApiResponseFactory.Fail("Invalid access for the given role."));

                long currentEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                long epochStart, epochEnd;

                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                {
                    if (!DateTimeOffset.TryParseExact(startDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDateParsed) ||
                        !DateTimeOffset.TryParseExact(endDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDateParsed))
                    {
                        return BadRequest(ApiResponseFactory.Fail("Invalid date format. Please use DD/MM/YYYY."));
                    }

                    epochStart = startDateParsed.ToUnixTimeSeconds();
                    epochEnd = endDateParsed.AddHours(23).AddMinutes(59).AddSeconds(59).ToUnixTimeSeconds();
                }
                else
                {
                    epochStart = timeframe switch
                    {
                        1 => currentEpoch - 86400,   
                        2 => currentEpoch - 604800,    
                        3 => currentEpoch - 2592000,       
                        _ => currentEpoch - 86400
                    };
                    epochEnd = currentEpoch;
                }

                var recentReports = await _context.LabUserReports
                    .Where(r => r.LabId == labId && r.EpochTime >= epochStart && r.EpochTime <= epochEnd)
                    .ToListAsync();

                if (!recentReports.Any())
                    return NotFound(ApiResponseFactory.Fail("No reports found for the selected timeframe or date range."));

                var reportIds = recentReports.Select(r => r.Id).ToList();
                var userReports = await _context.UserReports
                    .Where(ur => ur.LabUserReportId != null && reportIds.Contains(ur.LabUserReportId.Value))
                    .ToListAsync();

                var userIds = userReports.Select(ur => ur.UserId).Distinct().ToList();
                var userDetailsDict = await _context.UserDetails
                    .Where(ud => userIds.Contains(ud.user_id))
                    .ToDictionaryAsync(ud => ud.user_id, ud => ud.user_firstname);

                var labAdminUser = await _context.UserDetails
                    .Where(ud => ud.user_id == userId)
                    .Select(ud => ud.user_firstname)
                    .FirstOrDefaultAsync() ?? "Unknown Admin";

                var notifications = userReports
                    .Select(ur =>
                    {
                        var labReport = recentReports.FirstOrDefault(lr => lr.Id == ur.LabUserReportId);
                        return new
                        {
                            ReportType = ReverseReportTypeMapping(ur.ReportId),
                            SentTo = userDetailsDict.TryGetValue(ur.UserId, out var userName) ? userName : "Unknown User",
                            SentBy = labAdminUser,
                            ElapsedMinutes = labReport != null ? (currentEpoch - labReport.EpochTime) / 60 : int.MaxValue
                        };
                    })
                    .OrderBy(n => n.ElapsedMinutes)
                    .ToList();

                return Ok(ApiResponseFactory.Success(notifications, "Notifications fetched successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }
    }
}

