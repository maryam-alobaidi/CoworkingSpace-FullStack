using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoworkingSpace.Models
{
    public class notificationsModel
    {
        public int? NotificationID { get; set; }
        public int UserID { get; set; }
        public string Title { get; set; }
        public string? Message { get; set; }
        public string? NotificationType { get; set; }
        public string? TargetURL { get; set; }
        public bool? IsRead { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
    }
}
