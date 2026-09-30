using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using demo1.DTOs.Common;
using demo1.Services.Interfaces;

namespace demo1.Services.Implements
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("Cannot send email: recipient address is empty.");
                return;
            }

            try
            {
                var senderEmail = !string.IsNullOrWhiteSpace(_emailSettings.SenderEmail) 
                    ? _emailSettings.SenderEmail.Trim() 
                    : "no-reply-qlda@co-opbank.vn";
                var senderName = !string.IsNullOrWhiteSpace(_emailSettings.SenderName) 
                    ? _emailSettings.SenderName.Trim() 
                    : "Hệ thống quản lý hợp đồng Coopbank";

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail.Trim());

                if (_emailSettings.BypassCertificateValidation)
                {
                    ServicePointManager.ServerCertificateValidationCallback =
                        (sender, certificate, chain, sslPolicyErrors) => true;
                }

                using var smtpClient = new SmtpClient(_emailSettings.Host, _emailSettings.Port)
                {
                    EnableSsl = _emailSettings.EnableSsl
                };

                // Nếu có cấu hình Username thì dùng xác thực, nếu không thì dùng chế độ Anonymous Relay (Không User/Password - theo IP Whitelist)
                if (!string.IsNullOrWhiteSpace(_emailSettings.Username))
                {
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password);
                }
                else
                {
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = null;
                }

                _logger.LogInformation("Attempting to send email to {ToEmail} with subject: {Subject} via SMTP {Host}:{Port} (Auth: {HasAuth})", 
                    toEmail, subject, _emailSettings.Host, _emailSettings.Port, !string.IsNullOrWhiteSpace(_emailSettings.Username));
                
                // For development/mock purposes, if host is not configured properly, log it as mock
                if (_emailSettings.Host == "smtp.gmail.com" && _emailSettings.Username == "your_email@gmail.com")
                {
                    _logger.LogWarning("SMTP is not configured with real credentials. Logging email body instead:\nTo: {ToEmail}\nSubject: {Subject}\nBody: {Body}", toEmail, subject, body);
                    return;
                }

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {ToEmail}.", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail}.", toEmail);
            }
        }
    }
}
