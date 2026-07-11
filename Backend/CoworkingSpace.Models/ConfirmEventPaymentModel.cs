using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoworkingSpace.Models
{
    public class ConfirmEventPaymentModel
    {
        public string TicketIds { get; set; }
        public int Quantity { get; set; }
        public string TransactionId { get; set; }
    }
}
