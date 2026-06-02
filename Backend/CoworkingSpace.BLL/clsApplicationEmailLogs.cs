
using CoworkingSpace.DAL;
using CoworkingSpace.Models;
using System.Data;

namespace CoworkingSpace.BLL
{
    public class clsApplicationEmailLogs
    {
        public enum enMode { addNew = 0, update = 1 }
        public enMode Mode = enMode.addNew;



        public int LogID { get; set; }
        public int ReferenceID { get; set; }
        public string LogType { get; set; }
        public string RecipientEmail { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public DateTime SentDate { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
        public clsApplicationEmailLogs()
        {
            this.LogID = -1;
            this.ReferenceID = -1;
            this.LogType = "";
            this.RecipientEmail = "";
            this.Subject = "";
            this.Body = "";
            this.SentDate = DateTime.Now;
            this.Status = "";
            this.ErrorMessage = "";
            this.Mode = enMode.addNew;
        }

        private clsApplicationEmailLogs(applicationEmailLogsModel model)
        {
            this.LogID = (int)model.LogID;
            this.ReferenceID = model.ReferenceID;
            this.LogType = model.LogType;
            this.RecipientEmail = model.RecipientEmail;
            this.Subject = model.Subject;
            this.Body = model.Body;
            this.SentDate = model.SentDate;
            this.Status = model.Status;
            this.ErrorMessage = model.ErrorMessage;

            this.Mode = enMode.update;
        }

        private async Task<bool> _AddNewApplicationEmailLogs()
        {
            applicationEmailLogsModel model= new applicationEmailLogsModel
            {
                ReferenceID = this.ReferenceID,
                LogType = this.LogType,
                RecipientEmail = this.RecipientEmail,
                Subject = this.Subject,
                Body = this.Body,
                SentDate = this.SentDate,
                Status = this.Status,
                ErrorMessage = this.ErrorMessage
            };
            // Call DataAccess Layer
            this.LogID = (int)await clsApplicationEmailLogsData.AddNewApplicationEmailLogs(model);
            return (this.LogID != -1);
        }

        public static Task<bool> Delete(int LogID)
        {
            // Call DataAccess Layer
            return clsApplicationEmailLogsData.DeleteApplicationEmailLogs(LogID);
        }

        public static clsApplicationEmailLogs Find(int LogID)
        {
            // Call DataAccess Layer
           applicationEmailLogsModel model= new applicationEmailLogsModel();

            bool IsFound = clsApplicationEmailLogsData.FindByID(LogID, model);
            if (IsFound)
                return new clsApplicationEmailLogs(model);
            return null;
        }

  

        public static async Task<List<applicationEmailLogsModel>> GetAllApplicationEmailLogs()
        {
            return await clsApplicationEmailLogsData.GetAllApplicationEmailLogs();
        }

        public async Task<bool> Save()
        {
            switch (Mode)
            {
                case enMode.addNew:
                    Mode = enMode.update;
                    return await _AddNewApplicationEmailLogs();
                case enMode.update:
                    return await _UpdateApplicationEmailLogs();
            }
            return false;
        }

        private async Task<bool> _UpdateApplicationEmailLogs()
        {
            applicationEmailLogsModel model = new applicationEmailLogsModel
            {
                LogID = this.LogID,
                ReferenceID = this.ReferenceID,
                LogType = this.LogType,
                RecipientEmail = this.RecipientEmail,
                Subject = this.Subject,
                Body = this.Body,
                SentDate = this.SentDate,
                Status = this.Status,
                ErrorMessage = this.ErrorMessage
            };
            // Call DataAccess Layer
            return await clsApplicationEmailLogsData.UpdateApplicationEmailLogs(model);
        }

    }

}
