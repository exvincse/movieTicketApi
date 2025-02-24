using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using movieTickApi.Dtos.ApiInput.PayPal;
using movieTickApi.Dtos.ApiOutput.PayPal;
using movieTickApi.Dtos.Output.Users;
using movieTickApi.Helper;
using movieTickApi.Models;
using movieTickApi.Service;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace movieTickApi.Controllers
{
        [Route("api/[controller]")]
        [ApiController]
        public class PaypalController : ControllerBase
        {
                private readonly IHttpClientFactory _httpClientFactory;
                private readonly ResponseService _responseService;
                private readonly string clientId = "AZF0Rv39g-VaTuQN4rJL-T7mQfBqWtVSWUiO8K95j8wt3_cTpq0eYvrxDIN0Es3HUfnA_zT9osV5Ioj3"; // Replace with your PayPal client ID
                private readonly string secret = "EO7GPIf3Ty3Tn6mh-4O7EQaAVTseNtVXIhMNhF6PTgVqlr_5Gd-_k-TNRNBvdQ-n1drL8ZJ4V466i1EW"; // Replace with your PayPal secret
                private readonly string baseUrl = "https://api.sandbox.paypal.com";

                public PaypalController(IHttpClientFactory httpClientFactory, ResponseService responseService)
                {
                        _httpClientFactory = httpClientFactory;
                        _responseService = responseService;
                }

                private async Task<string> GetAccessTokenAsync()
                {
                        var client = _httpClientFactory.CreateClient();

                        var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{clientId}:{secret}"));
                        client.DefaultRequestHeaders.Add("Authorization", $"Basic {authValue}");

                        var requestBody = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

                        var response = await client.PostAsync($"{baseUrl}/v1/oauth2/token", requestBody);
                        if (!response.IsSuccessStatusCode)
                        {
                                throw new Exception("Failed to obtain access token");
                        }

                        var responseBody = await response.Content.ReadAsStringAsync();
                        dynamic responseJson = JsonConvert.DeserializeObject(responseBody);

                        return responseJson.access_token; // 返回 token
                }

                [HttpPost("PostCreatePayment")]
                public async Task<ActionResult<RequestResultOutputDto<object>>> CreatePayment(PayPalPaymentInputDto res)
                {
                        // 獲取 access token
                        var accessToken = await GetAccessTokenAsync();

                        var client = _httpClientFactory.CreateClient();

                        // 設置授權標頭
                        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                        // 創建 PayPal 訂單的請求體
                        var orderRequest = new
                        {
                                intent = "CAPTURE",
                                purchase_units = new[]
                                {
                                    new
                                    {
                                        amount = new
                                        {
                                            currency_code = "TWD",
                                            value = res.total.ToString()
                                        }
                                    }
                                 },
                                application_context = new
                                {
                                        return_url = "http://localhost:5000/api/payment/execute-order",
                                        cancel_url = "http://localhost:5000/api/payment/cancel-order"
                                }
                        };

                        var content = new StringContent(JsonConvert.SerializeObject(orderRequest), Encoding.UTF8, "application/json");

                        var response = await client.PostAsync($"{baseUrl}/v2/checkout/orders", content);

                        if (!response.IsSuccessStatusCode)
                        {
                                return BadRequest("Order creation failed");
                        }

                        var responseBody = await response.Content.ReadAsStringAsync();
                        dynamic responseJson = JsonConvert.DeserializeObject(responseBody);

                        // 提取支付 URL 並返回給前端
                        string approvalUrl = responseJson.links[1].href;

                        return _responseService.RequestResult(new RequestResultOutputDto<object>
                        {
                                StatusCode = HttpContext.Response.StatusCode,
                                Message = "送出成功",
                                Result = new PayPalPaymentOutputDto
                                {
                                        approvalUrl = approvalUrl
                                }
                        });
                }
        }
}
