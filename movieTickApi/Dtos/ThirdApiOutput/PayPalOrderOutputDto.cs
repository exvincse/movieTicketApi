namespace movieTickApi.Dtos.ApiOutput.PayPal
{
        public class PayPalOrderOutputDto
        {
                public DateTime Create_Time { get; set; }
                public string Id { get; set; }
                public string Intent { get; set; }
                public string Status { get; set; }
                public List<Link> Links { get; set; }
                public List<PurchaseUnit> Purchase_Units { get; set; }
        }

        public class Link
        {
                public string Href { get; set; }
                public string Rel { get; set; }
                public string Method { get; set; }
        }

        public class PurchaseUnit
        {
                public string Reference_Id { get; set; }
                public Amount Amount { get; set; }
        }

        public class Amount
        {
                public string Currency_Code { get; set; }
                public string Value { get; set; }
        }
}
