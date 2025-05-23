using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HFiles_Backend.Domain.Entities.Labs
{
    [NotMapped]
    [Table("user_details")]
    public class UserDetails
    {
        public int user_id { get; set; }
        public string? user_firstname { get; set; }
        public string? user_lastname { get; set; }
        public string? user_membernumber { get; set; }
        public string? user_email { get; set; }
        public string? user_reference { get; set; }
        public string? user_image { get; set; }

    }
}
