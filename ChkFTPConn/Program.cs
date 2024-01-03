using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace ChkFTPConn
{
    internal class Program
    {
        private static readonly string url = "https://61.222.180.120:5001/";
        private static readonly ILogger<Program> logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<Program>();

        static async Task Main(string[] args)
        {
            while (true)
            {
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        var response = await httpClient.GetAsync(url);
                        if (!response.IsSuccessStatusCode)
                        {
                            string message = $"Failed to connect to {url}. Status code: {response.StatusCode}";
                            logger.LogError(message);
                            SendEmail(message).Wait();
                        }
                        else
                        {
                            logger.LogInformation($"Successfully connected to {url}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    string message = $"Error checking connection for {url}. Exception: {ex.Message}";
                    logger.LogError(message);
                    SendEmail(message).Wait();
                }

                await Task.Delay(TimeSpan.FromMinutes(30));
            }
        }

        private static async Task SendEmail(string message)
        {
            // Set up email settings
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("Your Name", "your.email@example.com"));
            email.To.Add(new MailboxAddress("Recipient Name", "recipient.email@example.com"));
            email.Subject = "Website Connection Alert";
            email.Body = new TextPart("plain") { Text = message };

            // Replace with your SMTP server settings
            string smtpHost = "smtp.example.com";
            int smtpPort = 587;
            string smtpUser = "your.email@example.com";
            string smtpPass = "your-password";

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(smtpUser, smtpPass);
                await client.SendAsync(email);
                await client.DisconnectAsync(true);
            }
        }
    }
}