using System.ComponentModel.DataAnnotations;

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

        public int BranchId { get; set; } = 0;

        public int Resend { get; set; } = 0;
    }
}
