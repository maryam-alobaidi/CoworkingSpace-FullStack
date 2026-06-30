using CoworkingSpace.BLL.Interfaces;
using CoworkingSpace.DAL;
using CoworkingSpace.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace CoworkingSpace.BLL
{
    public class clsSpaceBookings
    {
        public enum enMode { addNew = 0, update = 1 }
        public enMode Mode = enMode.addNew;

        // الخصائص الأساسية
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SpaceId { get; set; }
        public DateTime BookingDate { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public decimal TotalPrice { get; set; }
        public string BookingStatus { get; set; }
        public DateTime CreatedAt { get; set; }

        // الخصائص الجديدة المضافة للدفع
        public string PaymentStatus { get; set; }
        public string? TransactionId { get; set; }

        public clsSpaceBookings()
        {
            this.Id = -1;
            this.UserId = -1;
            this.SpaceId = -1;
            this.BookingDate = DateTime.Now;
            this.StartTime = "";
            this.EndTime = "";
            this.TotalPrice = 0.0m;
            this.BookingStatus = "Pending"; // الحالة الافتراضية للحجز
            this.CreatedAt = DateTime.Now;
            this.PaymentStatus = "Pending"; // الحالة الافتراضية للدفع
            this.Mode = enMode.addNew;
        }

        // كونسرتكتور خاص للتحويل من الموديل (Private Constructor)
        private clsSpaceBookings(spaceBookingsModel model)
        {
            this.Id = model.Id ?? -1;
            this.UserId = model.UserId;
            this.SpaceId = model.SpaceId;
            this.BookingDate = model.BookingDate;
            this.StartTime = model.StartTime;
            this.EndTime = model.EndTime;
            this.TotalPrice = model.TotalPrice;
            this.BookingStatus = model.BookingStatus;
            this.CreatedAt = model.CreatedAt;
            this.PaymentStatus = model.PaymentStatus ?? "Pending";
            this.TransactionId = model.TransactionId;

            this.Mode = enMode.update;
        }

        // دالة مساعدة لتقليل تكرار الكود عند التحويل للموديل
        private spaceBookingsModel _MapToModel()
        {
            return new spaceBookingsModel
            {
                Id = this.Id,
                UserId = this.UserId,
                SpaceId = this.SpaceId,
                BookingDate = this.BookingDate,
                StartTime = this.StartTime,
                EndTime = this.EndTime,
                TotalPrice = this.TotalPrice,
                BookingStatus = this.BookingStatus,
                CreatedAt = this.CreatedAt,
                PaymentStatus = this.PaymentStatus,
                TransactionId = this.TransactionId
            };
        }

        private async Task<bool> _AddNewSpaceBookings()
        {
            this.Id = (int)await clsSpaceBookingsData.AddNewSpaceBookings(_MapToModel());
            return (this.Id != -1);
        }

        private async Task<bool> _UpdateSpaceBookings()
        {
            return await clsSpaceBookingsData.UpdateSpaceBookings(_MapToModel()) ?? false;
        }

        public static clsSpaceBookings Find(int Id)
        {
            spaceBookingsModel model = new spaceBookingsModel();
            bool IsFound = clsSpaceBookingsData.FindByID(Id, model);

            if (IsFound)
                return new clsSpaceBookings(model);

            return null;
        }

        public async Task<bool> Save()
        {
            switch (Mode)
            {
                case enMode.addNew:
                    if (await _AddNewSpaceBookings())
                    {
                        Mode = enMode.update;
                        return true;
                    }
                    return false;
                case enMode.update:
                    return await _UpdateSpaceBookings();
            }
            return false;
        }

        public static Task<bool> Delete(int Id)
        {
            return clsSpaceBookingsData.DeleteSpaceBookings(Id);
        }

        public static async Task<List<spaceBookingsModel>> GetAllSpaceBookings()
        {
            return await clsSpaceBookingsData.GetAllSpaceBookings();
        }


        public async Task<bool> SaveTicketWithEmailLog(string recipientEmail, string userName, IEmailService emailService)
        {

            var spaceDetails = clsSpaceBookings.Find(this.SpaceId);

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
                        ReferenceID = this.Id,
                        LogType = "Booking",
                        RecipientEmail = recipientEmail,
                        Subject = "🎟️ Confirmed: " + this.Id,
                        Body = _GenerateEmailTemplate(userName, spaceDetails),

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

        private string _GenerateEmailTemplate(string userName, clsSpaceBookings spaceDetails)
        {
            
            string primaryColor = "#11141a";    
            string secondaryColor = "#0d6efd";  
            

            return $@"
                    <div style='font-family: ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; background-color: #f4f6f9; padding: 40px 20px; line-height: 1.6; direction: ltr; text-align: left;'>
                        <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 10px 25px rgba(0,0,0,0.05);'>
                            
                           <div style='background-color: {primaryColor}; padding: 45px 30px; text-align: center; color: #ffffff; border-bottom: 4px solid {secondaryColor};'>
                    <div style='font-size: 50px; margin-bottom: 15px;'>🏢</div>
                    <h1 style='margin: 0; font-size: 28px; font-weight: 800; color: #ffffff; letter-spacing: -0.5px;'>Space Reserved!</h1>
                    <p style='margin-top: 12px; font-size: 15px; color: #ffffff; font-weight: 600; letter-spacing: 1px;'>VANTAGE COWORKING SPACE 👑</p>
                </div>
    
            <div style='padding: 40px 35px;'>
                <p style='font-size: 18px; color: {secondaryColor}; margin-bottom: 20px;'>Hi <strong>{userName}</strong> 👋,</p>
                <p style='color: #4b5563; font-size: 15px;'>Your booking has been confirmed successfully. Please find your reservation details below.</p>
    
                <div style='background-color: #ffffff; border: 1px solid #e5e7eb; border-radius: 12px; padding: 25px; margin: 30px 0; position: relative;'>
                    <div style='border-left: 4px solid {primaryColor}; padding-left: 15px;'>
                        <table style='width: 100%; border-collapse: collapse;'>
                            <tr>
                                <td style='padding: 10px 0; color: #9ca3af; font-size: 13px; text-transform: uppercase; letter-spacing: 1px;'>Booking Reference</td>
                                <td style='padding: 10px 0; text-align: right; color: {primaryColor}; font-weight: 800; font-family: ""Courier New"", monospace; font-size: 18px;'>#BK-{this.Id}</td>
                            </tr>
                            <tr>
                                <td style='padding: 10px 0; color: #9ca3af; font-size: 13px; text-transform: uppercase; letter-spacing: 1px;'>Space ID</td>
                                <td style='padding: 10px 0; text-align: right; color: {secondaryColor}; font-weight: 700;'>{this.SpaceId}</td>
                            </tr>
                            <tr>
                                <td style='padding: 10px 0; color: #9ca3af; font-size: 13px; text-transform: uppercase; letter-spacing: 1px;'>Date</td>
                                <td style='padding: 10px 0; text-align: right; color: {secondaryColor}; font-weight: 700;'>{this.BookingDate.ToString("MMM dd, yyyy")}</td>
                            </tr>
                            <tr>
                                <td style='padding: 10px 0; color: #9ca3af; font-size: 13px; text-transform: uppercase; letter-spacing: 1px;'>Time Slot</td>
                                <td style='padding: 10px 0; text-align: right; color: {secondaryColor}; font-weight: 700;'>{this.StartTime} - {this.EndTime}</td>
                            </tr>
                            <tr>
                                <td style='padding: 10px 0; color: #9ca3af; font-size: 13px; text-transform: uppercase; letter-spacing: 1px;'>Total Paid</td>
                                <td style='padding: 10px 0; text-align: right; color: {secondaryColor}; font-weight: 700; font-size: 16px;'>{this.TotalPrice:C}</td>
                            </tr>
                        </table>
                    </div>
                </div>
    
                <div style='background-color: #f8f9fa; border-radius: 8px; padding: 15px; border: 1px solid #e5e7eb; border-left: 4px solid {secondaryColor};'>
                    <p style='margin: 0; font-size: 13px; color: #555555;'>
                        <strong>Note:</strong> Please arrive 5 minutes before your start time. Show this email at the reception if requested.
                    </p>
                </div>
            </div>
    
            <div style='background-color: #11141a; padding: 30px; text-align: center; color: #a0a5b0; border-top: 1px solid #22252a;'>
                <p style='margin: 0; font-size: 13px; color: #a0a5b0;'>
                    Need help? <a href='mailto:maryamalobaidi107@gmail.com' style='color: {primaryColor}; text-decoration: none; font-weight: bold;'>Contact Support</a><br><br>
                    <strong>Vantage Coworking Team</strong><br>
                    <span style='color: {primaryColor}; font-weight: 600;'>Where Inspiration Meets Productivity</span><br>
                    <span style='color: #6b7280; font-size: 11px; margin-top: 15px; display: block;'>&copy; {DateTime.Now.Year} Vantage Space Inc. All rights reserved.</span>
                </p>
            </div>
        </div>
    </div>";
        }




        public static async Task<List<string>> GetBookedSlots(int spaceId, DateTime bookingDate)
        {
           
            List<string> bookedSlots = await clsSpaceBookingsData.GetBookedSlots(spaceId, bookingDate);

          
            return bookedSlots;
        }


        public static async Task<List<spaceBookingsModel>> getUserBooking(int id)
        {
            return await clsSpaceBookingsData.getUserBooking(id);
        }

    }
}