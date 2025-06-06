﻿namespace movieTickApi.Models.Users
{
        public class SmtpSettings
        {
                public string Host { get; set; } = "smtp.gmail.com";
                public int Port { get; set; } = 587;
                public required string SenderEmail { get; set; }
                public required string SenderName { get; set; }
                public required string UserName { get; set; }
                public required string Password { get; set; }
        }

        public class EmailRequest
        {
                public string ToName { get; set; }
                public string ToEmail { get; set; }
                public string Subject { get; set; }
                public string Body { get; set; }
        }
}
