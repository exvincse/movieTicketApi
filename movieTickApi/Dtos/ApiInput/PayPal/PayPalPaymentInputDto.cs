using System.ComponentModel.DataAnnotations;

namespace movieTickApi.Dtos.ApiInput.PayPal
{
        public class PayPalCheckOrderInputDto
        {
                [Required(ErrorMessage = "OrderId不可為空")]
                public required string OrderId { get; set; }
        }
}
