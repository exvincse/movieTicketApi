using System.ComponentModel.DataAnnotations;

namespace movieTickApi.Dtos.Input.Ticket
{
        public class TicketDetailInputDto
        {
                [Required(ErrorMessage = "MovieId 為必填")]
                public int MovieId { get; set; }

                [Required(ErrorMessage = "MovieName 為必填")]
                public required string MovieName { get; set; }

                [Required(ErrorMessage = "TicketDateTime 為必填")]
                public DateTime TicketDateTime { get; set; }

                [Required(ErrorMessage = "語言代碼為必填")]
                [StringLength(10, ErrorMessage = "語言代碼長度不能超過 10 個字元")]
                public required string TicketLanguageCode { get; set; }

                [Required(ErrorMessage = "語言名稱為必填")]
                public required string TicketLanguageName { get; set; }

                [Required(ErrorMessage = "票券類別不可為空")]
                [MinLength(1, ErrorMessage = "至少需要一張票")]
                public required List<TicketCategoryInputDto> TicketCategory { get; set; }

                [Required(ErrorMessage = "TotalCost不可為空")]
                public int TotalCost { get; set; }
        }

        public class TicketCategoryInputDto
        {
                [Required(ErrorMessage = "票種代碼為必填")]
                public required string CategoryCode { get; set; }

                [Required(ErrorMessage = "票種名稱為必填")]
                public required string CategoryName { get; set; }

                [Range(1, int.MaxValue, ErrorMessage = "座位列必須大於 0")]
                public int Column { get; set; }

                [Range(1, int.MaxValue, ErrorMessage = "座位號碼必須大於 0")]
                public int Seat { get; set; }

                [Range(0, int.MaxValue, ErrorMessage = "票價不能為負數")]
                public int Cost { get; set; }
        }
}
