namespace movieTickApi.Dtos.Output.Users
{
    public class LocationOutputDto
    {
        public string CountyName { get; set; }
        public string CountyCode { get; set; }
        public List<DistrictOutputDto> District { get; set; }
    }

    public class DistrictOutputDto
    {
        public string DistrictName { get; set; }
        public string DistrictCode { get; set; }
        public string PostalCode { get; set; }
    }
}
