using System.Net.Mail;
using System.Net;

namespace juezprueba.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public EmailSender(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            await SendEmailWithTemplateAsync(email, subject, "default", new
            {
                Title = subject,
                Content = message,
                Year = DateTime.Now.Year,
                AppName = "CodeMaster"
            });
        }

        public async Task SendEmailWithTemplateAsync(string email, string subject, string templateName, object model)
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");

            var client = new SmtpClient(smtpSettings["Host"], int.Parse(smtpSettings["Port"]))
            {
                Credentials = new NetworkCredential(smtpSettings["UserName"], smtpSettings["Password"]),
                EnableSsl = bool.Parse(smtpSettings["EnableSSL"])
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpSettings["UserName"], "CodeMaster"),
                Subject = subject,
                Body = await GetEmailTemplate(templateName, model),
                IsBodyHtml = true
            };

            mailMessage.To.Add(email);

            await client.SendMailAsync(mailMessage);
        }

        private async Task<string> GetEmailTemplate(string templateName, object model)
        {
            var templatePath = Path.Combine(_env.WebRootPath, "templates", $"{templateName}.html");
            var templateContent = await File.ReadAllTextAsync(templatePath);

            foreach (var prop in model.GetType().GetProperties())
            {
                templateContent = templateContent.Replace($"{{{{{prop.Name}}}}}", prop.GetValue(model)?.ToString());
            }

            return templateContent;
        }
    }
}