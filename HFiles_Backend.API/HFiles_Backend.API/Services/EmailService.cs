using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System;

namespace HFiles_Backend.API.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var host = _configuration["Smtp:Host"];
                var port = int.Parse(_configuration["Smtp:Port"]);
                var username = _configuration["Smtp:Username"];
                var password = _configuration["Smtp:Password"];
                var from = _configuration["Smtp:From"];


                Console.WriteLine("Email Config:");
                Console.WriteLine($"Host: {_configuration["Smtp:Host"]}");
                Console.WriteLine($"Port: {_configuration["Smtp:Port"]}");
                Console.WriteLine($"Username: {_configuration["Smtp:Username"]}");
                Console.WriteLine($"Password: {_configuration["Smtp:Password"]}");
                Console.WriteLine($"From: {_configuration["Smtp:From"]}");


                using var smtpClient = new SmtpClient(host)
                {
                    Port = port,
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = true,
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(from),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                };

                mailMessage.To.Add(toEmail);

                Console.WriteLine($"Sending email to: {toEmail}");

                await smtpClient.SendMailAsync(mailMessage);

                Console.WriteLine("✅ Email sent successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ FULL ERROR LOG:");
                Console.WriteLine(ex.ToString());
                throw new Exception("Failed to send email via SMTP.", ex);
            }
        }
    }
}
