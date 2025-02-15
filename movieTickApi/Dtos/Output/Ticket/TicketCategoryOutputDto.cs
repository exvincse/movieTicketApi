using System.ComponentModel.DataAnnotations;

namespace movieTickApi.Dtos.Output.Ticket
{
    public class TicketCategoryDto
    {
        public string CategoryCode { get; set; }
        public string CategoryName { get; set; }
        public int Cost { get; set; }
    }

    public class TicketCategoryOutputDto
    {
        [Required(ErrorMessage = "票種代碼為必填")]
        public string CategoryCode { get; set; }

        [Required(ErrorMessage = "票種名稱為必填")]
        public string CategoryName { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "座位列必須大於 0")]
        public int Column { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "座位號碼必須大於 0")]
        public int Seat { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "票價不能為負數")]
        public int Cost { get; set; }
    }
}
