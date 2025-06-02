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
                var host = _configuration["Smtp:Host"]
                           ?? throw new InvalidOperationException("SMTP Host is not configured.");
                var portStr = _configuration["Smtp:Port"]
                              ?? throw new InvalidOperationException("SMTP Port is not configured.");
                var username = _configuration["Smtp:Username"]
                               ?? throw new InvalidOperationException("SMTP Username is not configured.");
                var password = _configuration["Smtp:Password"]
                               ?? throw new InvalidOperationException("SMTP Password is not configured.");
                var from = _configuration["Smtp:From"]
                           ?? throw new InvalidOperationException("SMTP From address is not configured.");

                if (!int.TryParse(portStr, out int port))
                    throw new InvalidOperationException("SMTP Port is not a valid integer.");

                Console.WriteLine("Email Config:");
                Console.WriteLine($"Host: {host}");
                Console.WriteLine($"Port: {port}");
                Console.WriteLine($"Username: {username}");
                Console.WriteLine($"Password: {password}");
                Console.WriteLine($"From: {from}");

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
