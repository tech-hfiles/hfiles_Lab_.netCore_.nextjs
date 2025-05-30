using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HFiles_Backend.Application.DTOs.Labs
{
    public class PromoteAdminDto
    {
        [Required(ErrorMessage = "MemberID is Required.")]
        public int MemberId { get; set; }
    }
}
