using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HFiles_Backend.Application.DTOs.Labs
{
    public class LabAdminLoginDto
    {
        [Required]
        public string? HFID { get; set; }

        [Required]
        public string? Role { get; set; }

        [Required]
        public string? Password { get; set; }
    }
}
