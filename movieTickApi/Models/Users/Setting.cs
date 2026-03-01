namespace movieTickApi.Models.Users
{
        public class Setting
        {
                public int Id { get; set; }
                public required string TmdbApiKey { get; set; }
                public required string PdAzureSubKey { get; set; }

        }
}
