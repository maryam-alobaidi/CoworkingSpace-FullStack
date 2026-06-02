using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoworkingSpace.Models
{
    public class applicationEmailLogsModel
    {
        public int? LogID { get; set; }
        public int ReferenceID { get; set; }
        public string LogType { get; set; }
        public string RecipientEmail { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public DateTime SentDate { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}
