namespace movieTickApi.Models.Ticket
{
        public class TicketDetailMain
        {
                public Guid Id { get; set; }
                public int MovieId { get; set; }
                public required string MovieName { get; set; }
                public DateTime TicketDate { get; set; }
                public required string TicketLanguageCode { get; set; }
                public required string TicketLanguageName { get; set; }
                public int TicketTotalPrice { get; set; }
                public int CreateUserNo { get; set; }
                public DateTime CreateDateTime { get; set; }
                public required string CreateOrderId { get; set; }
                public int TicketStatusId { get; set; }
                public ICollection<TicketDetail> TicketDetail { get; set; }
                public TicketPaymentStatus? TicketPaymentStatus { get; set; }
        }
}