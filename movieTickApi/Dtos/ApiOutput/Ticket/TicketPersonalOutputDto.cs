namespace movieTickApi.Dtos.Input.Ticket
{
        public class TicketPersonalOutputDto
        {
                public required string MovieName { get; set; }
                public DateTime TicketDate { get; set; }
                public required string TicketLanguageName { get; set; }
                public required string TicketStatusName { get; set; }
                public required List<TicketPersonalItemOutputDto> TicketPersonalItem { get; set; }
        }

        public class TicketPersonalItemOutputDto
        {
                public required string TicketCategoryName { get; set; }
                public int TicketColumn { get; set; }
                public int TicketSeat { get; set; }
                public int TicketMoney { get; set; }
        }
}
