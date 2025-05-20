namespace HFiles_Backend.API.DTOs.Labs
{
    public class LabUserReportBatchUploadDTO
    {
        public List<LabUserReportUploadDTO> Entries { get; set; } = new();
    }
}
