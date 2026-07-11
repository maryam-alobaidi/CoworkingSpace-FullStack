using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoworkingSpace.Models
{
     public class RecentSpaceReservationDTO
    {
        public int BookingId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string SpaceName { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; } 
        public decimal Price { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
