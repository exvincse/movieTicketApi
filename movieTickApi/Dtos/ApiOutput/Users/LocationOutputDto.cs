namespace movieTickApi.Dtos.Output.Users
{
        public class LocationOutputDto
        {
                public required string CountyName { get; set; }
                public required string CountyCode { get; set; }
                public required List<DistrictOutputDto> District { get; set; }
        }

        public class DistrictOutputDto
        {
                public required string DistrictName { get; set; }
                public required string DistrictCode { get; set; }
                public required string PostalCode { get; set; }
        }
}
