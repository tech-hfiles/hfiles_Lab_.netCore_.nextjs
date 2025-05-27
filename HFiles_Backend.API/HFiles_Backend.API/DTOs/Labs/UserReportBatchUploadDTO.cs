using System.ComponentModel.DataAnnotations;

namespace HFiles_Backend.API.DTOs.Labs
{
    public class UserReportBatchUploadDTO
    {
        [Required(ErrorMessage = "At least one entry is required.")]
        [MinLength(1, ErrorMessage = "The Entries list cannot be empty.")]
        public List<UserReportUploadDTO> Entries { get; set; } = new();
    }
}
