namespace movieTickApi.Dtos.Input.Users
{
    public class UserProfileInputDto
    {
        public int UserNo { get; set; }
        public string Name { get; set; }
        public string? Email { get; set; }
        public string? CountyCode { get; set; }
        public string? DistrictCode { get; set; }
        public string? PostalCode { get; set; }
        public string? Address { get; set; }
        public string? SexCode { get; set; }
        public DateTime? Birthday { get; set; }
    }
}
