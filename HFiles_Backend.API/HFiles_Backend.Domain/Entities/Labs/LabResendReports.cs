namespace HFiles_Backend.Domain.Entities.Labs
{
    public class LabResendReports
    {
        public int Id { get; set; }

        public int LabUserReportId { get; set; }

        public long ResendEpochTime { get; set; }
    }
}
