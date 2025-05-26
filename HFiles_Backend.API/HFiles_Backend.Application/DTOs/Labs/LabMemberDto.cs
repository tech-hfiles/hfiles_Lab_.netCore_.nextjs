using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HFiles_Backend.Application.DTOs.Labs
{
    public class LabMemberDto
    {
        public string? HFID { get; set; } 
        public string? BranchName { get; set; } 
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }
    }
}
