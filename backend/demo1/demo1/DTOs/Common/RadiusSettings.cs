namespace demo1.DTOs.Common
{
    public class RadiusSettings
    {
        public bool Enabled { get; set; } = true;
        public string Server { get; set; } = string.Empty;
        public int Port { get; set; } = 1812;
        public string SharedSecret { get; set; } = string.Empty;
        public int Timeout { get; set; } = 3000;

        public bool IsConfigured =>
            Enabled &&
            !string.IsNullOrWhiteSpace(Server) &&
            !string.IsNullOrWhiteSpace(SharedSecret) &&
            Port > 0 &&
            Timeout > 0;
    }
}
