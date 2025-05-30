using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HFiles_Backend.Application.DTOs.Labs
{
    public class LocationDetailsResponseDto
    {
        public string? Status { get; set; }
        public List<PostOfficeDto>? PostOffice { get; set; }  // ✅ Matches API response
    }

    public class PostOfficeDto
    {
        public string? Name { get; set; }  // ✅ Area
        public string? District { get; set; }  // ✅ City
        public string? State { get; set; }  // ✅ State
    }

}
