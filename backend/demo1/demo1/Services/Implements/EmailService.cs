using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
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

            // Mock mode: chưa cấu hình SMTP thật
            if (_emailSettings.Host == "smtp.gmail.com" && _emailSettings.Username == "your_email@gmail.com")
            {
                _logger.LogWarning(
                    "SMTP is not configured with real credentials. Logging email body instead:\nTo: {ToEmail}\nSubject: {Subject}\nBody: {Body}",
                    toEmail, subject, body);
                return;
            }

            try
            {
                // Lấy password: ưu tiên EncryptedPassword, fallback về Password plain text
                string effectivePassword = _emailSettings.Password ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(_emailSettings.EncryptedPassword))
                {
                    var decrypted = demo1.Services.Helpers.SecretProtector.Decrypt(_emailSettings.EncryptedPassword);
                    if (!string.IsNullOrEmpty(decrypted))
                        effectivePassword = decrypted;
                }

                // Build message
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = body };

                _logger.LogInformation(
                    "Attempting to send email to {ToEmail} with subject: {Subject}", toEmail, subject);

                using var client = new SmtpClient();

                // Bypass certificate validation nếu được cấu hình (dùng cho cert tự ký nội bộ)
                if (_emailSettings.BypassCertificateValidation)
                {
                    client.ServerCertificateValidationCallback =
                        (object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
                            => true;
                }

                // Chọn SecureSocketOptions dựa trên EnableSsl:
                //   true  → StartTls: BẮT BUỘC STARTTLS, ném exception nếu server không hỗ trợ
                //           (KHÔNG fallback về plain text như StartTlsWhenAvailable)
                //   false → kết nối plain text hoàn toàn (chỉ dùng trong môi trường test nội bộ)
                var socketOptions = _emailSettings.EnableSsl
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.None;

                await client.ConnectAsync(_emailSettings.Host, _emailSettings.Port, socketOptions);

                // ── TLS diagnostics (dùng property sẵn có của MailKit, không cần package thêm) ──
                _logger.LogInformation(
                    "[SMTP-TLS] Host={Host}:{Port} | IsEncrypted={IsEncrypted} | Protocol={SslProtocol} | Cipher={CipherAlgorithm}",
                    _emailSettings.Host,
                    _emailSettings.Port,
                    client.IsEncrypted,
                    client.SslProtocol,
                    client.SslCipherAlgorithm);

                if (!client.IsEncrypted)
                {
                    _logger.LogWarning(
                        "[SMTP-TLS] ⚠️ Kết nối tới {Host}:{Port} KHÔNG được mã hóa (plain text)!",
                        _emailSettings.Host, _emailSettings.Port);
                }
                // ─────────────────────────────────────────────────────────────────────────────────

                // Xác thực nếu có username (MailKit dùng AUTH PLAIN/LOGIN, không cần libgssapi_krb5)
                if (!string.IsNullOrWhiteSpace(_emailSettings.Username))
                {
                    await client.AuthenticateAsync(_emailSettings.Username, effectivePassword);
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {ToEmail}.", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail}.", toEmail);
            }
        }
    }
}
