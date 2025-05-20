using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HFiles_Backend.Domain.Entities.Labs
{
    public class LabUserReports
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; } 

        public int LabId { get; set; }

        public string? Name { get; set; }

        public long EpochTime { get; set; }
    }
}
