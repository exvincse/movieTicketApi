namespace movieTickApi.Models.Users
{
        public class Locations
        {
                public int Id { get; set; }
                public required string CountyName { get; set; }
                public required string CountyCode { get; set; }
                public required string DistrictName { get; set; }
                public required string DistrictCode { get; set; }
                public required string PostalCode { get; set; }
        }
}