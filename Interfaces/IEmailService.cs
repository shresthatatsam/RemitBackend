namespace EverestRemit.Api.Interfaces
{
    public interface IEmailService
    {
        Task SendAsync(
            string to,
            string subject,
            string htmlBody,
            CancellationToken ct = default);
    }
}
