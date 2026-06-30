using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoworkingSpace.Models
{
    public class eventTicketModel
    {
        public int? Id { get; set; }
        public int EventId { get; set; }
        public int UserId { get; set; }
        public string? TicketCode { get; set; }
        public DateTime? PurchaseDate { get; set; }

       

        // حالة الدفع: Pending, Paid, Failed
        public string? PaymentStatus { get; set; }

        // رقم العملية القادم من Stripe أو البنك
        public string? TransactionId { get; set; }

        public string? EventTitle { get; set; }    
        public decimal? TotalPrice { get; set; }  
    }
}