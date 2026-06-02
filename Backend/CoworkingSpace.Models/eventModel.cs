namespace CoworkingSpace.Models
{
    public class eventModel
    {
        public int? Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public decimal TicketPrice { get; set; }
        public int MaxAttendees { get; set; }
        public int AvailableSeats { get; set; }
    }
}
