namespace movieTickApi.Dtos.Output.Ticket
{
        public class TicketCategoryDto
        {
                public required string CategoryCode { get; set; }
                public required string CategoryName { get; set; }
                public int Cost { get; set; }
        }
}
