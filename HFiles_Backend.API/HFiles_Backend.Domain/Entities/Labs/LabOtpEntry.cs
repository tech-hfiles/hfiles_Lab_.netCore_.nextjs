namespace HFiles_Backend.Domain.Entities.Labs
{
    public class LabOtpEntry
    {
        public int Id { get; set; }
        public string? Email { get; set; }
        public string? OtpCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiryTime { get; set; }
    }
}
