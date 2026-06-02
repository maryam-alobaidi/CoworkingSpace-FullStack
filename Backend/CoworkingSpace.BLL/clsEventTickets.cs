using CoworkingSpace.BLL.Interfaces;
using CoworkingSpace.DAL;
using CoworkingSpace.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;
using System.Transactions;

namespace CoworkingSpace.BLL
{
    public class clsEventTickets
    {
        public enum enMode { addNew = 0, update = 1 }
        public enMode Mode = enMode.addNew;

        // الخصائص الأساسية
        public int Id { get; set; }
        public int EventId { get; set; }
        public int UserId { get; set; }
        public string TicketCode { get; set; }
        public DateTime PurchaseDate { get; set; }

        // الخصائص الجديدة المرتبطة بالدفع (يجب إضافتها للموديل أيضاً)
        public string PaymentStatus { get; set; }
        public string? TransactionId { get; set; }

        public clsEventTickets()
        {
            this.Id = -1;
            this.EventId = -1;
            this.UserId = -1;
            this.TicketCode = GenerateUniqueTicketCode();
            this.PurchaseDate = DateTime.Now;
            this.PaymentStatus = "Pending"; // الحالة الافتراضية عند الحجز
            this.Mode = enMode.addNew;
        }

        private clsEventTickets(eventTicketModel model)
        {
            this.Id = (int)model.Id;
            this.EventId = model.EventId;
            this.UserId = model.UserId;
            this.TicketCode = model.TicketCode ?? string.Empty;
            this.PurchaseDate = model.PurchaseDate ?? DateTime.Now;
            this.PaymentStatus = model.PaymentStatus ?? "Pending";
            this.TransactionId = model.TransactionId;

            this.Mode = enMode.update;
        }


        public static async Task<List<eventTicketModel>> GetAll()
        {
            return await clsEventTicketsData.GetAllEventTickets();
        }
        public static string GenerateUniqueTicketCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        // دالة الحفظ الأساسية
        public async Task<bool> Save()
        {
            switch (Mode)
            {
                case enMode.addNew:
                    if (await _AddNewEventTickets())
                    {
                        Mode = enMode.update;
                        return true;
                    }
                    return false;
                case enMode.update:
                    return await _UpdateEventTickets();
            }
            return false;
        }

        private async Task<bool> _AddNewEventTickets()
        {
            eventTicketModel model = _MapToModel();
            this.Id = (int)await clsEventTicketsData.AddNewEventTickets(model);
            return (this.Id != -1);
        }

        private async Task<bool> _UpdateEventTickets()
        {
            return await clsEventTicketsData.UpdateEventTickets(_MapToModel());
        }

        // دالة مساعدة لتحويل البيانات للموديل (تمنع التكرار)
        private eventTicketModel _MapToModel()
        {
            return new eventTicketModel
            {
                Id = this.Id,
                EventId = this.EventId,
                UserId = this.UserId,
                TicketCode = this.TicketCode,
                PurchaseDate = this.PurchaseDate,
                PaymentStatus = this.PaymentStatus,
                TransactionId = this.TransactionId
            };
        }

        public static async Task<clsEventTickets> Find(int Id)
        {
            eventTicketModel model = new eventTicketModel();
                var result = await clsEventTicketsData.FindByID(Id, model);
            if (result == null || result.Id == null)
                return null;
            return new clsEventTickets(result);
        }

       
        public async Task<bool> SaveTicketWithEmailLog(string recipientEmail, string userName, IEmailService emailService)
        {
         
            var eventDetails = clsEvents.Find(this.EventId);

            bool isUpdate = (this.Mode == enMode.update);
            clsApplicationEmailLogs emailLog = null;
            bool isDatabaseSaved = false;

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    // 1. حفظ التذكرة أولاً
                    if (!await this.Save()) return false;

                   
                    emailLog = new clsApplicationEmailLogs
                    {
                        ReferenceID = this.Id,        // رقم التذكرة
                        LogType = "Event",            // تمييزها كفعالية
                        RecipientEmail = recipientEmail,
                        Subject =  "🎟️ Confirmed: " + this.TicketCode,
                        Body = _GenerateEmailTemplate(userName, eventDetails),
                  
                        Status = "Pending"
                    };

                    // 3. حفظ سجل الإيميل في الجدول الجديد
                    if (!await emailLog.Save()) return false;

                    scope.Complete();
                    isDatabaseSaved = true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            // 4. إرسال الإيميل فعلياً بعد نجاح المعاملة (Transaction)
            if (isDatabaseSaved && emailLog != null)
            {
                await emailService.SendEventConfirmationAsync(emailLog);
            }

            return isDatabaseSaved;
        }

