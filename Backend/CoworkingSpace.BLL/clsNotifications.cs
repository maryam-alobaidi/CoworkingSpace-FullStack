using CoworkingSpace.DAL;
using CoworkingSpace.Models;
using Org.BouncyCastle.Pqc.Crypto.Lms;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoworkingSpace.BLL
{
    public class clsNotifications
    {
        public enum enMode { addNew = 0, update = 1 }
        public enMode Mode = enMode.addNew;



        public int NotificationID { get; set; }
        public int UserID { get; set; }
        public string Title { get; set; }
        public string? Message { get; set; }
        public string? NotificationType { get; set; }
        public string? TargetURL { get; set; }
        public bool? IsRead { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public clsNotifications()
        {
            this.NotificationID = -1;
            this.UserID = -1;
            this.Title = "";
            this.Message = "";
            this.NotificationType = "";
            this.TargetURL = "";
            this.IsRead = false;
            this.CreatedAt = DateTime.Now;
            this.ReadAt =null;
            this.Mode = enMode.addNew;
        }



        public async Task<bool> AddNewNotifications()
        {
            notificationsModel model = new notificationsModel
            {
                NotificationID = this.NotificationID,
                UserID = this.UserID,
                Title = this.Title,
                Message = this.Message,
                NotificationType = this.NotificationType,
                TargetURL = this.TargetURL,
                IsRead = this.IsRead,
                CreatedAt = this.CreatedAt,
                ReadAt = this.ReadAt

            };


            int? newId = await clsNotificationsData.AddNewNotifications(model);


            if (newId.HasValue && newId.Value != -1)
            {
                this.NotificationID = newId.Value;
                this.Mode = enMode.update; 
                return true;
            }

            return false;
        }
        public static async Task<bool> MarkNotificationAsRead(int NotificationID)
        {
            return await clsNotificationsData.MarkNotificationAsRead(NotificationID);
        }


        public static async Task<List<notificationsModel>> GetAllNotificationsByUserID(int UserId)
        {
            List<notificationsModel> modelsList = await clsNotificationsData.GetAllNotificationsByUserID(UserId);
            if (modelsList != null)
            {
                return modelsList;
            }
            return null;
        }
    }
}
