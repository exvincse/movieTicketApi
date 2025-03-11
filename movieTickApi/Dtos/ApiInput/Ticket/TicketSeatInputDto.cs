using System.ComponentModel.DataAnnotations;

namespace movieTickApi.Dtos.Input.Ticket
{
        public class TicketSeatInputDto
        {
                [Required(ErrorMessage = "MovieId不可為空")]
                public int MovieId { get; set; }

                [Required(ErrorMessage = "MovieTicketDateTime不可為空")]
                public DateTime MovieTicketDateTime { get; set; }

                [Required(ErrorMessage = "TicketLanguageCode不可為空")]
                public required string TicketLanguageCode { get; set; }
        }
}
