using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HFiles_Backend.Application.DTOs.Labs
{
    public class PromoteMembersRequestDto
    {
        [Required(ErrorMessage = "At least one member ID is required.")]
        public List<int> Ids { get; set; } = new();
    }
}
