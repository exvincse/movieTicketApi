namespace movieTickApi.Models
{
        public class SmtpSettings
        {
                public string Host { get; set; } = "smtp.gmail.com";
                public int Port { get; set; } = 587;
                public string SenderEmail { get; set; }
                public string SenderName { get; set; }
                public string UserName { get; set; }
                public string Password { get; set; }
        }

        public class EmailRequest
        {
                public string ToName { get; set; } // 收件人名稱
                public string ToEmail { get; set; } // 收件人 Email
                //public string Subject { get; set; } // 郵件標題
                //public string Body { get; set; } // 郵件內容
        }
}
