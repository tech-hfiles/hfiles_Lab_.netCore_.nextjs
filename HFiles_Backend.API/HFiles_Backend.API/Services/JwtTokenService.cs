using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ApiJwtSettings = HFiles_Backend.API.Settings.JwtSettings;

namespace HFiles_Backend.API.Services
{
    public class JwtTokenService
    {
        private readonly ApiJwtSettings _jwtSettings;

        public JwtTokenService(IOptions<ApiJwtSettings> jwtOptions)
        {
            _jwtSettings = jwtOptions.Value ?? throw new ArgumentNullException(nameof(jwtOptions), "JWT settings cannot be null.");

            if (string.IsNullOrWhiteSpace(_jwtSettings.Key))
                throw new ArgumentException("JWT secret key is missing or empty in configuration.");

            if (string.IsNullOrWhiteSpace(_jwtSettings.Issuer))
                throw new ArgumentException("JWT issuer is missing or empty in configuration.");

            if (string.IsNullOrWhiteSpace(_jwtSettings.Audience))
                throw new ArgumentException("JWT audience is missing or empty in configuration.");

            if (_jwtSettings.DurationInMinutes <= 0)
                throw new ArgumentException("JWT duration must be a positive number.");
        }

        public string GenerateToken(int userId, string email, int labAdminId, string role)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be null or empty.", nameof(email));

            if (string.IsNullOrWhiteSpace(role))
                throw new ArgumentException("Role cannot be null or empty.", nameof(role));

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, email),
                new("UserId", userId.ToString()),
                new("LabAdminId", labAdminId.ToString()),
                new("Role", role)
            };

            var keyBytes = Encoding.UTF8.GetBytes(_jwtSettings.Key!);
            var key = new SymmetricSecurityKey(keyBytes);
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
