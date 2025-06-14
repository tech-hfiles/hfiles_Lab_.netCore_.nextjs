using System.Globalization;
using HFiles_Backend.API.DTOs.Labs;
using HFiles_Backend.API.Services;
using HFiles_Backend.Application.Common;
using HFiles_Backend.Application.DTOs.Labs;
using HFiles_Backend.Domain.Entities.Labs;
using HFiles_Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sprache;


namespace HFiles_Backend.API.Controllers.Labs
{
    [Route("api/")]
    [ApiController]
    [Authorize]
    public class LabUserReportController(
    AppDbContext context,
    IWebHostEnvironment env,
    LabAuthorizationService labAuthorizationService,
    EmailService emailService,
    ILogger<LabUserReportController> logger,
    S3StorageService s3Service) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly IWebHostEnvironment _env = env;
        private readonly LabAuthorizationService _labAuthorizationService = labAuthorizationService;
        private readonly EmailService _emailService = emailService;
        private readonly ILogger<LabUserReportController> _logger = logger;
        private readonly S3StorageService _s3Service = s3Service;



        // Method to Map Report Type
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


        // Method to Reverse Map the Report Type
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
        public async Task<IActionResult> UploadReports([FromForm] UserReportBatchUpload dto)
        {
            HttpContext.Items["Log-Category"] = "Lab Management";

            _logger.LogInformation("Received request to upload lab reports.");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                _logger.LogWarning("Invalid model state during report upload: {Errors}", string.Join(", ", errors));
                return BadRequest(ApiResponseFactory.Fail(errors));
            }

