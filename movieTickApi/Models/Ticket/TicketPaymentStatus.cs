namespace movieTickApi.Models.Ticket
{
        public class TicketPaymentStatus
        {
                public int StatusId { get; set; }
                public required string StatusName { get; set; }
                public required ICollection<TicketDetailMain> TicketDetailMain { get; set; }
        }
}
