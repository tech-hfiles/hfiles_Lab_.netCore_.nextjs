using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HFiles_Backend.Application.DTOs.Labs
{
    public class EmailRequestDto
    {
        public string? Email { get; set; }
    }
    public class OtpLoginDto
    {
        public string? Email { get; set; }
        public string? Otp { get; set; }
    }

    public class PasswordLoginDto
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
    }
}
