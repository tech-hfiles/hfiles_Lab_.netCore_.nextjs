using HFiles_Backend.API.DTOs.Labs;
using HFiles_Backend.Domain.Entities.Labs;
using HFiles_Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HFiles_Backend.API.Controllers.Labs
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Ensure the user is authenticated
    public class LabUserReportController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public LabUserReportController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpPost("upload-batch")]
        public async Task<IActionResult> UploadReports([FromForm] LabUserReportBatchUploadDTO dto)
        {
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
                        MemberId = 0,
                        ReportUrl = fileName,
                        ReportId = reportTypeValue,
                        IsActive = false,
                        CreatedDate = DateTime.UtcNow,
                        AccessMappingId = null,
                        FileSize = Math.Round(file.Length / 1024.0, 2),
                        UploadType = uploadType,
                        NewIsActive = null,
                        UploadedBy = "Lab"
                    };

                    var labUserReport = new LabUserReports
                    {
                        UserId = userId,
                        LabId = labId, // ✅ NEW: LabId from token
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

        private int GetReportTypeValue(string? reportType)
        {
            return reportType?.ToLower() switch
            {
                "lab report" => 3,
                "dental report" => 4,
                "immunization" => 5,
                "medications" or "prescription" => 6,
                "radiology" => 7,
                "opthalmology" => 8,
                "special report" => 9,
                "invoices" or "mediclaim insurance" => 10,
                _ => 0
            };
        }
    }
}
