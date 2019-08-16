﻿using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MVCMusicStore.Authentication;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;

namespace MVCMusicStore.Utilities
{
    public class EmailSender : IEmailSender
    {
        private AuthMessageSenderOptions Options { get; }

        public EmailSender(IOptions<AuthMessageSenderOptions> OptionsAccessor) => Options = OptionsAccessor.Value;

        private static Task Execute(string apiKey, string subject, string message, string email)
        {
            var client = new SendGridClient(apiKey);
            var msg = new SendGridMessage
            {
                From = new EmailAddress("kaygusuzburak@gmail.com", "Burak Kaygusuz"),
                Subject = subject,
                PlainTextContent = message,
                HtmlContent = message
            };
            msg.AddTo(new EmailAddress(email));

            // Disable click tracking.
            // See https://sendgrid.com/docs/User_Guide/Settings/tracking.html

            msg.SetClickTracking(false, false);

            return client.SendEmailAsync(msg);
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            return Execute(Options.SendGridKey, email, subject, htmlMessage);
        }
    }
}