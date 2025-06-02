namespace juezprueba.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
        Task SendEmailWithTemplateAsync(string email, string subject, string templateName, object model);
    }
}
