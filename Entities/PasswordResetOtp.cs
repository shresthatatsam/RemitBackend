namespace EverestRemit.Api.Entities
{
    public class PasswordResetOtp
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }

        public string OtpHash { get; set; } = null!;

        public DateTime ExpiresAtUtc { get; set; }
        public DateTime? UsedAtUtc { get; set; }

        public int FailedAttempts { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
