using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using movieTickApi.Dtos.ApiOutput.PayPal;
using movieTickApi.Dtos.Output.Users;
using movieTickApi.Models;
using movieTickApi.Service;
using Newtonsoft.Json;
using System.Net.Http.Headers;

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

                [HttpGet("GetOrderLink")]
                [Authorize]
                public async Task<RequestResultOutputDto<object>> GetOrderLink([FromQuery] string orderId)
                {
                        if (string.IsNullOrEmpty(orderId))
                        {
                                return _responseService.RequestResult(new RequestResultOutputDto<object>
                                {
                                        StatusCode = 400,
                                        Message = "缺少 orderId 參數",
                                        Result = null
                                });
                        }

                        var userId = HttpContext.Items["UserId"] as string;

                        if (string.IsNullOrEmpty(userId) == true)
                        {
                                return _responseService.RequestResult(new RequestResultOutputDto<object>
                                {
                                        StatusCode = HttpContext.Response.StatusCode,
                                        Message = "請重新登入",
                                        Result = null
                                });
                        }

                        var result = await _context.TicketDetailMain.Where(x => x.CreateUserNo == int.Parse(userId) && x.CreateOrderId == orderId).CountAsync();
                        if (result == 0)
                        {
                                return _responseService.RequestResult(new RequestResultOutputDto<object>
                                {
                                        StatusCode = HttpContext.Response.StatusCode,
                                        Message = "訂單不存在",
                                        Result = null
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
                                                Result = null
                                        });
                                }

                                var responseBody = await response.Content.ReadAsStringAsync();
                                PayPalOrderOutputDto responseJson = JsonConvert.DeserializeObject<PayPalOrderOutputDto>(responseBody);

                                if (responseJson?.Status == null || responseJson?.Links == null)
                                {
                                        return _responseService.RequestResult(new RequestResultOutputDto<object>
                                        {
                                                StatusCode = 400,
                                                Message = "無效的訂單 ID 或回應格式錯誤",
                                                Result = null
                                        });
                                }

                                return _responseService.RequestResult(new RequestResultOutputDto<object>
                                {
                                        StatusCode = (int)response.StatusCode,
                                        Message = "取得訂單詳情成功",
                                        Result = responseJson.Links[1].Href
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

                [HttpGet("GetOrderDetail")]
                [Authorize]
                public async Task<RequestResultOutputDto<object>> GetOrderDetail([FromQuery] string orderId)
                {
                        if (string.IsNullOrEmpty(orderId))
                        {
                                return _responseService.RequestResult(new RequestResultOutputDto<object>
                                {
                                        StatusCode = 400,
                                        Message = "缺少 orderId 參數",
                                        Result = null
                                });
                        }

                        var userId = HttpContext.Items["UserId"] as string;

                        if (string.IsNullOrEmpty(userId) == true)
                        {
                                return _responseService.RequestResult(new RequestResultOutputDto<object>
                                {
                                        StatusCode = HttpContext.Response.StatusCode,
                                        Message = "請重新登入",
                                        Result = null
                                });
                        }

                        var result = await _context.TicketDetailMain.Where(x => x.CreateUserNo == int.Parse(userId) && x.CreateOrderId == orderId).CountAsync();
                        if (result == 0)
                        {
                                return _responseService.RequestResult(new RequestResultOutputDto<object>
                                {
                                        StatusCode = HttpContext.Response.StatusCode,
                                        Message = "訂單不存在",
                                        Result = null
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
                                                Result = null
                                        });
                                }

                                var responseBody = await response.Content.ReadAsStringAsync();
                                PayPalOrderOutputDto responseJson = JsonConvert.DeserializeObject<PayPalOrderOutputDto>(responseBody);

                                if (responseJson?.Status == null || responseJson?.Links == null)
                                {
                                        return _responseService.RequestResult(new RequestResultOutputDto<object>
                                        {
                                                StatusCode = 400,
                                                Message = "無效的訂單 ID",
                                                Result = null
                                        });
                                }

                                if (responseJson?.Status == "APPROVED")
                                {
                                        var orderSuccessDetail = await _payPalService.PostSuccessOrder(responseJson.Id);
                                        return _responseService.RequestResult(new RequestResultOutputDto<object>
                                        {
                                                StatusCode = (int)response.StatusCode,
                                                Message = "取得訂單詳情成功",
                                                Result = new CaptureOutputDto
                                                {
                                                        OrderId = orderSuccessDetail.Id,
                                                        Status = orderSuccessDetail.Status,
                                                        Amount = orderSuccessDetail.Purchase_units[0].Payments.Captures[0].Amount.Value,
                                                        CreateTime = orderSuccessDetail.Purchase_units[0].Payments.Captures[0].Create_time
                                                }
                                        });
                                }

                                return _responseService.RequestResult(new RequestResultOutputDto<object>
                                {
                                        StatusCode = (int)response.StatusCode,
                                        Message = "取得訂單詳情成功",
                                        Result = new CaptureOutputDto
                                        {
                                                OrderId = responseJson.Id,
                                                Status = responseJson.Status,
                                                Amount = responseJson.Purchase_Units[0].Amount.Value,
                                                CreateTime = responseJson.Create_Time
                                        }
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
