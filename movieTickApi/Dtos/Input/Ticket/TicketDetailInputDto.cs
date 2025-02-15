using movieTickApi.Dtos.Output.Ticket;
using System.ComponentModel.DataAnnotations;

namespace movieTickApi.Dtos.Input.Ticket
{
    public class TicketDetailInputDto
    {
        [Required(ErrorMessage = "MovieId 為必填")]
        public int MovieId { get; set; }

        [Required(ErrorMessage = "TicketDateTime 為必填")]
        public DateTime TicketDateTime { get; set; }

        [Required(ErrorMessage = "語言代碼為必填")]
        [StringLength(10, ErrorMessage = "語言代碼長度不能超過 10 個字元")]
        public string TicketLanguageCode { get; set; }

        [Required(ErrorMessage = "語言名稱為必填")]
        public string TicketLanguageName { get; set; }

        [Required(ErrorMessage = "票券類別不可為空")]
        [MinLength(1, ErrorMessage = "至少需要一張票")]
        public List<TicketCategoryOutputDto> TicketCategory { get; set; }
    }
}
