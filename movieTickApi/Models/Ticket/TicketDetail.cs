namespace movieTickApi.Models.Ticket
{
    public class TicketDetail
    {
        public Guid Id { get; set; }
        public int MovieId { get; set; }
        public DateTime TicketDate { get; set; }
        public string TicketLanguageCode { get; set; }
        public string TicketLanguageName { get; set; }
        public string TicketCategoryCode { get; set; }
        public string TicketCategoryName { get; set; }
        public int TicketColumn { get; set; }
        public int TicketSeat { get; set; }
        public int TicketMoney { get; set; }
        public int CreateUserNo { get; set; }
        public DateTime CreateDateTime { get; set; }
    }
}