            try
            {
                var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
                {
                    _logger.LogWarning("Missing or invalid LabId claim during report upload.");
                    return Unauthorized(ApiResponseFactory.Fail("Invalid or missing LabId claim."));
                }

                if (!await _labAuthorizationService.IsLabAuthorized(labId, User))
                {
                    _logger.LogWarning("Unauthorized attempt to upload report by LabId: {LabId}", labId);
                    return Unauthorized(ApiResponseFactory.Fail("Permission denied. You can only create/modify/delete data for your main lab or its branches."));
                }

                if (dto.Entries == null || dto.Entries.Count == 0)
                {
                    _logger.LogWarning("No entries provided in the report upload payload.");
                    return BadRequest(ApiResponseFactory.Fail("No entries provided in the payload."));
                }

                string tempFolder = Path.Combine(_env.WebRootPath ?? "wwwroot", "temp-reports");
                if (!Directory.Exists(tempFolder)) Directory.CreateDirectory(tempFolder);

                var entryResults = new List<object>();
                int successfulUploads = 0;

                foreach (var entry in dto.Entries)
                {
                    _logger.LogInformation("Processing entry for HFID: {HFID}, Email: {Email}", entry.HFID, entry.Email);

                    if (entry.ReportFiles == null || entry.ReportTypes == null)
                    {
                        entryResults.Add(new { entry.HFID, entry.Email, Status = "Failed", Reason = "ReportFiles or ReportTypes missing" });
                        _logger.LogWarning("ReportFiles or ReportTypes missing for HFID: {HFID}", entry.HFID);
                        continue;
                    }

                    if (entry.ReportFiles.Count != entry.ReportTypes.Count)
                    {
                        entryResults.Add(new
                        {
                            entry.HFID,
                            entry.Email,
                            Status = "Failed",
                            Reason = $"Mismatch between files ({entry.ReportFiles.Count}) and report types ({entry.ReportTypes.Count})"
                        });
                        _logger.LogWarning("Mismatch between files and types for HFID: {HFID}", entry.HFID);
                        continue;
                    }

                    if (entry.ReportFiles.Count == 0)
                    {
                        entryResults.Add(new { entry.HFID, entry.Email, Status = "Failed", Reason = "No report files provided" });
                        _logger.LogWarning("No files provided for HFID: {HFID}", entry.HFID);
                        continue;
                    }

                    var userDetails = await _context.Set<UserDetails>()
                        .FirstOrDefaultAsync(u => u.user_membernumber == entry.HFID && u.user_email == entry.Email);

                    if (userDetails == null)
                    {
                        entryResults.Add(new { entry.HFID, entry.Email, Status = "Failed", Reason = "HFID and Email do not match any user" });
                        _logger.LogWarning("No matching user for HFID: {HFID} and Email: {Email}", entry.HFID, entry.Email);
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

                        var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now:dd-MM-yyyy_HH-mm-ss}{Path.GetExtension(file.FileName)}";
                        var tempFilePath = Path.Combine(tempFolder, fileName);

                        using (var stream = new FileStream(tempFilePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        var s3Key = $"reports/{fileName}";
                        var s3Url = await _s3Service.UploadFileToS3(tempFilePath, s3Key);

                        if (System.IO.File.Exists(tempFilePath)) System.IO.File.Delete(tempFilePath);

                        _logger.LogInformation("Saved file {FileName} for UserId: {UserId}", fileName, userId);

                        var userReport = new UserReports
                        {
                            UserId = userId,
                            ReportName = Path.GetFileNameWithoutExtension(file.FileName),
                            MemberId = "0",
                            ReportUrl = s3Url,
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
                        _logger.LogInformation("Uploaded {Count} files for HFID: {HFID}", uploadedFiles.Count, entry.HFID);
                    }
                    else
                    {
                        entryResults.Add(new { entry.HFID, entry.Email, Status = "Failed", Reason = "Valid user, but no report files were uploaded" });
                        _logger.LogWarning("Valid user {HFID}, but no files uploaded", entry.HFID);
                    }
                }

                if (successfulUploads == 0)
                {
                    _logger.LogWarning("No reports uploaded successfully. All entries failed.");
                    return BadRequest(ApiResponseFactory.Fail(entryResults, "No reports were uploaded. All entries failed."));
                }

                if (successfulUploads < dto.Entries.Count)
                {
                    _logger.LogInformation("Partial success: {SuccessCount} of {Total} entries uploaded successfully.", successfulUploads, dto.Entries.Count);
                    return StatusCode(202, ApiResponseFactory.PartialSuccess(entryResults, "Some reports uploaded successfully. Others failed."));
                }

                _logger.LogInformation("All reports uploaded successfully for LabId: {LabId}", labId);
                return Ok(ApiResponseFactory.Success(entryResults, "All reports uploaded successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during report upload.");
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }





        // Fetch All Reports of Selected User
        [HttpGet("labs/reports/{userId}")]
        public async Task<IActionResult> GetLabUserReportsByUserId([FromRoute] int userId, [FromQuery] string? reportType)
        {
            HttpContext.Items["Log-Category"] = "Lab Management";

            _logger.LogInformation("Received request to fetch lab user reports for UserId: {UserId}", userId);

            try
            {
                var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
                {
                    _logger.LogWarning("Invalid or missing LabId claim while fetching reports for UserId: {UserId}", userId);
                    return Unauthorized(ApiResponseFactory.Fail("Invalid or missing LabId claim."));
                }

                if (!await _labAuthorizationService.IsLabAuthorized(labId, User))
                {
                    _logger.LogWarning("Unauthorized access attempt by LabId {LabId} to fetch reports for UserId {UserId}", labId, userId);
                    return Unauthorized(ApiResponseFactory.Fail("Permission denied. You can only create/modify/delete data for your main lab or its branches."));
                }

                var currentLab = await _context.LabSignups.FirstOrDefaultAsync(lsu => lsu.Id == labId);
                if (currentLab == null)
                {
                    _logger.LogWarning("Lab with LabId {LabId} not found while fetching reports for UserId {UserId}", labId, userId);
                    return NotFound(ApiResponseFactory.Fail($"LabId {labId} not found."));
                }

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
                {
                    _logger.LogWarning("User details not found for UserId {UserId}", userId);
                    return NotFound(ApiResponseFactory.Fail($"User details not found for UserId {userId}."));
                }

                string fullName = $"{userDetails.user_firstname} {userDetails.user_lastname}".Trim();

                List<int> relatedLabIds;
                if (currentLab.LabReference == 0)
                {
                    relatedLabIds = await _context.LabSignups
                        .Where(lsu => lsu.LabReference == labId)
                        .Select(lsu => lsu.Id)
                        .ToListAsync();
                    relatedLabIds.Add(labId);
                }
                else
                {
                    relatedLabIds = await _context.LabSignups
                        .Where(lsu => lsu.LabReference == currentLab.LabReference)
                        .Select(lsu => lsu.Id)
                        .ToListAsync();
                    relatedLabIds.Add(currentLab.LabReference);
                }

                _logger.LogInformation("Related Lab IDs for LabId {LabId}: {RelatedLabIds}", labId, string.Join(",", relatedLabIds));

                var userReports = await _context.UserReports
                    .Where(ur => ur.UserId == userId && ur.LabId != null && relatedLabIds.Contains(ur.LabId.Value) && ur.UploadedBy == "Lab")
                    .ToListAsync();

                var labUserReportsDict = await _context.LabUserReports
                    .Where(lur => lur.UserId == userId && relatedLabIds.Contains(lur.LabId))
                    .ToDictionaryAsync(lur => lur.Id, lur => lur);

                if (userReports.Count == 0 && labUserReportsDict.Count == 0)
                {
                    _logger.LogWarning("No reports found for UserId {UserId} in related labs.", userId);
                    return NotFound(ApiResponseFactory.Fail($"No reports found for UserId {userId} in related labs."));
                }

                var allBranchIds = labUserReportsDict.Values.Select(l => l.BranchId).Distinct().ToList();
                var branchNamesDict = await _context.LabSignups
                    .Where(lsu => allBranchIds.Contains(lsu.Id))
                    .ToDictionaryAsync(lsu => lsu.Id, lsu => lsu.LabName);

                var allLabUserReportIds = userReports
                    .Where(ur => ur.LabUserReportId is int)
                    .Select(ur => ur.LabUserReportId!.Value)
                    .Distinct()
                    .ToList();

                var latestResendTimes = await _context.LabResendReports
                    .Where(r => allLabUserReportIds.Contains(r.LabUserReportId))
                    .GroupBy(r => r.LabUserReportId)
                    .Select(g => new
                    {
                        LabUserReportId = g.Key,
                        LatestResendEpochTime = g.Max(x => x.ResendEpochTime)
                    })
                    .ToDictionaryAsync(x => x.LabUserReportId, x => x.LatestResendEpochTime);

                long? firstSentEpoch = labUserReportsDict.Values.Min(l => l.EpochTime > 0 ? l.EpochTime : (long?)null);
                long? lastSentEpoch = labUserReportsDict.Values.Max(l => l.EpochTime > 0 ? l.EpochTime : (long?)null);

                string firstSentDate = firstSentEpoch.HasValue ? DateTimeOffset.FromUnixTimeSeconds(firstSentEpoch.Value).UtcDateTime.ToString("dd-MM-yyyy") : "No Reports";
                string lastSentDate = lastSentEpoch.HasValue ? DateTimeOffset.FromUnixTimeSeconds(lastSentEpoch.Value).UtcDateTime.ToString("dd-MM-yyyy") : "No Reports";

                var responseData = userReports.Select(userReport =>
                {
                    int labUserReportId = userReport.LabUserReportId ?? 0;
                    labUserReportsDict.TryGetValue(labUserReportId, out var matchedLabReport);
                    int branchId = matchedLabReport?.BranchId ?? 0;
                    long epochTime = matchedLabReport?.EpochTime ?? 0;
                    string createdDate = epochTime > 0 ? DateTimeOffset.FromUnixTimeSeconds(epochTime).UtcDateTime.ToString("dd-MM-yyyy") : "";
                    string branchName = branchNamesDict.TryGetValue(branchId, out string? value) ? value ?? "Unknown Branch" : currentLab.LabName ?? "Unknown Lab";
                    latestResendTimes.TryGetValue(labUserReportId, out long latestResendEpoch);
                    string resendDate = latestResendEpoch > 0 ? DateTimeOffset.FromUnixTimeSeconds(latestResendEpoch).UtcDateTime.ToString("dd-MM-yyyy") : "Not Resend";

                    return new
                    {
                        userReport.Id,
                        filename = userReport.ReportName,
                        fileURL = userReport.ReportUrl,
                        labName = currentLab.LabName,
                        reportType = ReverseReportTypeMapping(userReport.ReportId),
                        branchName,
                        epochTime,
                        createdDate,
                        LabUserReportId = labUserReportId,
                        resendDate
                    };
                })
                .Where(report => string.IsNullOrEmpty(reportType) || report.reportType == reportType)
                .ToList();

                var ReportCounts = responseData.Count;

                _logger.LogInformation("Successfully fetched {Count} report(s) for UserId {UserId}", ReportCounts, userId);

                return Ok(ApiResponseFactory.Success(new
                {
                    ReportCounts,
                    UserDetails = new
                    {
                        UserId = userId,
                        HFID = userDetails.user_membernumber,
                        FullName = fullName,
                        Email = userDetails.user_email,
                        UserImage = string.IsNullOrEmpty(userDetails.user_image) ? "No Image Available" : userDetails.user_image,
                        FirstSentReportDate = firstSentDate,
                        LastSentReportDate = lastSentDate
                    },
                    Reports = responseData,
                }, "Reports fetched successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while fetching reports for UserId {UserId}", userId);
                return StatusCode(500, ApiResponseFactory.Fail("An unexpected error occurred. Please contact support."));
            }
        }






        // Fetch All Distinct Users for All Dates (Currently we do not use this API in Frontend)
        [HttpGet("labs/reports/all")]
        public async Task<IActionResult> GetLabUserReports()
        {
            HttpContext.Items["Log-Category"] = "Lab Management";

            _logger.LogInformation("Received request to get lab user reports.");

            try
            {
                var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
                {
                    _logger.LogWarning("Missing or invalid LabId claim.");
                    return Unauthorized(ApiResponseFactory.Fail("Invalid or missing LabId claim."));
                }

                if (!await _labAuthorizationService.IsLabAuthorized(labId, User))
                {
                    _logger.LogWarning("Unauthorized access attempt by LabId: {LabId}", labId);
                    return Unauthorized(ApiResponseFactory.Fail("Permission denied. You can only access data for your main lab or its branches."));
                }

                _logger.LogInformation("Fetching latest lab user reports for LabId: {LabId}", labId);

                var latestReports = await _context.LabUserReports
                    .Where(lur => lur.LabId == labId)
                    .GroupBy(lur => lur.UserId)
                    .Select(group => group.OrderByDescending(lur => lur.EpochTime).First())
                    .ToListAsync();

                if (latestReports.Count == 0)
                {
                    _logger.LogInformation("No reports found for LabId: {LabId}", labId);
                    return NotFound(ApiResponseFactory.Fail($"No reports found for LabId {labId}."));
                }

                var userIds = latestReports.Select(lr => lr.UserId).ToList();

                _logger.LogInformation("Fetching user details for {Count} users.", userIds.Count);

                var userDetailsDict = await _context.Set<UserDetails>()
                    .Where(u => userIds.Contains(u.user_id))
                    .ToDictionaryAsync(
                        u => u.user_id,
                        u => new
                        {
                            HFID = u.user_membernumber,
                            Name = $"{u.user_firstname} {u.user_lastname}".Trim(),
                            UserId = u.user_id
                        });

                _logger.LogInformation("Fetching latest report types for users.");

                var reportIdsDict = await _context.UserReports
                    .Where(ur => userIds.Contains(ur.UserId) && ur.LabId == labId)
                    .GroupBy(ur => ur.UserId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        g.OrderByDescending(ur => ur.CreatedDate).First().ReportId
                    })
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

                _logger.LogInformation("Successfully fetched {Count} reports for LabId: {LabId}", responseData.Count, labId);

                return Ok(ApiResponseFactory.Success(responseData, "Reports fetched successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while fetching lab user reports.");
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }






        // Fetch All Distinct Users Based on Selection of Date
        [HttpGet("labs/{labId}/reports")]
        public async Task<IActionResult> GetLabUserReports([FromRoute] int labId, [FromQuery] string? startDate, [FromQuery] string? endDate)
        {
            HttpContext.Items["Log-Category"] = "Lab Management";

            _logger.LogInformation("Received request to get reports for LabId: {LabId}, StartDate: {StartDate}, EndDate: {EndDate}", labId, startDate, endDate);

            try
            {
                if (labId <= 0)
                {
                    _logger.LogWarning("Invalid LabId provided: {LabId}", labId);
                    return BadRequest(ApiResponseFactory.Fail("Invalid LabId."));
                }

                var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int loggedInLabId))
                {
                    _logger.LogWarning("Missing or invalid LabId claim.");
                    return Unauthorized(ApiResponseFactory.Fail("Invalid or missing LabId claim."));
                }

                if (!await _labAuthorizationService.IsLabAuthorized(labId, User))
                {
                    _logger.LogWarning("Unauthorized access attempt by LabId: {LoggedInLabId} for LabId: {TargetLabId}", loggedInLabId, labId);
                    return Unauthorized(ApiResponseFactory.Fail("Permission denied. You can only create/modify/delete data for your main lab or its branches."));
                }

                long startEpoch, endEpoch;
                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                {
                    if (!DateTime.TryParseExact(startDate, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var selectedStartDate) ||
                        !DateTime.TryParseExact(endDate, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var selectedEndDate))
                    {
                        _logger.LogWarning("Invalid date format. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
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

                    _logger.LogInformation("Default date range applied: {StartEpoch} to {EndEpoch}", startEpoch, endEpoch);
                }

                _logger.LogInformation("Fetching all reports for LabId: {LabId}", labId);

                var allReports = await _context.LabUserReports
                    .Where(lur => (lur.LabId == labId && lur.BranchId == 0) || lur.BranchId == labId)
                    .GroupBy(lur => lur.UserId)
                    .Select(g => g.OrderByDescending(r => r.EpochTime).First())
                    .ToListAsync();

                _logger.LogInformation("Filtering reports between {StartEpoch} and {EndEpoch}", startEpoch, endEpoch);

                var filteredReports = await _context.LabUserReports
                    .Where(lur => (lur.LabId == labId && lur.BranchId == 0) || lur.BranchId == labId)
                    .Where(lur => lur.EpochTime >= startEpoch && lur.EpochTime <= endEpoch)
                    .GroupBy(lur => lur.UserId)
                    .Select(g => g.OrderByDescending(r => r.EpochTime).First())
                    .ToListAsync();

                var PatientReports = allReports.Count;

                if (filteredReports.Count == 0)
                {
                    _logger.LogInformation("No reports found in given date range for LabId: {LabId}", labId);
                    return NotFound(ApiResponseFactory.Fail($"No reports found of past 48 hours."));
                }

                var userIds = filteredReports.Select(lr => lr.UserId).ToList();

                _logger.LogInformation("Fetching user details for {UserCount} users", userIds.Count);

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

                _logger.LogInformation("Returning {Count} filtered reports for LabId: {LabId}", responseData.Count, labId);

                var response = new { PatientReports, responseData };
                return Ok(ApiResponseFactory.Success(response, "Reports fetched successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while fetching reports for LabId: {LabId}", labId);
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }





        // Resend Reports using LabUserReportID
        [HttpPost("labs/reports/resend")]
        public async Task<IActionResult> ResendReport([FromBody] ResendReport dto)
        {
            HttpContext.Items["Log-Category"] = "Lab Management";
            _logger.LogInformation("ResendReport request received with {Count} IDs", dto?.Ids?.Count ?? 0);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                _logger.LogWarning("Model validation failed: {Errors}", string.Join(", ", errors));
                return BadRequest(ApiResponseFactory.Fail(errors));
            }

            try
            {
                var labIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int labId))
                {
                    _logger.LogWarning("Invalid or missing LabId claim.");
                    return Unauthorized(ApiResponseFactory.Fail("Invalid or missing LabId claim."));
                }

                if (!await _labAuthorizationService.IsLabAuthorized(labId, User))
                {
                    _logger.LogWarning("Unauthorized attempt to resend reports by LabId: {LabId}", labId);
                    return Unauthorized(ApiResponseFactory.Fail("Permission denied. You can only create/modify/delete data for your main lab or its branches."));
                }

                if (dto?.Ids == null || dto.Ids.Count == 0)
                {
                    _logger.LogWarning("No LabUserReport IDs provided for resending.");
                    return BadRequest(ApiResponseFactory.Fail("No LabUserReport IDs provided for resending."));
                }

                var loggedInLab = await _context.LabSignups.FirstOrDefaultAsync(l => l.Id == labId);
                if (loggedInLab == null)
                {
                    _logger.LogWarning("Lab not found for LabId: {LabId}", labId);
                    return BadRequest(ApiResponseFactory.Fail("Lab not found"));
                }

                int mainLabId = loggedInLab.LabReference == 0 ? labId : loggedInLab.LabReference;

                var branchIds = await _context.LabSignups
                    .Where(l => l.LabReference == mainLabId)
                    .Select(l => l.Id)
                    .ToListAsync();

                _logger.LogInformation("Resending reports from MainLabId: {MainLabId}, BranchIds: {BranchIds}", mainLabId, string.Join(",", branchIds));

                var successReports = new List<object>();
                var failedReports = new List<object>();
                var resendEntries = new List<LabResendReports>();

                var reportLogs = new List<NotificationResponse>();

                foreach (var labUserReportId in dto.Ids)
                {
                    var labUserReport = await _context.LabUserReports
                        .FirstOrDefaultAsync(lur => lur.Id == labUserReportId);

                    if (labUserReport == null || (!branchIds.Contains(labUserReport.LabId) && labUserReport.LabId != mainLabId))
                    {
                        _logger.LogWarning("LabUserReport {Id} failed validation. Not part of an authorized lab.", labUserReportId);

                        failedReports.Add(new
                        {
                            Id = labUserReportId,
                            Status = "Failed",
                            Reason = "LabUserReport entry not found or not part of an authorized branch."
                        });

                        reportLogs.Add(new NotificationResponse
                        {
                            Success = false,
                            Status = "Failed",
                            Reason = "LabUserReport entry not found or not part of an authorized branch."
                        });

                        continue;
                    }

                    var userReport = await _context.UserReports.FirstOrDefaultAsync(r => r.LabUserReportId == labUserReport.Id && r.UploadedBy == "Lab");

                    if (userReport == null)
                    {
                        _logger.LogWarning("User Reports failed validation. Not Found with Matching LabUserReportId {Id}.", labUserReportId);
                        return BadRequest(ApiResponseFactory.Fail("User Reports not found"));
                    }

                    long currentEpochTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    labUserReport.Resend += 1;
                    _context.LabUserReports.Update(labUserReport);

                    successReports.Add(new
                    {
                        Id = labUserReportId,
                        Status = "Success",
                        NewResendCount = labUserReport.Resend,
                        labUserReport.EpochTime
                    });

                    resendEntries.Add(new LabResendReports
                    {
                        LabUserReportId = labUserReportId,
                        ResendEpochTime = currentEpochTime
                    });

                    reportLogs.Add(new NotificationResponse
                    {
                        LabUserReportId = labUserReportId,
                        ResendReportName = userReport.ReportName,
                        ResendReportType = ReverseReportTypeMapping(userReport.ReportId),
                        NewResendCount = labUserReport.Resend,
                        Success = true,
                        Status = "Success",
                    });
                }

                await _context.LabResendReports.AddRangeAsync(resendEntries);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Resend operation complete. Success: {SuccessCount}, Failed: {FailedCount}", successReports.Count, failedReports.Count);

                HttpContext.Items["PerReportLogs"] = reportLogs;

                var result = new
                {
                    Success = successReports,
                    Failed = failedReports
                };

                if (failedReports.Count == 0)
                {
                    _logger.LogInformation("All reports resent successfully.");
                    return Ok(ApiResponseFactory.Success(result, "All reports resent successfully."));
                }

                if (successReports.Count == 0)
                {
                    _logger.LogWarning("All resend operations failed.");
                    return BadRequest(ApiResponseFactory.Fail(result, "All reports resend operations failed."));
                }

                _logger.LogInformation("Partial resend completed. Some reports succeeded, some failed.");
                return Ok(ApiResponseFactory.PartialSuccess(result, "Some reports were resent successfully, others failed."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during resend operation.");
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
            HttpContext.Items["Log-Category"] = "Lab Management";

            _logger.LogInformation("GetLabNotifications called for LabId: {LabId}, Timeframe: {Timeframe}, StartDate: {StartDate}, EndDate: {EndDate}", labId, timeframe, startDate, endDate);

            try
            {
                if (!await _labAuthorizationService.IsLabAuthorized(labId, User))
                {
                    _logger.LogWarning("Unauthorized access attempt for LabId: {LabId}", labId);
                    return Unauthorized(ApiResponseFactory.Fail("Permission denied. You can only create/modify/delete data for your main lab or its branches."));
                }

                var labAdminIdClaim = User.Claims.FirstOrDefault(c => c.Type == "LabAdminId");
                var roleClaim = User.Claims.FirstOrDefault(c => c.Type == "Role");

                if (labAdminIdClaim == null || !int.TryParse(labAdminIdClaim.Value, out int labAdminId))
                {
                    _logger.LogWarning("Missing or invalid LabAdminId claim.");
                    return Unauthorized(ApiResponseFactory.Fail("Invalid or missing LabAdminId claim."));
                }

                if (roleClaim == null)
                {
                    _logger.LogWarning("Missing Role claim for LabAdminId: {LabAdminId}", labAdminId);
                    return Unauthorized(ApiResponseFactory.Fail("Invalid or missing Role claim."));
                }

                string role = roleClaim.Value;
                int? userId = null;

                if (role == "Super Admin")
                {
                    userId = await _context.LabSuperAdmins
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
                {
                    _logger.LogWarning("UserId resolution failed for LabAdminId: {LabAdminId}, Role: {Role}", labAdminId, role);
                    return Unauthorized(ApiResponseFactory.Fail("Invalid access for the given role."));
                }

                long currentEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                long epochStart, epochEnd;

                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                {
                    if (!DateTimeOffset.TryParseExact(startDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDateParsed) ||
                        !DateTimeOffset.TryParseExact(endDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDateParsed))
                    {
                        _logger.LogWarning("Invalid date format received: StartDate={StartDate}, EndDate={EndDate}", startDate, endDate);
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
                {
                    _logger.LogInformation("No reports found for LabId: {LabId} in the given time range.", labId);
                    return NotFound(ApiResponseFactory.Fail("No reports found for the selected timeframe or date range."));
                }

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

                _logger.LogInformation("Notifications fetched successfully for LabId: {LabId}. Total: {Count}", labId, notifications.Count);
                return Ok(ApiResponseFactory.Success(notifications, "Notifications fetched successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred in GetLabNotifications for LabId: {LabId}", labId);
                return StatusCode(500, ApiResponseFactory.Fail($"An unexpected error occurred: {ex.Message}"));
            }
        }
    }
}

