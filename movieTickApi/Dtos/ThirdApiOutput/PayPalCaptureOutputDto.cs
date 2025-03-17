namespace movieTickApi.Dtos.ThirdApiOutput
{
        public class PayPalCaptureOutputDto
        {
                public string Id { get; set; }
                public string Status { get; set; }
                public List<PurchaseUnitDto> Purchase_units { get; set; }
        }


        public class PurchaseUnitDto
        {
                public PaymentDto Payments { get; set; }
        }

        public class PaymentDto
        {
                public List<CaptureDto> Captures { get; set; }
        }

        public class CaptureDto
        {
                public AmountDto Amount { get; set; }
                public DateTime Create_time { get; set; }
                public DateTime Update_time { get; set; }
        }

        public class AmountDto
        {
                public string Currency_code { get; set; }
                public string Value { get; set; }
        }
}
