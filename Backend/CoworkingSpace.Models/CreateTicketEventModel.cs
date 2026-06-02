using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoworkingSpace.Models
{
    public class CreateTicketEventModel
    {
        public int? Id { get; set; }
        public int EventId { get; set; }
        public int UserId { get; set; }
    }
}
