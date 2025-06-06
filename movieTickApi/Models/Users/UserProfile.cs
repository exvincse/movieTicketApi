﻿namespace movieTickApi.Models.Users
{
        public class UserProfile
        {
                public Guid Id { get; set; }
                public required string Name { get; set; }
                public Guid UserId { get; set; }
                public int UserNo { get; set; }
                public string? Email { get; set; }
                public string? CountyCode { get; set; }
                public string? DistrictCode { get; set; }
                public string? PostalCode { get; set; }
                public string? Address { get; set; }
                public string? SexCode { get; set; }
                public DateTime? Birthday { get; set; }
                public DateTime CreateDateTime { get; set; }
                public DateTime ModifyDateTime { get; set; }
                public User User { get; set; }
        }
}
