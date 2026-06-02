using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoworkingSpace.Models
{
    public class PaymentRequestModel
    {
        public int? PaymentID { get; set; }
        public int ReferenceID { get; set; }
        public string ReferenceType { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }

        // إضافة علامة الاستفهام تجعل الحقول اختيارية وتسمح بالـ NULL
        public string? PaymentMethod { get; set; }
        public string? TransactionID { get; set; }
        public string? PaymentStatus { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}