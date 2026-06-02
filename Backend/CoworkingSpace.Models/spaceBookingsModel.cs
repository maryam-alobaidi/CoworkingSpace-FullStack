using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoworkingSpace.Models
{
    public class spaceBookingsModel
    {
        public int? Id { get; set; }
        public int UserId { get; set; }
        public int SpaceId { get; set; }
        public DateTime BookingDate { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public decimal TotalPrice { get; set; }
        public string BookingStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        /// <summary>
        /// حالة الدفع (Pending, Completed, Failed, Refunded)
        /// </summary>
        public string? PaymentStatus { get; set; }

        /// <summary>
        /// الرقم المرجعي للعملية القادم من Stripe
        /// </summary>
        public string? TransactionId { get; set; }
    }
}
