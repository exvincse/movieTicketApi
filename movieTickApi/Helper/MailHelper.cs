﻿using MailKit.Net.Smtp;
using MimeKit;
using movieTickApi.Models;
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
                        public required MimeMessage Message { get; set; }
                        public required string Otp { get; set; }
                }

                public async Task<bool> SendMail(EmailRequest emailRequest)
                {
                        var message = CreateEmailMessage(emailRequest);
                        var client = new SmtpClient();

                        try
                        {
                                // 連接到 Gmail SMTP 伺服器
                                await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, MailKit.Security.SecureSocketOptions.StartTls);

                                // 認證 Gmail 帳戶
                                await client.AuthenticateAsync(_smtpSettings.UserName, _smtpSettings.Password);

                                // 發送郵件
                                await client.SendAsync(message);

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


                public MimeMessage CreateEmailMessage(EmailRequest emailRequest)
                {
                        // 建立郵件訊息
                        var message = new MimeMessage();
                        message.From.Add(new MailboxAddress(_smtpSettings.SenderName, _smtpSettings.SenderEmail));
                        message.To.Add(new MailboxAddress(emailRequest.ToName, emailRequest.ToEmail));
                        message.Subject = emailRequest.Subject;

                        // 設定郵件內容
                        var bodyBuilder = new BodyBuilder
                        {
                                HtmlBody = emailRequest.Body
                        };

                        message.Body = bodyBuilder.ToMessageBody();

                        return message;
                }
        }
}
