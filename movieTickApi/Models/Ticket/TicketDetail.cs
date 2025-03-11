namespace movieTickApi.Models.Ticket
{
        public class TicketDetail
        {
                public Guid Id { get; set; }
                public Guid TicketDetailMainId { get; set; }
                public required string TicketCategoryCode { get; set; }
                public required string TicketCategoryName { get; set; }
                public int TicketColumn { get; set; }
                public int TicketSeat { get; set; }
                public int TicketMoney { get; set; }
                public TicketDetailMain? TicketDetailMain { get; set; }
        }
}