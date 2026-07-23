namespace demo1.DTOs.Common
{
    public class EmailSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
        public string SenderName { get; set; } = "Hệ thống quản lý hợp đồng";
        public string SenderEmail { get; set; } = string.Empty;
        public bool BypassCertificateValidation { get; set; } = false;
    }
}
