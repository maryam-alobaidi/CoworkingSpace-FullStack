using CoworkingSpace.BLL.Interfaces;
using CoworkingSpace.Models;
using Microsoft.Extensions.Options;
using MimeKit;


namespace CoworkingSpace.BLL.Services
{
    // Service to handle email sending and logging لان ال IOptions لا تعمل مع static class لذلك تم انشاء service لارسال الايميلات
    public class EmailService : IEmailService
    {
        private readonly EmailSettingsModel _settings;



        public EmailService(IOptions<EmailSettingsModel> settings)
        {
            _settings = settings.Value;
        }

        //method to send email and log the result in the database
        public async Task<bool> SendBookingConfirmationAsync(clsApplicationEmailLogs emailLog)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            message.To.Add(new MailboxAddress("", emailLog.RecipientEmail));
            message.Subject = emailLog.Subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = emailLog.Body };
            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new MailKit.Net.Smtp.SmtpClient())
            {
                try
                {
                    await client.ConnectAsync(_settings.SmtpServer, _settings.Port, MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(_settings.SenderEmail, _settings.AppPassword);
                    await client.SendAsync(message);

                    emailLog.Status = "Sent";
                    emailLog.ErrorMessage = "";
                }
                catch (Exception ex)
                {
                    emailLog.Status = "Failed";
                    emailLog.ErrorMessage = ex.Message;
                }
                finally
                {
                    emailLog.SentDate = DateTime.Now;
                }
            }

            return await emailLog.Save();
        }


        public async Task<bool> SendEventConfirmationAsync(clsApplicationEmailLogs eventEmailLog)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            message.To.Add(new MailboxAddress("", eventEmailLog.RecipientEmail));
            message.Subject = eventEmailLog.Subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = eventEmailLog.Body };
            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new MailKit.Net.Smtp.SmtpClient())
            {
                try
                {
                    // 1. الاتصال (يفضل استخدام Auto أو SslOnConnect مع 465)
                    await client.ConnectAsync(_settings.SmtpServer, _settings.Port, MailKit.Security.SecureSocketOptions.Auto);

                    client.AuthenticationMechanisms.Clear(); // نمسح كل شيء
                    client.AuthenticationMechanisms.Add("PLAIN"); // نضيف النوع البسيط
                    client.AuthenticationMechanisms.Add("LOGIN"); // والنوع التقليدي

                    // 3. التوثيق باستخدام الإيميل والرمز (الـ 16 حرفاً بدون مسافات)
                    await client.AuthenticateAsync(_settings.SenderEmail, _settings.AppPassword);

                    await client.SendAsync(message);
                    eventEmailLog.Status = "Sent";
                }
                catch (Exception ex)
                {
                    eventEmailLog.Status = "Failed";
                    eventEmailLog.ErrorMessage = ex.Message;
                    Console.WriteLine($"Current Password in Memory: {_settings.AppPassword}");
                }
                finally
                {
                    await client.DisconnectAsync(true);
                }
            }

            // حفظ النتيجة النهائية (نجاح أو فشل) في جدول EventEmailLogs
            return await eventEmailLog.Save();
        }

    }
}