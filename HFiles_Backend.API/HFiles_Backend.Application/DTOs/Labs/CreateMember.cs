using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HFiles_Backend.Application.DTOs.Labs
{
    public class CreateMember
    {
        [Required(ErrorMessage = "HFID is required.")]
        public string HFID { get; set; } = null!;

        [Required(ErrorMessage = "BranchId is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "BranchId must be greater than zero.")]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Confirm Password is required.")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = null!;
    }
}
