using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HFiles_Backend.Application.DTOs.Labs
{
    public class LoginOtpRequest
    {
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string? Email { get; set; } 

        [Phone(ErrorMessage = "Invalid phone number.")]
        public string? PhoneNumber { get; set; } 
    }

    public class OtpLogin
    {
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string? Email { get; set; } 

        [Phone(ErrorMessage = "Invalid phone number.")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "OTP is required.")]
        public string Otp { get; set; } = null!;
    }

    public class PasswordLogin
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = null!;
    }
}
