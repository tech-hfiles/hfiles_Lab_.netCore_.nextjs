using System.Data;
using System.Text;
using HFiles_Backend.API.DTOs.Labs;
using HFiles_Backend.Application.DTOs.Labs;
using HFiles_Backend.Domain.Entities.Labs;
using HFiles_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace HFiles_Backend.API.Middleware
{
    public class ApiLoggingMiddleware(RequestDelegate next, ILogger<ApiLoggingMiddleware> logger, IServiceProvider serviceProvider, IHttpContextAccessor httpContextAccessor)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<ApiLoggingMiddleware> _logger = logger;
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        // Request Body to fetch data
        private static async Task<string> GetRequestBody(HttpContext context)
        {
            try
            {
                context.Request.EnableBuffering();
                context.Request.Body.Position = 0;

                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                var body = await reader.ReadToEndAsync();

                context.Request.Body.Position = 0;

                return string.IsNullOrWhiteSpace(body) ? "No payload or request body was already read" : body;
            }
            catch (Exception ex)
            {
                return $"Error reading payload: {ex.Message}";
            }
        }


        // Response Body to fetch data
        //private static async Task<string> GetResponseBody(MemoryStream responseBodyStream)
        //{
        //    responseBodyStream.Seek(0, SeekOrigin.Begin);

        //    using var reader = new StreamReader(responseBodyStream, Encoding.UTF8);
        //    var body = await reader.ReadToEndAsync();

        //    responseBodyStream.Seek(0, SeekOrigin.Begin); 

        //    return string.IsNullOrWhiteSpace(body) ? "No response body or body already consumed" : body;
        //}





        public async Task Invoke(HttpContext context)
        {
            _logger.LogInformation("Processing API request for URL: {RequestUrl}, Method: {RequestMethod}, IP: {IpAddress}",
                context.Request.Path.Value ?? "Unknown",
                context.Request.Method ?? "Unknown",
                context.Connection.RemoteIpAddress?.ToString() ?? "Unknown");

            int? labId = 0, userId = 0, branchId = 0;
            string? role = "Not Assigned Yet", sessionId = "Not Generated Yet";
            string requestUrl = context.Request.Path.Value ?? "Unknown";
            string requestMethod = context.Request.Method ?? "Unknown";
            string ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "Unknown";

            _logger.LogInformation("Incoming request from IP: {IP}", ipAddress);




            context.Request.EnableBuffering();
            string requestBody = await GetRequestBody(context);

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();


            // Runs Controller logic first, then executes this Middleware logic
            await _next(context);





            // Fetch LabId, UserId and ROle from User Login API
            if (context.Request.Path.Value?.Contains("labs/users/login", StringComparison.OrdinalIgnoreCase) == true)
            {
                _logger.LogInformation("User Login API detected. Extracting user details from request body.");

                try
                {
                    var loginDto = JsonConvert.DeserializeObject<UserLogin>(requestBody);
                    if (loginDto != null)
                    {
                        labId = loginDto.UserId;
                        role = loginDto.Role;
                        sessionId = "Not Generated Yet";

                        var userDetails = await dbContext.UserDetails
                            .FirstOrDefaultAsync(u => u.user_membernumber == loginDto.HFID);

                        if (userDetails != null)
                        {
                            var member = await dbContext.LabMembers.FirstOrDefaultAsync(m => m.UserId == userDetails.user_id && m.LabId == labId && m.DeletedBy == 0);
                            var admin = await dbContext.LabSuperAdmins.FirstOrDefaultAsync(a => a.UserId == userDetails.user_id && (a.LabId == labId || a.LabId == labId) && a.IsMain == 1);

                            userId = member?.Id ?? admin?.Id ?? 0;
                        }

                        _logger.LogInformation("Login user details extracted successfully. LabId: {LabId}, UserId: {UserId}, Role: {Role}, SessionId: {SessionId}",
                            labId, userId, role, sessionId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract user details from login request.");
                }
            }






            // Fetch LabId from Login with Password API
            else if (context.Request.Path.Value?.Contains("labs/login/password", StringComparison.OrdinalIgnoreCase) == true)
            {
                _logger.LogInformation("Lab Login with Password API detected. Extracting lab details from request body.");

                try
                {
                    var loginPasswordDto = JsonConvert.DeserializeObject<PasswordLogin>(requestBody);
                    if (loginPasswordDto != null)
                    {
                        var lab = await dbContext.LabSignups.FirstOrDefaultAsync(l => l.Email == loginPasswordDto.Email);
                        if (lab != null)
                        {
                            labId = lab.Id;
                            role = "Not Assigned Yet";
                            sessionId = "Not Generated Yet";
                        }

                        _logger.LogInformation("Lab login with password details extracted succesfully. LabId:  {LabId}, UserId: {UserId}, Role: {Role}, SessionId: {SessionId}",
                            labId, userId, role, sessionId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract lab login with password details from login request.");
                }
            }





            // Fetch LabId from Login with Otp API
            else if (context.Request.Path.Value?.Contains("labs/login/otp", StringComparison.OrdinalIgnoreCase) == true)
            {
                _logger.LogInformation("Lab Login with OTP API detected. Extracting lab details from request body.");

                try
                {
                    var loginOtpDto = JsonConvert.DeserializeObject<OtpLogin>(requestBody);
                    if (loginOtpDto != null)
                    {
                        var lab = await dbContext.LabSignups.FirstOrDefaultAsync(l => l.Email == loginOtpDto.Email);
                        if (lab != null)
                        {
                            labId = lab.Id;
                            role = "Not Assigned Yet";
                            sessionId = "Not Generated Yet";
                        }

                        _logger.LogInformation("Lab login with OTP details extracted succesfully. LabId:  {LabId}, UserId: {UserId}, Role: {Role}, SessionId: {SessionId}",
                            labId, userId, role, sessionId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract lab login with OTP details from login request.");
                }
            }





            // Fetch LabId from Sending Otp to Lab Email API
            else if (context.Request.Path.Value?.Contains("labs/otp", StringComparison.OrdinalIgnoreCase) == true)
            {
                _logger.LogInformation("Sending Otp for Login to Lab's email API detected. Extracting lab details from request body.");

                try
                {
                    var labLoginDto = JsonConvert.DeserializeObject<LoginOtpRequest>(requestBody);
                    if (labLoginDto != null)
                    {
                        var lab = await dbContext.LabSignups.FirstOrDefaultAsync(l => l.Email == labLoginDto.Email);
                        if (lab != null)
                        {
                            labId = lab.Id;
                            role = "Not Assigned Yet";
                            sessionId = "Not Generated Yet";
                        }

                        _logger.LogInformation("Sending Otp for Login to Lab's email extracted succesfully. LabId:  {LabId}, UserId: {UserId}, Role: {Role}, SessionId: {SessionId}",
                            labId, userId, role, sessionId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract sending Otp for Login to Lab's email details from login request.");
                }
            }





            // Fetch LabId, UserId and Role from Create Super Admin API
            else if (context.Request.Path.Value?.Contains("labs/super-admins", StringComparison.OrdinalIgnoreCase) == true)
            {
                _logger.LogInformation("Creating Lab Super Admin API detected. Extracting user details from request body.");

                try
                {
                    var superAdminDto = JsonConvert.DeserializeObject<CreateSuperAdmin>(requestBody);
                    if (superAdminDto != null)
                    {
                        labId = superAdminDto.UserId;
                        role = superAdminDto.Role;
                        sessionId = "Not Generated Yet";

                        var userDetails = await dbContext.UserDetails
                            .FirstOrDefaultAsync(u => u.user_membernumber == superAdminDto.HFID);

                        if (userDetails != null)
                        {
                            var member = await dbContext.LabMembers.FirstOrDefaultAsync(m => m.UserId == userDetails.user_id && m.LabId == labId && m.DeletedBy == 0);
                            var admin = await dbContext.LabSuperAdmins.FirstOrDefaultAsync(a => a.UserId == userDetails.user_id && (a.LabId == labId || a.LabId == labId) && a.IsMain == 1);

                            userId = member?.Id ?? admin?.Id ?? 0;
                        }

                        _logger.LogInformation("Creating Lab Super Admin details extracted successfully. LabId: {LabId}, UserId: {UserId}, Role: {Role}, SessionId: {SessionId}",
                            labId, userId, role, sessionId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract Lab Super Admin details from login request.");
                }
            }





            // Fetch Branch Id from Promote Admin API
            else if (context.Request.Path.Value?.Contains("labs/admin/promote", StringComparison.OrdinalIgnoreCase) == true)
            {
                _logger.LogInformation("Admin Promotion API detected. Extracting Branch details from request body.");

                try
                {
                    var promoteAdminDto = JsonConvert.DeserializeObject<PromoteAdmin>(requestBody);

                    if (promoteAdminDto != null)
                    {
                        var user = context.User;
                        var labIdClaim = user.Claims.FirstOrDefault(c => c.Type == "UserId");
                        var labAdminIdClaim = user.Claims.FirstOrDefault(c => c.Type == "LabAdminId");
                        labId = labIdClaim != null && int.TryParse(labIdClaim.Value, out int parsedLabId) ? parsedLabId : 0;
                        userId = labAdminIdClaim != null && int.TryParse(labAdminIdClaim.Value, out int parsedUserId) ? parsedUserId : 0;
                        role = user.Claims.FirstOrDefault(c => c.Type == "Role")?.Value ?? "Not Assigned Yet";
                        sessionId = user.Claims.FirstOrDefault(c => c.Type == "SessionId")?.Value ?? "Not Generated Yet";

                        var loggedInLab = await dbContext.LabSignups.FirstOrDefaultAsync(l => l.Id == labId);

                        int mainLabId = (int)(loggedInLab!.LabReference == 0 ? labId : loggedInLab.LabReference);

                        var branchIds = await dbContext.LabSignups
                            .Where(l => l.LabReference == mainLabId)
                            .Select(l => l.Id)
                            .ToListAsync();

                        branchIds.Add(mainLabId);

                        var member = await dbContext.LabMembers.FirstOrDefaultAsync(m =>
                            m.Id == promoteAdminDto.MemberId && m.DeletedBy == 0 && (branchIds.Contains(m.LabId) || m.LabId == mainLabId));

                        if (member != null && member.LabId != labId)
                        {
                            branchId = member.LabId;
                        }

                        else
                        {
                            branchId = 0;
                        }
                        _logger.LogInformation("Admin Promotion details extracted successfully. LabId: {LabId}, UserId: {UserId}, Role: {Role}, SessionId: {SessionId}, Branch: {BranchId}",
                           labId, userId, role, sessionId, branchId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract Branch details from Admin Promotion request.");
                }
            }





            // Extract BranchId from Update Lab API
            else if (context.Request.Path.Value?.Contains("labs/update", StringComparison.OrdinalIgnoreCase) == true)
            {
                _logger.LogInformation("Updating Lab API detected. Extracting lab details from request.");

                try
                {
                    var user = context.User;

                    labId = int.TryParse(user.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value, out int parsedLabId) ? parsedLabId : 0;
                    userId = int.TryParse(user.Claims.FirstOrDefault(c => c.Type == "LabAdminId")?.Value, out int parsedUserId) ? parsedUserId : 0;
                    role = user.Claims.FirstOrDefault(c => c.Type == "Role")?.Value ?? "Not Assigned Yet";
                    sessionId = user.Claims.FirstOrDefault(c => c.Type == "SessionId")?.Value ?? "Not Generated Yet";

                    context.Request.EnableBuffering();

                    var form = await context.Request.ReadFormAsync();

                    var file = form.Files.GetFile("ProfilePhoto");
                    string? profilePhotoFileName = file?.FileName;

                    var updateLabDto = new ProfileUpdate
                    {
                        Id = int.TryParse(form["Id"], out var parsedId) ? parsedId : 0,
                        Address = form["Address"]
                    };

                    var lab = await dbContext.LabSignups.FirstOrDefaultAsync(l => l.Id == updateLabDto.Id && l.DeletedBy == 0);
                    if (lab != null && labId != lab.Id)
                    {
                        branchId = lab.Id;
                        _logger.LogInformation("Middleware: Branch updated successfully with ID: {branchId}", branchId);
                    }
                    else
                    {
                        branchId = 0;
                        _logger.LogWarning("Middleware: Branch ID not found or invalid.");
                    }

                    _logger.LogInformation("Update Lab details extracted. LabId: {LabId}, UserId: {UserId}, Role: {Role}, SessionId: {SessionId}, BranchId: {BranchId}, ProfilePhoto: {ProfilePhoto}",
                        labId, userId, role, sessionId, branchId, profilePhotoFileName ?? "No file uploaded");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract Lab details from Update Lab request.");
                }
            }





            // Extract Branch Id from Delete Branch API
            else if (context.Request.Path.Value?.StartsWith("/api/labs/branches/", StringComparison.OrdinalIgnoreCase) == true)
            {
                _logger.LogInformation("Branch Delete API detected. Extracting Branch details.");

                try
                {
                    var user = context.User;

                    labId = int.TryParse(user.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value, out int parsedLabId) ? parsedLabId : null;
                    userId = int.TryParse(user.Claims.FirstOrDefault(c => c.Type == "LabAdminId")?.Value, out int parsedUserId) ? parsedUserId : null;
                    role = user.Claims.FirstOrDefault(c => c.Type == "Role")?.Value ?? "Not Assigned Yet";
                    sessionId = user.Claims.FirstOrDefault(c => c.Type == "SessionId")?.Value ?? "Not Generated Yet";

                    var routeData = context.GetRouteData();
                    if (routeData.Values.TryGetValue("branchId", out var branchIdValue) && int.TryParse(branchIdValue?.ToString(), out int parsedBranchId))
                    {
                        branchId = parsedBranchId;
                        _logger.LogInformation("Extracted Branch ID from route: {BranchId}", branchId);
                    }
                    else
                    {
                        _logger.LogWarning("Could not extract Branch ID from route. Falling back to path parsing.");
                        var path = context.Request.Path.Value;
                        var segments = path?.Split('/', StringSplitOptions.RemoveEmptyEntries);
                        if (segments?.Length >= 4 && int.TryParse(segments[3], out parsedBranchId))
                        {
                            branchId = parsedBranchId;
                            _logger.LogInformation("Extracted Branch ID from path: {BranchId}", branchId);
                        }
                        else
                        {
                            _logger.LogWarning("Still could not extract Branch ID from path: {Path}", path);
                            branchId = 0;
                        }
                    }

                    _logger.LogInformation("Branch Delete details extracted. LabId: {LabId}, UserId: {UserId}, Role: {Role}, SessionId: {SessionId}, Branch: {BranchId}",
                        labId, userId, role, sessionId, branchId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error extracting Branch details from Delete Branch request.");
                }
            }





            // Fetch BranchId from Delete Member API
            else if (context.Request.Path.Value?.StartsWith("/api/labs/members/", StringComparison.OrdinalIgnoreCase) == true)
            {
                _logger.LogInformation("Member Delete API detected. Extracting Member details.");

                try
                {
                    var user = context.User;

                    labId = int.TryParse(user.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value, out int parsedLabId) ? parsedLabId : 0;
                    userId = int.TryParse(user.Claims.FirstOrDefault(c => c.Type == "LabAdminId")?.Value, out int parsedUserId) ? parsedUserId : 0;
                    role = user.Claims.FirstOrDefault(c => c.Type == "Role")?.Value ?? "Not Assigned Yet";
                    sessionId = user.Claims.FirstOrDefault(c => c.Type == "SessionId")?.Value ?? "Not Generated Yet";

                    int memberId = 0;

                    if (context.Items.TryGetValue("MemberId", out var memberIdObj) && int.TryParse(memberIdObj?.ToString(), out memberId))
                    {
                        _logger.LogInformation("Extracted Member ID from HttpContext.Items: {MemberId}", memberId);
                    }
                    else
                    {
                        var routeData = context.GetRouteData();
                        if (routeData?.Values.TryGetValue("memberId", out var memberIdValue) == true && int.TryParse(memberIdValue?.ToString(), out memberId))
                        {
                            _logger.LogInformation("Extracted Member ID from route: {MemberId}", memberId);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to extract Member ID. Defaulting Branch ID to 0.");
                            branchId = 0;
                        }
                    }

                    if (memberId > 0)
                    {

                        var member = await dbContext.LabMembers.AsNoTracking()
                            .FirstOrDefaultAsync(m => m.Id == memberId && m.DeletedBy != 0);

                        if (member is not null)
                        {
                            branchId = member.LabId != labId ? member.LabId : 0;
                            _logger.LogInformation("Mapped Lab ID {LabId} to Branch ID {BranchId} based on Member ID {MemberId}.", member.LabId, branchId, memberId);
                        }
                        else
                        {
                            _logger.LogWarning("Member ID {MemberId} not found or already deleted. Defaulting Branch ID to 0.", memberId);
                            branchId = 0;
                        }
                    }

                    branchId = branchId.HasValue && double.IsNaN(branchId.Value) ? 0 : branchId.GetValueOrDefault(0);

                    _logger.LogInformation("Member deletion details extracted. LabId: {LabId}, UserId: {UserId}, Role: {Role}, SessionId: {SessionId}, BranchId: {BranchId}",
                        labId, userId, role, sessionId, branchId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error extracting Member details from Delete Member request.");
                }
            }





            // Extract Lab Id from Sending Email during Password Reset for Labs
            else if (context.Request.Path.Value?.Contains("labs/password-reset/request", StringComparison.OrdinalIgnoreCase) == true)
            {
                _logger.LogInformation("Request Lab Password Reset API detected. Extracting lab details from request body.");
                try
                {
                    var resetPasswordRequestDto = JsonConvert.DeserializeObject<PasswordResetRequest>(requestBody);
                    if (resetPasswordRequestDto != null)
                    {
                        var lab = dbContext.LabSignups.FirstOrDefault(l => l.Email == resetPasswordRequestDto.Email && l.DeletedBy == 0);
                        if (lab != null)
                        {
                            labId = lab.Id;
                            _logger.LogInformation("Middleware: Request Lab Password Reset created successfully with ID: {labId}", labId);
                        }
                        else
                        {
                            labId = 0;
                            _logger.LogWarning("Middleware: Lab ID not found or invalid.");
                        }
                        _logger.LogInformation("Request Lab Password Reset details extracted. LabId: {LabId}, UserId: {UserId}, Role: {Role}, SessionId: {SessionId}, BranchId: {BranchId}",
                        labId, userId, role, sessionId, branchId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract Lab details from Request Lab Password Reset request.");
                }
            }





            // Extract LabId from Lab Reset Password API
            else if (context.Request.Path.Value?.Contains("labs/password-reset", StringComparison.OrdinalIgnoreCase) == true)
            {
                _logger.LogInformation("Lab Password Reset API detected. Extracting lab details from request body.");
                try
                {
                    var resetPasswordDto = JsonConvert.DeserializeObject<PasswordReset>(requestBody);
                    if (resetPasswordDto != null)
                    {
                        var lab = dbContext.LabSignups.FirstOrDefault(l => l.Email == resetPasswordDto.Email && l.DeletedBy == 0);
                        if (lab != null)
                        {
                            labId = lab.Id;
                            _logger.LogInformation("Middleware: Lab Password Reset created successfully with ID: {labId}", labId);
                        }
                        else
                        {
                            labId = 0;
                            _logger.LogWarning("Middleware: Lab ID not found or invalid.");
                        }
                        _logger.LogInformation("Lab Password Reset details extracted. LabId: {LabId}, UserId: {UserId}, Role: {Role}, SessionId: {SessionId}, BranchId: {BranchId}",
                        labId, userId, role, sessionId, branchId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract Lab details from Lab Password Reset request.");
                }
            }





            // Extract Lab Id, UserId and Role from Sending Email during Password Reset for Lab Users
            else if (context.Request.Path.Value?.Contains("labs/users/password-reset/request", StringComparison.OrdinalIgnoreCase) == true)
            {
                _logger.LogInformation("Request Lab User Password Reset API detected. Extracting lab details from request body.");
                try
                {
                    var userResetPasswordRequestDto = JsonConvert.DeserializeObject<UserPasswordResetRequest>(requestBody);
                    if (userResetPasswordRequestDto != null)
                    {
                        var lab = await dbContext.LabSignups
                            .FirstOrDefaultAsync(l => l.Id == userResetPasswordRequestDto.LabId && l.DeletedBy == 0);

                        var userDetails = await dbContext.UserDetails
                            .FirstOrDefaultAsync(u => u.user_email == userResetPasswordRequestDto.Email);

                        if (userDetails == null)
                        {
                            _logger.LogWarning("User not found for email: {Email}", userResetPasswordRequestDto.Email);
                            return;
                        }

                        int UserId = userDetails.user_id;

                        var superAdmin = await dbContext.LabSuperAdmins
                            .FirstOrDefaultAsync(a => a.UserId == UserId && a.IsMain == 1 && a.LabId == userResetPasswordRequestDto.LabId);

                        if (superAdmin != null && lab != null)
                        {
                            labId = lab.Id;
                            userId = superAdmin.Id;
                            role = "Super Admin";
                        }
                        else
                        {
                            var labMember = await dbContext.LabMembers
                                .FirstOrDefaultAsync(m => m.UserId == UserId && m.DeletedBy == 0 && m.LabId == userResetPasswordRequestDto.LabId);

                            if (labMember != null && lab != null)
                            {
                                labId = lab.Id;
                                userId = labMember.Id;
                                role = labMember.Role;
                            }
                            else
                            {
                                _logger.LogWarning("No matching lab member or super admin found for User ID {UserId} and Lab ID {LabId}.", userId, userResetPasswordRequestDto.LabId);
                            }
                        }

                        _logger.LogInformation("Extracted Reset Request Context: LabId={LabId}, UserId={UserId}, Role={Role}, SessionId={SessionId}, BranchId={BranchId}",
                            labId, userId, role, sessionId, branchId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract Lab details from Request Lab User Password Reset request.");
                }
            }





            // Extract LabId from Lab User Reset Password API
            if (context.Request.Path.Value?.Contains("labs/users/password-reset", StringComparison.OrdinalIgnoreCase) == true)
            {
                _logger.LogInformation("Reset Password API detected. Extracting lab user context from request body.");

                try
                {

                    var passwordResetDto = JsonConvert.DeserializeObject<UserPasswordReset>(requestBody);
                    if (passwordResetDto == null)
                    {
                        _logger.LogWarning("Failed to deserialize password reset DTO.");
                        return;
                    }

                    var lab = await dbContext.LabSignups
                        .FirstOrDefaultAsync(l => l.Id == passwordResetDto.LabId && l.DeletedBy == 0);

                    var userDetails = await dbContext.UserDetails
                        .FirstOrDefaultAsync(u => u.user_email == passwordResetDto.Email);

                    if (userDetails == null)
                    {
                        _logger.LogWarning("User not found for email: {Email}", passwordResetDto.Email);
                        return;
                    }

                    int UserId = userDetails.user_id;

                    var superAdmin = await dbContext.LabSuperAdmins
                        .FirstOrDefaultAsync(a => a.UserId == UserId && a.IsMain == 1 && a.LabId == passwordResetDto.LabId);

                    if (superAdmin != null && lab != null)
                    {
                        userId = superAdmin.Id;
                        role = "Super Admin";
                    }
                    else
                    {
                        var labMember = await dbContext.LabMembers
                            .FirstOrDefaultAsync(m => m.UserId == UserId && m.DeletedBy == 0 && m.LabId == passwordResetDto.LabId);

                        if (labMember != null && lab != null)
                        {
                            labId = lab.Id;
                            userId = labMember.Id;
                            role = labMember.Role ?? "Member";
                        }
                    }

                    _logger.LogInformation("Extracted Reset Request Context for Lab Users: LabId={LabId}, UserId={UserId}, Role={Role}, SessionId={SessionId}, BranchId={BranchId}",
                            labId, userId, role, sessionId, branchId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract context from Password Reset request.");
                }
            }





            // Extract Branch Id from Create Branch API
            else if (context.Request.Path.Value?.Contains("labs/branches", StringComparison.OrdinalIgnoreCase) == true)
            {
                _logger.LogInformation("Branch Create API detected. Extracting Branch details.");
                try
                {
                    var user = context.User;

                    labId = int.TryParse(user.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value, out int parsedLabId) ? parsedLabId : null;
                    userId = int.TryParse(user.Claims.FirstOrDefault(c => c.Type == "LabAdminId")?.Value, out int parsedUserId) ? parsedUserId : null;
                    role = user.Claims.FirstOrDefault(c => c.Type == "Role")?.Value ?? "Not Assigned Yet";
                    sessionId = user.Claims.FirstOrDefault(c => c.Type == "SessionId")?.Value ?? "Not Generated Yet";

                    _logger.LogInformation("Checking HttpContext.Items before extraction: {@Items}", context.Items);
                    if (context.Items.TryGetValue("CreatedBranchId", out var createdBranchIdObj) && createdBranchIdObj is int createdBranchId)
                    {
                        branchId = createdBranchId;
                        _logger.LogInformation("Middleware: Branch created successfully with ID: {BranchId}", branchId);
                    }
                    else
                    {
                        branchId = 0;
                        _logger.LogWarning("Middleware: Branch ID not found or invalid.");
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error extracting Branch details from Create Branch request.");
                }
            }





            // Extract Branch Id from Create Member API
            else if (context.Request.Path.Value?.Contains("labs/members", StringComparison.OrdinalIgnoreCase) == true)
            {
                _logger.LogInformation("Creating Lab Member API detected. Extracting member details from request body.");

                try
                {
                    var user = context.User;

                    labId = int.TryParse(user.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value, out int parsedLabId) ? parsedLabId : null;
                    userId = int.TryParse(user.Claims.FirstOrDefault(c => c.Type == "LabAdminId")?.Value, out int parsedUserId) ? parsedUserId : null;
                    role = user.Claims.FirstOrDefault(c => c.Type == "Role")?.Value ?? "Not Assigned Yet";
                    sessionId = user.Claims.FirstOrDefault(c => c.Type == "SessionId")?.Value ?? "Not Generated Yet";

                    _logger.LogInformation("Raw Request Body: {RequestBody}", requestBody);

                    var memberDto = JsonConvert.DeserializeObject<CreateMember>(requestBody);

                    if (memberDto != null)
                    {
                        _logger.LogInformation("Extracted Branch ID from request body: {BranchId}", memberDto.BranchId);

                        if (memberDto.BranchId != labId)
                        {
                            branchId = memberDto.BranchId;
                        }
                        else
                        {
                            branchId = 0;
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Failed to parse request body into CreateMember DTO.");
                    }

                    _logger.LogInformation("Create Member details extracted. LabId: {LabId}, UserId: {UserId}, Role: {Role}, SessionId: {SessionId}, BranchId: {BranchId}",
                        labId, userId, role, sessionId, branchId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract Member details from Create Member request.");
                }
            }





            // Extract LabId from Create Lab API
            else if (context.Request.Path.Value?.Equals("labs", StringComparison.OrdinalIgnoreCase) == true)
            {
                _logger.LogInformation("Creating Lab API detected. Extracting lab details from request body.");
                try
                {
                    _logger.LogInformation("Checking HttpContext.Items before extraction: {@Items}", context.Items);
                    if (context.Items.TryGetValue("CreatedLabId", out var createdLabIdObj) && createdLabIdObj is int createdLabId)
                    {
                        labId = createdLabId;
                        _logger.LogInformation("Middleware: Lab created successfully with ID: {labId}", labId);
                    }
                    else
                    {
                        labId = 0;
                        _logger.LogWarning("Middleware: Lab ID not found or invalid.");
                    }

                    _logger.LogInformation("Create Lab details extracted. LabId: {LabId}, UserId: {UserId}, Role: {Role}, SessionId: {SessionId}, BranchId: {BranchId}",
                        labId, userId, role, sessionId, branchId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract Lab details from Create Lab request.");
                }
            }




            // By Default : Extract LabId, UserId, Role and SessionId from Token
            else
            {
                var user = context.User;
                var labAdminIdClaim = user.Claims.FirstOrDefault(c => c.Type == "LabAdminId");
                var labIdClaim = user.Claims.FirstOrDefault(c => c.Type == "UserId");

                labId = labIdClaim != null && int.TryParse(labIdClaim.Value, out int parsedLabId) ? parsedLabId : 0;
                userId = labAdminIdClaim != null && int.TryParse(labAdminIdClaim.Value, out int parsedUserId) ? parsedUserId : 0;
                role = user.Claims.FirstOrDefault(c => c.Type == "Role")?.Value ?? "Not Assigned Yet";
                sessionId = user.Claims.FirstOrDefault(c => c.Type == "SessionId")?.Value ?? "Not Generated Yet";
                _logger.LogInformation("Extracted user details from claims - LabId: {LabId}, UserId: {UserId}, Role: {Role}, SessionId: {SessionId}",
                    labId, userId, role, sessionId);
            }

            var allowedStatusCodes = new[] { StatusCodes.Status200OK, StatusCodes.Status202Accepted };
            if (allowedStatusCodes.Contains(context.Response.StatusCode) && context.Request.Method != HttpMethods.Get)
            {
                _logger.LogInformation("API Call Logged Successfully - URL: {RequestUrl}, Method: {RequestMethod}, Status: {StatusCode}",
                    requestUrl, requestMethod, context.Response.StatusCode);


                // Logging if promote admin endpoint is hit
                if (context.Request?.Path.Value?.Contains("labs/members/promote", StringComparison.OrdinalIgnoreCase) == true)
                {
                    _logger.LogInformation("Intercepted Promote Members API for audit logging.");
                    PromoteMembersRequest? promoteDto = null;

                    try
                    {
                        promoteDto = JsonConvert.DeserializeObject<PromoteMembersRequest>(requestBody);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize PromoteMembersRequest for logging.");
                    }

                    if (promoteDto != null && promoteDto.Ids.Any())
                    {
                        var logs = new List<LabAuditLog>();

                        var members = await dbContext.LabMembers
                            .Where(m => promoteDto.Ids.Contains(m.Id))
                            .ToListAsync();

                        foreach (var member in members)
                        {
                            int assignedBranchId = (member.LabId != labId) ? member.LabId : 0;

                            logs.Add(new LabAuditLog
                            {
                                EntityName = context.Request.RouteValues["controller"]?.ToString() ?? "Unknown",
                                LabId = labId,
                                UserId = userId,
                                UserRole = role,
                                BranchId = assignedBranchId,
                                Details = $"Request to {requestUrl} with method {requestMethod}. Promoted Member ID: {member.Id}",
                                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                                IpAddress = ipAddress,
                                SessionId = sessionId,
                                Url = requestUrl,
                                HttpMethod = requestMethod,
                                Category = context.Items.ContainsKey("Log-Category") ? context.Items["Log-Category"]?.ToString() ?? "General" : "General"
                            });
                        }

                        await dbContext.LabAuditLogs.AddRangeAsync(logs);
                        await dbContext.SaveChangesAsync();

                        _logger.LogInformation("API Call Logged Successfully - {Count} Member(s) promoted. URL: {RequestUrl}, Method: {RequestMethod}",
                            logs.Count, requestUrl, requestMethod);
                        return;
                    }
                    else
                    {
                        _logger.LogWarning("No valid Member IDs found in request body for promote logging.");
                    }
                }

                // Logging if Upload Report API is hit
                if (context.Request?.Path.Value?.Contains("labs/reports/upload", StringComparison.OrdinalIgnoreCase) == true)
                {
                    _logger.LogInformation("Intercepted UploadReports API for audit logging.");

                    try
                    {
                        if (!context.Request.HasFormContentType)
                        {
                            _logger.LogWarning("Request does not have form content type. Skipping audit log.");
                            return;
                        }

                        var form = await context.Request.ReadFormAsync();
                        var entries = new Dictionary<int, UserReportUpload>();

                        foreach (var key in form.Keys)
                        {
                            var match = System.Text.RegularExpressions.Regex.Match(key, @"Entries\[(\d+)\]\.(.+)");
                            if (!match.Success) continue;

                            int index = int.Parse(match.Groups[1].Value);
                            string property = match.Groups[2].Value;

                            if (!entries.ContainsKey(index))
                                entries[index] = new UserReportUpload
                                {
                                    ReportTypes = new List<string>(),
                                    ReportFiles = new List<IFormFile>()
                                };

                            var value = form[key];

                            switch (property)
                            {
                                case "HFID":
                                    entries[index].HFID = value!;
                                    break;
                                case "Email":
                                    entries[index].Email = value!;
                                    break;
                                case "Name":
                                    entries[index].Name = value!;
                                    break;
                                case "BranchId":
                                    if (int.TryParse(value, out int reportBranchId))
                                        entries[index].BranchId = reportBranchId;
                                    break;
                                case "ReportTypes":
                                    entries[index].ReportTypes.Add(value!);
                                    break;
                            }
                        }

                        foreach (var file in form.Files)
                        {
                            var match = System.Text.RegularExpressions.Regex.Match(file.Name, @"Entries\[(\d+)\]\.ReportFiles");
                            if (!match.Success) continue;

                            int index = int.Parse(match.Groups[1].Value);

                            if (!entries.ContainsKey(index))
                            {
                                entries[index] = new UserReportUpload
                                {
                                    ReportTypes = new List<string>(),
                                    ReportFiles = new List<IFormFile>()
                                };
                            }

                            entries[index].ReportFiles.Add(file);
                        }

                        var logs = new List<LabAuditLog>();

                        foreach (var entry in entries.Values)
                        {
                            logs.Add(new LabAuditLog
                            {
                                EntityName = context.Request.RouteValues["controller"]?.ToString() ?? "Unknown",
                                LabId = labId,
                                UserId = userId,
                                UserRole = role,
                                BranchId = entry.BranchId,
                                Details = $"Batch report upload initiated. HFID: {entry.HFID}, Email: {entry.Email}, Name: {entry.Name}, BranchId - {entry.BranchId}. Payload: {JsonConvert.SerializeObject(entry)}",
                                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                                IpAddress = ipAddress,
                                SessionId = sessionId,
                                Url = requestUrl,
                                HttpMethod = requestMethod,
                                Category = context.Items.ContainsKey("Log-Category") ? context.Items["Log-Category"]?.ToString() ?? "General" : "General"
                            });
                        }

                        if (logs.Count > 0)
                        {
                            await dbContext.LabAuditLogs.AddRangeAsync(logs);
                            await dbContext.SaveChangesAsync();
                            _logger.LogInformation("Audit logs created for {EntryCount} upload entries.", logs.Count);
                        }
                        else
                        {
                            _logger.LogWarning("No valid entries found to log.");
                        }
                        return;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process UploadReports API for logging.");
                    }
                }





                // Logging if Resend Reports API is hit
                // Logging if Resend Reports API is hit
                if (context.Request?.Path.Value?.Contains("labs/reports/resend", StringComparison.OrdinalIgnoreCase) == true)
                {
                    _logger.LogInformation("Intercepted ResendReports API for audit logging.");
                    var tempLogs = new Dictionary<long, LabAuditLog>();
                    var logs = new List<LabAuditLog>();

                    try
                    {
                        var parsedBody = JsonConvert.DeserializeObject<ResendReport>(requestBody);
                        if (parsedBody?.Ids == null || parsedBody.Ids.Count == 0)
                        {
                            _logger.LogWarning("No IDs found in resend request.");
                            await _next(context);
                            return;
                        }

                        foreach (var id in parsedBody.Ids)
                        {
                            var report = await dbContext.LabUserReports.FirstOrDefaultAsync(r => r.Id == id);
                            if (report == null)
                            {
                                _logger.LogWarning("Report ID {Id} not found.", id);
                                continue;
                            }

                            if (report.LabId != labId)
                            {
                                _logger.LogWarning("Unauthorized attempt to log report ID {Id}. LabId mismatch.", id);
                                continue;
                            }

                            var log = new LabAuditLog
                            {
                                EntityName = "LabUserReports",
                                LabId = labId,
                                UserId = userId,
                                UserRole = role,
                                BranchId = report.BranchId == 0 ? 0 : report.BranchId,
                                Details = $"Resend operation logged for LabUserReport ID {id}. Original EpochTime: {report.EpochTime}, Resend Count: {report.Resend}",
                                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                                IpAddress = ipAddress,
                                SessionId = sessionId,
                                Url = requestUrl,
                                HttpMethod = requestMethod,
                                Category = context.Items.ContainsKey("Log-Category") ? context.Items["Log-Category"]?.ToString() ?? "General" : "General"
                            };

                            tempLogs[id] = log;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error while processing request payload for audit logging.");
                    }

                    try
                    {
                        if (context.Items.TryGetValue("PerReportLogs", out var responseLogObj) &&
                            responseLogObj is List<NotificationResponse> responseLogs)
                        {
                            foreach (var logEntry in responseLogs)
                            {
                                if (tempLogs.TryGetValue(logEntry.LabUserReportId, out var auditLog))
                                {
                                    var reportName = logEntry.ResendReportName ?? "Unknown";
                                    var reportType = logEntry.ResendReportType ?? "Unknown";

                                    var statusMsg = logEntry.Success
                                        ? $" ✅ Resend successful for Report: {reportName} [{reportType}]."
                                        : $" ❌ Resend failed. Reason: {logEntry.Reason}.";

                                    auditLog.Notifications += statusMsg;
                                    auditLog.Details += JsonConvert.SerializeObject(logEntry, Formatting.None);
                                    logs.Add(auditLog);
                                }
                            }

                            if (logs.Count > 0)
                            {
                                await dbContext.LabAuditLogs.AddRangeAsync(logs);
                                await dbContext.SaveChangesAsync();
                                _logger.LogInformation("Audit logs recorded for {Count} resend operations.", logs.Count);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("No response data found to complete resend audit log details.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error while processing response for resend audit logging.");
                    }

                    return;
                }





                // Default fallback for other endpoints
                var apiLog = new LabAuditLog
                {
                    EntityName = context.Request?.RouteValues["controller"]?.ToString() ?? "Unknown",
                    LabId = labId,
                    UserId = userId,
                    UserRole = role,
                    BranchId = branchId,
                    Details = $"Request to {requestUrl} with method {requestMethod}. Payload: {requestBody}",
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    IpAddress = ipAddress,
                    SessionId = sessionId,
                    Url = requestUrl,
                    HttpMethod = requestMethod,
                    Category = context.Items.ContainsKey("Log-Category") ? context.Items["Log-Category"]?.ToString() ?? "General" : "General"
                };

                dbContext.LabAuditLogs.Add(apiLog);
                await dbContext.SaveChangesAsync();

                _logger.LogInformation("API Call Logged Successfully - URL: {RequestUrl}, Method: {RequestMethod}, Timestamp: {Timestamp}",
                    requestUrl, requestMethod, apiLog.Timestamp);
            }
        }
    }
}
