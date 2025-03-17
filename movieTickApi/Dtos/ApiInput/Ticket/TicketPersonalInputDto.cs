using System.ComponentModel.DataAnnotations;

namespace movieTickApi.Dtos.Input.Ticket
{
        public class TicketPersonalInputDto
        {
                [Required(ErrorMessage = "PageIndex不可為空")]
                public int PageIndex { get; set; }

                [Required(ErrorMessage = "PageSize不可為空")]
                public int PageSize { get; set; }
        }
}
