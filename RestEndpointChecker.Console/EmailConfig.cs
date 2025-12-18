namespace RestEndpointChecker.Console
{
    public class EmailConfig
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 25;
        public string FromAddress { get; set; } = string.Empty;
        public string ToAddress { get; set; } = string.Empty;
        public string Subject { get; set; } = "URL Check Results";
        public string? Username { get; set; }
        public string? Password { get; set; }
        public bool UseSsl { get; set; } = false;
        public bool AcceptAllCertificates { get; set; } = true;
    }
}
