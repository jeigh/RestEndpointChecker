using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Net;

namespace RestEndpointChecker.Console
{
    public class EmailService
    {
        private readonly EmailConfig _config;

        public EmailService(EmailConfig config)
        {
            _config = config;
        }

        public async Task SendResultsAsync(string messageBody)
        {
            using var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_config.FromAddress));
            message.To.Add(MailboxAddress.Parse(_config.ToAddress));
            message.Subject = _config.Subject;

            var htmlBody = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <style>
        body {{
            font-family: 'Courier New', Courier, monospace;
            font-size: 12px;
            line-height: 1.4;
            white-space: pre;
            margin: 20px;
        }}
    </style>
</head>
<body>{WebUtility.HtmlEncode(messageBody)}</body>
</html>";

            var bodyBuilder = new BodyBuilder
            {
                TextBody = messageBody,  
                HtmlBody = htmlBody      
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            
            try
            {
                if (_config.AcceptAllCertificates)
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                var secureSocketOptions = _config.SmtpPort switch
                {
                    25 => SecureSocketOptions.StartTlsWhenAvailable,  
                    587 => SecureSocketOptions.StartTls,              
                    465 => SecureSocketOptions.SslOnConnect,          
                    _ => _config.UseSsl ? SecureSocketOptions.Auto : SecureSocketOptions.None
                };

                await client.ConnectAsync(_config.SmtpServer, _config.SmtpPort, secureSocketOptions);

                if (!string.IsNullOrEmpty(_config.Username) && !string.IsNullOrEmpty(_config.Password))
                    await client.AuthenticateAsync(_config.Username, _config.Password);

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error sending email: {ex.Message}");
                throw;
            }
        }
    }
}
