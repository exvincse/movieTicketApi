namespace movieTickApi.Dtos.ApiOutput.PayPal
{
        public class CaptureOutputDto
        {
                public required string OrderId { get; set; }
                public required string Status { get; set; }
                public required string Amount { get; set; }
                public DateTime CreateTime { get; set; }
        }
}
