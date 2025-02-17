namespace movieTickApi.Dtos.Input.Ticket
{
    public class TicketPersonalOutputDto
    {
                public DateTime TicketDate { get; set; }
                public string TicketLanguageName { get; set; }
                public string TicketCategoryName { get; set; }
                public int Column { get; set; }
                public int Seat { get; set; }
        }
}
