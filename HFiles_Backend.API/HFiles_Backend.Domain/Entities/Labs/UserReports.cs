using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HFiles_Backend.Domain.Entities.Labs
{
    [Table("user_reports")]
    public class UserReports
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        public string? ReportName { get; set; }
        public string? MemberId { get; set; } = "0";
        public string? ReportUrl { get; set; }
        public int ReportId { get; set; }
        public string? IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? AccessMappingId { get; set; }
        public double FileSize { get; set; }

        public string? UploadType { get; set; }
        public string? NewIsActive { get; set; }

        [Column("UploadedBy")]
        public string? UploadedBy { get; set; }
        public int? LabId { get; set; }
        public int? LabUserReportId { get; set; }

    }

}
