using System.Security.Claims;
using HFiles_Backend.Domain.Entities.Labs;
using HFiles_Backend.Infrastructure.Data;

namespace HFiles_Backend.API.Middleware
{
    public class ExceptionLoggingMiddleware(RequestDelegate next, ILogger<ExceptionLoggingMiddleware> logger, IServiceProvider serviceProvider, IHttpContextAccessor httpContextAccessor)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<ExceptionLoggingMiddleware> _logger = logger;
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;





        public async Task Invoke(HttpContext context)
        {
            _logger.LogInformation("Processing API request for URL: {RequestUrl}, Method: {RequestMethod}, IP: {IpAddress}",
                context.Request.Path.Value ?? "Unknown",
                context.Request.Method ?? "Unknown",
                context.Connection.RemoteIpAddress?.ToString() ?? "Unknown");

            try
            {
                await _next(context);

                if (context.Response.StatusCode >= 400)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await LogCustomError(context, context.Response.StatusCode, dbContext);
                }
            }
            catch (Exception ex)
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await LogException(context, ex, dbContext);

                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("An unexpected error occurred.");
            }
        }





        private async Task LogCustomError(HttpContext context, int statusCode, AppDbContext dbContext)
        {
            var user = _httpContextAccessor.HttpContext?.User;

            string requestUrl = context.Request.Path.Value ?? "Unknown";
            string requestMethod = context.Request.Method ?? "Unknown";
            string ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            _logger.LogWarning("Custom error detected - Status Code: {StatusCode}, URL: {RequestUrl}, Method: {RequestMethod}, IP: {IpAddress}",
                statusCode, requestUrl, requestMethod, ipAddress);

            var errorLog = new LabErrorLog
            {
                EntityName = "CustomError",
                LabId = ExtractLabId(user),
                UserId = ExtractUserId(user),
                UserRole = ExtractUserRole(user),
                EntityId = context.Items.ContainsKey("RecordId") ? context.Items["RecordId"] as int? : null,
                Action = statusCode.ToString(),
                ErrorMessage = $"Custom Error - {statusCode}",
                StackTrace = "N/A",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                IpAddress = ipAddress,
                SessionId = ExtractSessionId(user),
                Url = requestUrl,
                HttpMethod = requestMethod,
                Category = "User Error"
            };

            dbContext.LabErrorLogs.Add(errorLog);
            await dbContext.SaveChangesAsync();
        }

        private async Task LogException(HttpContext context, Exception ex, AppDbContext dbContext)
        {
            var user = _httpContextAccessor.HttpContext?.User;

            string requestUrl = context.Request.Path.Value ?? "Unknown";
            string requestMethod = context.Request.Method ?? "Unknown";
            string ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            _logger.LogError(ex, "Unhandled exception occurred - URL: {RequestUrl}, Method: {RequestMethod}, IP: {IpAddress}",
                requestUrl, requestMethod, ipAddress);

            var errorLog = new LabErrorLog
            {
                EntityName = "GlobalError",
                LabId = ExtractLabId(user),
                UserId = ExtractUserId(user),
                UserRole = ExtractUserRole(user),
                EntityId = context.Items.ContainsKey("RecordId") ? context.Items["RecordId"] as int? : null,
                Action = "EXCEPTION",
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace ?? "No stack trace available",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                IpAddress = ipAddress,
                SessionId = ExtractSessionId(user),
                Url = requestUrl,
                HttpMethod = requestMethod,
                Category = "System Error"
            };

            dbContext.LabErrorLogs.Add(errorLog);
            await dbContext.SaveChangesAsync();
        }





        // Helper methods to extract claim values safely
        private int? ExtractLabId(ClaimsPrincipal? user) => user?.FindFirst("UserId")?.Value is string value && int.TryParse(value, out var id) ? id : 0;
        private int? ExtractUserId(ClaimsPrincipal? user) => user?.FindFirst("LabAdminId")?.Value is string value && int.TryParse(value, out var id) ? id : 0;
        private string? ExtractUserRole(ClaimsPrincipal? user) => user?.FindFirst("Role")?.Value ?? "Unknown";
        private string? ExtractSessionId(ClaimsPrincipal? user) => user?.FindFirst("SessionId")?.Value ?? "Unknown";

    }
}
