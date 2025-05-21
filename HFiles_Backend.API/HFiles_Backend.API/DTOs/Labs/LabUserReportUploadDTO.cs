namespace HFiles_Backend.API.DTOs.Labs
{
    public class LabUserReportUploadDTO
    {
        public string? HFID { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
        public string? ReportType { get; set; }
        public List<IFormFile> ReportFiles { get; set; } = new();
        public int? BranchId { get; set; }
    }
}
