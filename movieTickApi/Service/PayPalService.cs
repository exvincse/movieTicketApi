using Microsoft.EntityFrameworkCore;
using movieTickApi.Dtos.ThirdApiOutput;
using movieTickApi.Helper;
using movieTickApi.Models;
using movieTickApi.Models.Users;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace movieTickApi.Service
{
        public class PayPalService
        {
                private readonly IConfiguration _configuration;
                private readonly WebDbContext _context;
                private readonly IHttpClientFactory _httpClientFactory;
                private readonly MailHelper _mailHelper;
                private readonly IHttpContextAccessor _httpContextAccessor;
                private readonly string clientId = "AZF0Rv39g-VaTuQN4rJL-T7mQfBqWtVSWUiO8K95j8wt3_cTpq0eYvrxDIN0Es3HUfnA_zT9osV5Ioj3"; // Replace with your PayPal client ID
                private readonly string secret = "EO7GPIf3Ty3Tn6mh-4O7EQaAVTseNtVXIhMNhF6PTgVqlr_5Gd-_k-TNRNBvdQ-n1drL8ZJ4V466i1EW"; // Replace with your PayPal secret
                public readonly string baseUrl = "https://api-m.sandbox.paypal.com";

                public PayPalService(IConfiguration configuration, WebDbContext context, IHttpClientFactory httpClientFactory, MailHelper mailHelper, IHttpContextAccessor httpContextAccessor)
                {
                        _configuration = configuration;
                        _context = context;
                        _httpClientFactory = httpClientFactory;
                        _mailHelper = mailHelper;
                        _httpContextAccessor = httpContextAccessor;
                }

                public async Task<string> GetAccessTokenAsync()
                {
                        var client = _httpClientFactory.CreateClient();

                        var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{clientId}:{secret}"));
                        client.DefaultRequestHeaders.Add("Authorization", $"Basic {authValue}");

                        var requestBody = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

                        var response = await client.PostAsync($"{baseUrl}/v1/oauth2/token", requestBody);
                        if (!response.IsSuccessStatusCode)
                        {
                                throw new Exception("Failed to retrieve access token from PayPal API");
                        }

                        var responseBody = await response.Content.ReadAsStringAsync();
                        dynamic responseJson = JsonConvert.DeserializeObject(responseBody);

                        return responseJson.access_token;
                }

                public async Task<(string orderId, string approvalUrl)> CreatePayment(int totalCost)
                {
                        // 獲取 access token
                        var accessToken = await GetAccessTokenAsync();

                        var client = _httpClientFactory.CreateClient();
                        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                        // 創建 PayPal 訂單
                        var orderRequest = new
                        {
                                intent = "CAPTURE",
                                purchase_units = new[]
                                {
                                    new
                                    {
                                        amount = new
                                        {
                                            currency_code = "USD",
                                            value = totalCost.ToString()
                                        }
                                    }
                                 },
                                application_context = new
                                {
                                        return_url = _configuration["FrontHostUrl"] + "paypal-success",
                                        cancel_url = _configuration["FrontHostUrl"] + "paypal-error"
                                }
                        };

                        var content = new StringContent(JsonConvert.SerializeObject(orderRequest), Encoding.UTF8, "application/json");

                        var response = await client.PostAsync($"{baseUrl}/v2/checkout/orders", content);

                        if (!response.IsSuccessStatusCode)
                        {
                                return (null, null);
                        }

                        var responseBody = await response.Content.ReadAsStringAsync();
                        dynamic responseJson = JsonConvert.DeserializeObject(responseBody);

                        string orderId = responseJson.id;
                        string approvalUrl = responseJson.links[1].href;

                        return (orderId, approvalUrl);
                }

                public async Task<PayPalCaptureOutputDto> PostSuccessOrder(string OrderId)
                {
                        var accessToken = await GetAccessTokenAsync();
                        var client = _httpClientFactory.CreateClient();
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                        var content = new StringContent("{}", Encoding.UTF8, "application/json");
                        var response = await client.PostAsync($"{baseUrl}/v2/checkout/orders/{OrderId}/capture", content);

                        var responseBody = await response.Content.ReadAsStringAsync();
                        PayPalCaptureOutputDto responseJson = JsonConvert.DeserializeObject<PayPalCaptureOutputDto>(responseBody);

                        await _context.TicketDetailMain.Where(x => x.CreateOrderId == responseJson.Id).ExecuteUpdateAsync(setters => setters.SetProperty(t => t.TicketStatusId, 1));

                        string html = $@"
                                <!DOCTYPE html>
                                <html>
                                <body style=""font-family: Arial, sans-serif; background-color: #f9f9f9; padding: 20px;"">
                                  <div style=""max-width: 600px; margin: auto; background-color: #ffffff; border: 1px solid #ddd; border-radius: 8px; padding: 20px;"">
                                    <h2 style=""color: #333333; border-bottom: 2px solid #2660a9; padding-bottom: 10px;"">訂單明細</h2>

                                    <table style=""width: 100%; border-collapse: collapse; margin-top: 20px;"">
                                      <thead>
                                        <tr style=""background-color: #2660a9; color: white;"">
                                          <th style=""padding: 12px; border: 1px solid #ddd; text-align: right;"">票種</th>
                                          <th style=""padding: 12px; border: 1px solid #ddd; text-align: right;"">座位</th>
                                          <th style=""padding: 12px; border: 1px solid #ddd; text-align: right;"">單價</th>
                                        </tr>
                                      </thead>
                                      <tbody>
                        ";
                        var result = await _context.TicketDetailMain.Include(x => x.TicketDetail).Where(x => x.CreateOrderId == responseJson.Id).FirstOrDefaultAsync();
                        var items = result.TicketDetail.ToList();
                        foreach (var item in items)
                        {
                                html += $@"
                                        <tr>
                                                <td style=""padding: 10px; border: 1px solid #ddd; text-align: right;"">{item.TicketCategoryName}</td>
                                                <td style=""padding: 10px; border: 1px solid #ddd; text-align: right;"">{item.TicketColumn}排{item.TicketSeat}號</td>
                                                <td style=""padding: 10px; border: 1px solid #ddd; text-align: right;"">${item.TicketMoney}</td>
                                        </tr>
                                ";
                        }

                        html += $@"
                                <tr>
                                  <td colspan=""2"" style=""padding: 10px; border: 1px solid #ddd; text-align: right;""><strong>總計</strong></td>
                                  <td style=""padding: 10px; border: 1px solid #ddd; text-align: right;""><strong>${result.TicketTotalPrice}</strong></td>
                                </tr>
                              </tbody>
                            </table>
                          </div>
                        </body>
                        </html>
                        ";


                        await _mailHelper.SendMail(new EmailRequest
                        {
                                ToEmail = _httpContextAccessor?.HttpContext.Items["UserEmail"]?.ToString(),
                                Subject = $"訂單明細-{result.MovieName}",
                                Body = html
                        });

                        return responseJson;
                }
        }
}
