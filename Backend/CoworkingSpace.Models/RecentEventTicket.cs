using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoworkingSpace.Models
{
    public class RecentEventTicket
    {
        public int TicketId { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string EventName { get; set; } = string.Empty;

        public DateTime PurchaseDate { get; set; }

        public decimal Price { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}
