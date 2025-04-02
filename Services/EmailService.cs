using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace ClinicBookingSystem.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config) {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpConfig = _config.GetSection("Smtp");
            var fromEmail = smtpConfig["Username"];
            var password = smtpConfig["Password"];

            var mail = new MailMessage();
            mail.From = new MailAddress(fromEmail, "Clinic Booking");
            mail.To.Add(toEmail);
            mail.Subject = subject;
            mail.IsBodyHtml = true;
            mail.Body = body;

            using (var smtpClient = new SmtpClient(smtpConfig["Host"], int.Parse(smtpConfig["Port"])))
            {
                smtpClient.EnableSsl = true;
                smtpClient.Credentials = new NetworkCredential(fromEmail, password);
                await smtpClient.SendMailAsync(mail);
            }
        }
    }
}