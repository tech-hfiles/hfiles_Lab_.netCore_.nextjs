using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HFiles_Backend.Application.DTOs.Labs
{
    public class HFIDRequest
    {
        [Required(ErrorMessage = "HFID is required.")]
        public string HFID { get; set; } = null!;
    }
}