        private string _GenerateEmailTemplate(string userName, clsEvents eventDetails)
        {
            // Design Configuration
            string primaryColor = "#4F46E5"; // Indigo Modern
            string secondaryColor = "#111827"; // Dark Gray
            string accentColor = "#6366F1"; // Light Indigo

            return $@"
    <div style='font-family: ""Inter"", ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; background-color: #f4f7fa; padding: 40px 20px; line-height: 1.6; direction: ltr; text-align: left;'>
        <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 10px 25px rgba(0,0,0,0.05);'>
            
            <div style='background: linear-gradient(135deg, {primaryColor} 0%, {accentColor} 100%); padding: 40px 30px; text-align: center; color: #ffffff;'>
                <div style='font-size: 50px; margin-bottom: 10px;'>🎟️</div>
                <h1 style='margin: 0; font-size: 28px; font-weight: 800; letter-spacing: -0.5px;'>Your Ticket is Ready!</h1>
                <p style='margin-top: 10px; opacity: 0.9; font-size: 16px;'>We're excited to have you join us.</p>
            </div>

            <div style='padding: 40px 35px;'>
                <p style='font-size: 18px; color: {secondaryColor}; margin-bottom: 20px;'>Hi <strong>{userName}</strong>,</p>
                <p style='color: #4b5563; font-size: 15px;'>Great news! Your booking has been confirmed. Below are your official ticket details. Please keep this email handy for check-in.</p>

                <div style='background-color: #ffffff; border: 1px solid #e5e7eb; border-radius: 12px; padding: 25px; margin: 30px 0; position: relative;'>
                    <div style='border-left: 4px solid {primaryColor}; padding-left: 15px;'>
                        <table style='width: 100%; border-collapse: collapse;'>
                            <tr>
                                <td style='padding: 10px 0; color: #9ca3af; font-size: 13px; text-transform: uppercase; letter-spacing: 1px;'>Event Name</td>
                                <td style='padding: 10px 0; text-align: right; color: {secondaryColor}; font-weight: 700; font-size: 16px;'>{eventDetails?.Title ?? "Premium Event"}</td>
                            </tr>
                            <tr>
                                <td style='padding: 10px 0; color: #9ca3af; font-size: 13px; text-transform: uppercase; letter-spacing: 1px;'>Ticket Code</td>
                                <td style='padding: 10px 0; text-align: right; color: {primaryColor}; font-weight: 800; font-family: ""Courier New"", monospace; font-size: 20px;'>{this.TicketCode}</td>
                            </tr>
                            <tr>
                                <td style='padding: 10px 0; color: #9ca3af; font-size: 13px; text-transform: uppercase; letter-spacing: 1px;'>Order Date</td>
                                <td style='padding: 10px 0; text-align: right; color: {secondaryColor}; font-size: 15px;'>{this.PurchaseDate.ToString("MMM dd, yyyy | hh:mm tt")}</td>
                            </tr>
                        </table>
                    </div>
                </div>

             
            </div>

                  <div> style='background-color: #f9fafb; padding: 30px; text-align: center; border-top: 1px solid #f3f4f6;'>
                <p style='margin: 0; font-size: 13px; color: #6b7280;'>
                   Refrence ID: {this.Id} <br>
                     </div>

            <div style='background-color: #f9fafb; padding: 30px; text-align: center; border-top: 1px solid #f3f4f6;'>
                <p style='margin: 0; font-size: 13px; color: #6b7280;'>
                    Need help? <a href=""mailto:maryamalobaidi107@gmail.com"" style=""color: #4F46E5; text-decoration: none;"">Contact Support</a><br><br>
                    <strong>CoworkSpace Team</strong><br>
                    123 Innovation Way, Tech City, 2026<br>
                    <span style='color: #9ca3af; font-size: 11px; margin-top: 10px; display: block;'>&copy; 2026 CoworkSpace Inc. All rights reserved.</span>
                </p>
            </div>
        </div>
    </div>";
        }

        public static async Task<bool> Delete(int Id)
        {
            return await clsEventTicketsData.DeleteEventTickets(Id);
        }
    }
}