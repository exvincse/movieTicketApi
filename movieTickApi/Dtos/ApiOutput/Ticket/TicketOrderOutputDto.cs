namespace movieTickApi.Dtos.ApiOutput.PayPal
{
        public class TicketOrderOutputDto
        {
                public string CreateTime { get; set; }
                public string OrderId { get; set; }
                public string Status { get; set; }
                public string Link { get; set; }
                public string Amounts { get; set; }
        }
}
