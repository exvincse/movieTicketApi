using System.ComponentModel.DataAnnotations;

namespace movieTickApi.Dtos.ApiInput.PayPal
{
        public class PayPalPaymentInputDto
        {
                [Required(ErrorMessage = "Total不可為空")]
                public int Total { get; set; }
        }
}
