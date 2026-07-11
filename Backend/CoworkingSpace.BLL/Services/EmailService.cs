using CoworkingSpace.BLL.Interfaces;
using CoworkingSpace.Models;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace CoworkingSpace.BLL.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettingsModel _settings;

        public EmailService(IOptions<EmailSettingsModel> settings)
        {
            _settings = settings.Value;
        }

        public async Task<bool> SendBookingConfirmationAsync(clsApplicationEmailLogs emailLog)
        {
            await ExecuteEmailSendingAsync(emailLog);
            return await emailLog.Save();
        }

        public async Task<bool> SendEventConfirmationAsync(clsApplicationEmailLogs eventEmailLog)
        {
            await ExecuteEmailSendingAsync(eventEmailLog);
            return await eventEmailLog.Save();
        }

        
        private async Task ExecuteEmailSendingAsync(clsApplicationEmailLogs log)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            message.To.Add(new MailboxAddress("", log.RecipientEmail));
            message.Subject = log.Subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = log.Body };
            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                try
                {
                    // استخدام Auto يتكيف تلقائياً مع المنفذ 465 أو 587
                    await client.ConnectAsync(_settings.SmtpServer, _settings.Port, SecureSocketOptions.Auto);

                    client.AuthenticationMechanisms.Clear();
                    client.AuthenticationMechanisms.Add("PLAIN");
                    client.AuthenticationMechanisms.Add("LOGIN");

                    await client.AuthenticateAsync(_settings.SenderEmail, _settings.AppPassword);
                    await client.SendAsync(message);

                    log.Status = "Sent";
                    log.ErrorMessage = "";
                }
                catch (Exception ex)
                {
                    log.Status = "Failed";
                    log.ErrorMessage = ex.Message;
                }
                finally
                {
                    log.SentDate = DateTime.Now;
                    if (client.IsConnected)
                    {
                        await client.DisconnectAsync(true);
                    }
                }
            }
        }
    }
}