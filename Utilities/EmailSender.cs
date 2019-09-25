using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Text;
using System.Threading.Tasks;

namespace MVCMusicStore.Utilities
{
    public class EmailSender
    {
        private readonly string senderName;
        private readonly string senderEmailAddress;
        private readonly string bodyContent;
        private readonly string userName;
        private readonly string password;
        private const string Subject = "";
        private const string SmtpServer = "smtp.gmail.com";
        private const int SmtpPort = 587;

        private readonly ILogger<EmailSender> logger;

        public EmailSender(string senderName, string senderEmailAddress, string bodyContent, string userName, string password, ILogger<EmailSender> log)
        {
            this.senderName = senderName;
            this.senderEmailAddress = senderEmailAddress;
            this.bodyContent = bodyContent;
            this.userName = userName;
            this.password = password;
            logger = log;
        }

        private async Task SendEmailAsync(string reciever)
        {
            try
            {
                var message = new MimeMessage
                {
                    Sender = new MailboxAddress(senderName, senderEmailAddress),
                    Subject = Subject,
                    Body = new TextPart(TextFormat.Html) { Text = bodyContent }
                };

                message.To.Add(new MailboxAddress(reciever));

                using (var smtpClient = new SmtpClient())
                {
                    smtpClient.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    await smtpClient.ConnectAsync(SmtpServer, SmtpPort, true);
                    await smtpClient.AuthenticateAsync(userName, password);
                    await smtpClient.SendAsync(message);
                }
            }
            catch (SmtpCommandException ex)
            {
                logger.LogError($"Failed to send email: '{ex.Message}'");
            }
        }
    }
}