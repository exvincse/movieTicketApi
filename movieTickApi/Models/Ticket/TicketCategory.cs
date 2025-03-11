namespace movieTickApi.Models.Ticket
{
        public class TicketCategory
        {
                public int Id { get; set; }
                public required string CategoryCode { get; set; }
                public required string CategoryName { get; set; }
                public int Cost { get; set; }
        }
}