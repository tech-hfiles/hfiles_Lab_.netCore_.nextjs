using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HFiles_Backend.Application.DTOs.Labs
{
    public class LabUpdateDto
    {
        public int Id { get; set; } 
        public string? Address { get; set; } 
        public string? ProfilePhoto { get; set; } 
    }
}
