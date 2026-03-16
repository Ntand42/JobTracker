using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;

namespace JobTracker.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var mailServer = _configuration["EmailSettings:MailServer"] ?? "smtp.gmail.com";
            var portString = _configuration["EmailSettings:MailPort"];
            if (!int.TryParse(portString, out var port))
            {
                port = 587;
            }

            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            var password = _configuration["EmailSettings:Password"];
            var senderName = _configuration["EmailSettings:SenderName"] ?? "Job Tracker";

            var hasRealCredentials =
                !string.IsNullOrWhiteSpace(senderEmail) &&
                !string.IsNullOrWhiteSpace(password) &&
                !senderEmail.Contains("YOUR_GMAIL_ADDRESS", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(password, "YOUR_APP_PASSWORD_HERE", StringComparison.Ordinal);

            if (!hasRealCredentials)
            {
                Console.WriteLine("[EmailSender] EmailSettings not configured. Using console fallback.");
                Console.WriteLine($"[EmailSender] To: {email}");
                Console.WriteLine($"[EmailSender] Subject: {subject}");
                Console.WriteLine($"[EmailSender] Body: {htmlMessage}");
                return;
            }

            var senderEmailValue = senderEmail!;
            var passwordValue = password!;

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmailValue, senderName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            mailMessage.To.Add(email);

            async Task SendWithPortAsync(int targetPort)
            {
                using var client = new SmtpClient(mailServer, targetPort)
                {
                    Credentials = new NetworkCredential(senderEmailValue, passwordValue),
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 20000
                };

                Console.WriteLine($"[EmailSender] Sending via {mailServer}:{targetPort} as {senderEmailValue} -> {email}");
                await client.SendMailAsync(mailMessage);
            }

            try
            {
                await SendWithPortAsync(port);
                Console.WriteLine($"[EmailSender] Successfully sent email to {email}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailSender] FAILED to send email to {email}. Error: {ex.GetType().Name}: {ex.Message}");
                if (ex is SmtpException smtpEx)
                {
                    Console.WriteLine($"[EmailSender] SmtpStatusCode: {smtpEx.StatusCode}");
                    if (smtpEx.InnerException != null)
                    {
                        Console.WriteLine($"[EmailSender] Inner: {smtpEx.InnerException.GetType().Name}: {smtpEx.InnerException.Message}");
                    }
                }

                var looksLikePortBlocked =
                    ex is SocketException ||
                    (ex is SmtpException se && se.InnerException is SocketException) ||
                    ex.Message.Contains("timed out", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("No connection could be made", StringComparison.OrdinalIgnoreCase);

                if (looksLikePortBlocked && mailServer.Equals("smtp.gmail.com", StringComparison.OrdinalIgnoreCase) && port != 465)
                {
                    try
                    {
                        Console.WriteLine("[EmailSender] Retrying via smtp.gmail.com:465...");
                        await SendWithPortAsync(465);
                        Console.WriteLine($"[EmailSender] Successfully sent email to {email} (via 465)");
                        return;
                    }
                    catch (Exception retryEx)
                    {
                        Console.WriteLine($"[EmailSender] Retry FAILED. Error: {retryEx.GetType().Name}: {retryEx.Message}");
                    }
                }

                Console.WriteLine($"[EmailSender] Fallback to Console:");
                Console.WriteLine($"[EmailSender] To: {email}");
                Console.WriteLine($"[EmailSender] Subject: {subject}");
                Console.WriteLine($"[EmailSender] Body: {htmlMessage}");
            }
        }
    }
}
