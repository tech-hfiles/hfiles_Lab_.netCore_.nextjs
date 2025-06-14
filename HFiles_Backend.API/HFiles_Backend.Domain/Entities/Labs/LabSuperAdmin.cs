using System.ComponentModel.DataAnnotations;

namespace HFiles_Backend.Domain.Entities.Labs
{
    public class LabSuperAdmin
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int LabId { get; set; }

        [Required]
        public string? PasswordHash { get; set; }

        [Required]
        public long EpochTime { get; set; }

        [Required]
        public int IsMain { get; set; } = 1;
    }
}
