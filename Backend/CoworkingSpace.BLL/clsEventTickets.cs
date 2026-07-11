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

        public string PaymentStatus { get; set; }
        public string? TransactionId { get; set; }

        public clsEventTickets()
        {
            this.Id = -1;
            this.EventId = -1;
            this.UserId = -1;
            this.TicketCode = GenerateUniqueTicketCode();
            this.PurchaseDate = DateTime.Now;
            this.PaymentStatus = "Pending"; 
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

        public static async Task<eventTicketModel> Find(int Id)
        {
           
                var result = await clsEventTicketsData.FindByID(Id);
            if (result == null)
                return null;
            return result;
        }

        public static async Task<clsEventTickets?> FindWithReturnclass(int Id)
        {

            var ticketModel = await clsEventTicketsData.FindByID(Id);
            if (ticketModel == null)
                return null;
            return new clsEventTickets
            {
                Id = ticketModel.Id ?? 0,
                EventId = ticketModel.EventId,
                UserId = ticketModel.UserId,
                TicketCode = ticketModel.TicketCode,
                PurchaseDate = ticketModel.PurchaseDate ?? DateTime.Now,
                PaymentStatus = ticketModel.PaymentStatus,
                TransactionId = ticketModel.TransactionId,
                Mode = enMode.update 
            };
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

            string primaryColor = "#11141a";
            string secondaryColor = "#0d6efd";


            return $@"
    <div style='font-family: ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; background-color: #f4f6f9; padding: 40px 20px; line-height: 1.6; direction: ltr; text-align: left;'>
        <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 10px 25px rgba(0,0,0,0.05);'>
            
            <div style='background-color: {primaryColor};  padding: 40px 30px; text-align: center; color: #ffffff; border-bottom: 5px solid {secondaryColor};'>
                <div style='font-size: 50px; margin-bottom: 10px;'>🎟️</div>
                <h1 style='margin: 0; font-size: 28px; font-weight: 800; letter-spacing: -0.5px;'>Your Ticket is Ready!</h1>
                <p style='margin-top: 10px; font-size: 16px;'>VANTAGE COWORKING SPACE 👑</p>
            </div>

            <div style='padding: 40px 35px;'>
                <p style='font-size: 18px; color: {secondaryColor}; margin-bottom: 20px;'>Hi <strong>{userName}</strong> 👋,</p>
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

            <div style='background-color: #11141a; padding: 30px; text-align: center; color: #a0a5b0; border-top: 1px solid #22252a;'>
                <p style='margin: 0 0 10px 0; font-size: 13px; color: #9ca3af;'>
                    Reference ID: <span style='color: #ffffff; font-weight: bold;'>{this.Id}</span>
                </p>
                <p style='margin: 0; font-size: 13px; color: #a0a5b0;'>
                    Need help? <a href=""mailto:maryamalobaidi107@gmail.com"" style=""color: {primaryColor}; text-decoration: none; font-weight: bold;"">Contact Support</a><br><br>
                    <strong>Vantage Coworking Team</strong><br>
                    <span style='color: {primaryColor}; font-weight: 600;'>Where Inspiration Meets Productivity</span><br>
                    <span style='color: #6b7280; font-size: 11px; margin-top: 15px; display: block;'>&copy; {DateTime.Now.Year} Vantage Space Inc. All rights reserved.</span>
                </p>
            </div>

        </div>
    </div>";
        }

        public static async Task<bool> Delete(int Id)
        {
            return await clsEventTicketsData.DeleteEventTickets(Id);
        }

        public static async Task<List<eventTicketModel>> GetTicketsByUserId(int UserId)
        {
            return await clsEventTicketsData.GetTicketsByUserId(UserId);
        }

        public static async Task<List<RecentEventTicket>> GetRecentEventTicket()
        {
            return await clsEventTicketsData.GetRecentEventTicket();
        }
    }
}