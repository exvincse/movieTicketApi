namespace movieTickApi.Models.Ticket
{
    public class TicketCategory
    {
        public int Id { get; set; }
        public string CategoryCode { get; set; }
        public string CategoryName { get; set; }
        public int Cost { get; set; }
    }
}