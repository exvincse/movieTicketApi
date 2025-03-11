namespace movieTickApi.Models.Ticket
{
        public class TicketLanguage
        {
                public int Id { get; set; }
                public required string CategoryCode { get; set; }
                public required string CategoryName { get; set; }
        }
}