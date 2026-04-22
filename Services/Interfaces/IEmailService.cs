using System.Threading.Tasks;

public interface IEmailService
{
    Task SendEmailAsync(string recipient, string subject, string body, bool isHtml = false);
}
