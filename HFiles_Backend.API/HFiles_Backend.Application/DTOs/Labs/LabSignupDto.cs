using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HFiles_Backend.Application.DTOs.Labs
{
    public class LabSignupDto
    {
        public string? LabName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Pincode { get; set; }
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }
        public string? Otp { get; set; }

    }
}
