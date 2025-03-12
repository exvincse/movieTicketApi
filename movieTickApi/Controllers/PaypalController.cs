using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using movieTickApi.Dtos.ApiInput.PayPal;
using movieTickApi.Dtos.Output.Users;
using movieTickApi.Models;
using movieTickApi.Service;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace movieTickApi.Controllers
{
        [Route("api/[controller]")]
        [ApiController]
        public class PaypalController : ControllerBase
        {
                private readonly WebDbContext _context;
                private readonly IHttpClientFactory _httpClientFactory;
                private readonly ResponseService _responseService;
                private readonly PayPalService _payPalService;

                public PaypalController(
                        WebDbContext context,
                        IHttpClientFactory httpClientFactory,
                        ResponseService responseService,
                        PayPalService payPalService
                 )
                {
                        _context = context;
                        _httpClientFactory = httpClientFactory;
                        _responseService = responseService;
                        _payPalService = payPalService;
                }

                [HttpPost("PostSuccessOrder")]
                public async Task<RequestResultOutputDto<object>> PostSuccessOrder([FromBody] PayPalCheckOrderInputDto value)
                {
                        if (string.IsNullOrEmpty(value.Token))
                        {
                                return _responseService.RequestResult(new RequestResultOutputDto<object>
                                {
                                        StatusCode = HttpContext.Response.StatusCode,
                                        Message = "缺少 token 參數",
                                        Result = false
                                });
                        }

                        var accessToken = await _payPalService.GetAccessTokenAsync();
                        var client = _httpClientFactory.CreateClient();
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                        var content = new StringContent("{}", Encoding.UTF8, "application/json");
                        var response = await client.PostAsync($"{_payPalService.baseUrl}/v2/checkout/orders/{value.Token}/capture", content);

                        if (!response.IsSuccessStatusCode)
                        {
                                return _responseService.RequestResult(new RequestResultOutputDto<object>
                                {
                                        StatusCode = HttpContext.Response.StatusCode,
                                        Message = "付款失敗，請重試",
                                        Result = response
                                });
                        }

                        var responseBody = await response.Content.ReadAsStringAsync();
                        dynamic responseJson = JsonConvert.DeserializeObject(responseBody);

                        string orderId = responseJson.id;
                        await _context.TicketDetailMain
                            .Where(x => x.CreateOrderId == orderId)
                            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.TicketStatusId, 1));

                        return _responseService.RequestResult(new RequestResultOutputDto<object>
                        {
                                StatusCode = HttpContext.Response.StatusCode,
                                Message = "付款成功",
                                Result = true
                        });
                }

                [HttpGet("GetOrderDetail")]
                public async Task<RequestResultOutputDto<object>> GetOrderDetail([FromQuery] string orderId)
                {
                        if (string.IsNullOrEmpty(orderId))
                        {
                                return _responseService.RequestResult(new RequestResultOutputDto<object>
                                {
                                        StatusCode = 400,
                                        Message = "缺少 orderId 參數",
                                        Result = false
                                });
                        }

                        try
                        {
                                var accessToken = await _payPalService.GetAccessTokenAsync();
                                var client = _httpClientFactory.CreateClient();
                                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                                var response = await client.GetAsync($"{_payPalService.baseUrl}/v2/checkout/orders/{orderId}");

                                if (!response.IsSuccessStatusCode)
                                {
                                        return _responseService.RequestResult(new RequestResultOutputDto<object>
                                        {
                                                StatusCode = (int)response.StatusCode,
                                                Message = response.ReasonPhrase,
                                                Result = ""
                                        });
                                }

                                var responseBody = await response.Content.ReadAsStringAsync();
                                var responseJson = JsonConvert.DeserializeObject<dynamic>(responseBody);

                                if (responseJson?.status == null || responseJson?.links == null)
                                {
                                        return _responseService.RequestResult(new RequestResultOutputDto<object>
                                        {
                                                StatusCode = 400,
                                                Message = "無效的訂單 ID 或回應格式錯誤",
                                                Result = ""
                                        });
                                }

                                if (responseJson.links.Count >= 2 && responseJson.links[1]?.href != null)
                                {
                                        string approvalUrl = responseJson.links[1].href;
                                        return _responseService.RequestResult(new RequestResultOutputDto<object>
                                        {
                                                StatusCode = (int)response.StatusCode,
                                                Message = "取得訂單詳情成功",
                                                Result = approvalUrl
                                        });
                                }

                                return _responseService.RequestResult(new RequestResultOutputDto<object>
                                {
                                        StatusCode = (int)response.StatusCode,
                                        Message = "找不到訂單的核准連結",
                                        Result = ""
                                });
                        }
                        catch (Exception ex)
                        {
                                return _responseService.RequestResult(new RequestResultOutputDto<object>
                                {
                                        StatusCode = 500,
                                        Message = ex.Message,
                                        Result = false
                                });
                        }
                }

        }
}
