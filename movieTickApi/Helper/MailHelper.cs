using movieTickApi.Models;
using MimeKit;
using MailKit.Net.Smtp;
using movieTickApi.Models.Users;

namespace movieTickApi.Helper
{
    public class MailHelper
        {
                private readonly SmtpSettings _smtpSettings;
                private readonly WebDbContext _context;
                private readonly IHttpContextAccessor _httpContextAccessor;

                public MailHelper(SmtpSettings smtpSettings, WebDbContext context, IHttpContextAccessor httpContextAccessor)
                {
                        _smtpSettings = smtpSettings;
                        _context = context;
                        _httpContextAccessor = httpContextAccessor;
                }

                public class EmailMessageResult
                {
                        public MimeMessage Message { get; set; }
                        public string Otp { get; set; }
                }

                public async Task<bool> SendMail(EmailRequest emailRequest)
                {
                        // 發信前刪除跟這個email相關驗證碼
                        var mail = _context.OtpVerification.Where(x => x.Email == emailRequest.ToEmail).ToList();
                        if (mail.Count > 0)
                        {
                                _context.OtpVerification.RemoveRange(mail);
                               await _context.SaveChangesAsync();
                        }

                        var message = CreateEmailMessage(emailRequest);
                        var client = new SmtpClient();

                        try
                        {
                                // 連接到 Gmail SMTP 伺服器
                                await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, MailKit.Security.SecureSocketOptions.StartTls);

                                // 認證 Gmail 帳戶
                                await client.AuthenticateAsync(_smtpSettings.UserName, _smtpSettings.Password);

                                // 發送郵件
                                await client.SendAsync(message.Message);

                                // 儲存驗證碼
                                var otp = new OtpVerification
                                {
                                        Id = Guid.NewGuid(),
                                        Email = emailRequest.ToEmail,
                                        Otp = message.Otp,
                                        ExpirationTime = DateTime.UtcNow.AddMinutes(4),
                                        IsUsed = false,
                                        CreatedAt = DateTime.UtcNow,
                                        UpdatedAt = DateTime.UtcNow
                                };

                                _context.OtpVerification.Add(otp);
                                await _context.SaveChangesAsync();

                                //_httpContextAccessor.HttpContext?.Session.SetString("Email", otp.Email);

                                return true;
                        }
                        catch
                        {
                                return false;
                        }
                        finally
                        {
                                await client.DisconnectAsync(true);
                        }
                }


                public EmailMessageResult CreateEmailMessage(EmailRequest emailRequest)
                {
                        // 建立郵件訊息
                        var message = new MimeMessage();
                        message.From.Add(new MailboxAddress(_smtpSettings.SenderName, _smtpSettings.SenderEmail));
                        message.To.Add(new MailboxAddress(emailRequest.ToName, emailRequest.ToEmail));
                        message.Subject = "驗證Email";

                        //var message = new MimeMessage();
                        //message.From.Add(new MailboxAddress(_smtpSettings.SenderName, _smtpSettings.SenderEmail));
                        //message.To.Add(new MailboxAddress("yang", "exvincse@gmail.com"));
                        //message.Subject = "驗證Email";

                        var otp = new Random().Next(100000, 999999).ToString();

                        // 設定郵件內容
                        var bodyBuilder = new BodyBuilder
                        {
                                HtmlBody = $"驗證碼: {otp}. 驗證碼4分鐘後到期。"
                        };
                        message.Body = bodyBuilder.ToMessageBody();

                        return new EmailMessageResult
                        {
                                Message = message,
                                Otp = otp
                        };
                }
        }
}
