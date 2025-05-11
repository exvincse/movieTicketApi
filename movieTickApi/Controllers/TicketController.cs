using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using movieTickApi.Dtos.Input.Ticket;
using movieTickApi.Dtos.Output.Ticket;
using movieTickApi.Dtos.Output.Users;
using movieTickApi.Models;
using movieTickApi.Models.Ticket;
using movieTickApi.Service;

namespace movieTickApi.Controllers
{
        [Route("api/[controller]")]
        [ApiController]
        public class TicketController : ControllerBase
        {
                private readonly WebDbContext _context;
                private readonly IConfiguration _configuration;
                private readonly IMapper _mapper;
                private readonly TokenService _tokenService;
                private readonly ResponseService _responseService;
                private readonly PayPalService _payPalService;

                public TicketController(
                        WebDbContext context,
                        IConfiguration configuration,
                        IMapper mapper,
                        TokenService tokenService,
                        ResponseService responseService,
                        PayPalService payPalService
                 )
                {
                        _context = context;
                        _mapper = mapper;
                        _configuration = configuration;
                        _tokenService = tokenService;
                        _responseService = responseService;
                        _payPalService = payPalService;
                }

                // 取得票種
                [HttpGet("TicketCategory")]
                public async Task<ActionResult<RequestResultOutputDto<object>>> TicketCategory()
                {
                        var result = await _context.TicketCategory
                                .Select(item => new TicketCategoryDto
                                {
                                        CategoryCode = item.CategoryCode,
                                        CategoryName = item.CategoryName,
                                        Cost = item.Cost
                                }).ToListAsync();

                        return _responseService.RequestResult(new RequestResultOutputDto<object>
                        {
                                StatusCode = HttpContext.Response.StatusCode,
                                Message = "",
                                Result = result
                        });
                }

                // 取得票種語言
                [HttpGet("TicketLanguage")]
                public async Task<ActionResult<RequestResultOutputDto<object>>> TicketLanguage()
                {
                        var result = await _context.TicketLanguage.Select(item => new TicketLanguageOutputDto
                        {
                                CategoryCode = item.CategoryCode,
                                CategoryName = item.CategoryName
                        }).ToListAsync();

                        return _responseService.RequestResult(new RequestResultOutputDto<object>
                        {
                                StatusCode = HttpContext.Response.StatusCode,
                                Message = "",
                                Result = result
                        });
                }

                // 取得已選座位
                [HttpPost("SelectSeat")]
                public async Task<ActionResult<RequestResultOutputDto<object>>> SelectSeat([FromBody] TicketSeatInputDto value)
                {
                        var result = await _context.TicketDetailMain
                                .Where(x => x.MovieId == value.MovieId && x.TicketDate == value.MovieTicketDateTime && x.TicketLanguageCode == value.TicketLanguageCode && x.TicketStatusId != 3)
                                .Include(x => x.TicketDetail)
                                .SelectMany(y => y.TicketDetail.Select(td => new TicketSeatOutputDto
                                {
                                        Column = td.TicketColumn,
                                        Seat = td.TicketSeat
                                })).ToListAsync();

                        return _responseService.RequestResult(new RequestResultOutputDto<object>
                        {
                                StatusCode = HttpContext.Response.StatusCode,
                                Message = "",
                                Result = result
                        });
                }

                // 送出票卷
                [HttpPost("SealTicket")]
                [Authorize]
                public async Task<ActionResult<RequestResultOutputDto<object>>> SealTicket([FromBody] TicketDetailInputDto value)
                {
                        if (!ModelState.IsValid)
                        {
                                return _responseService.ApiRequestResult<object>(400, "請求參數不合法", default);
                        }

                        var userId = HttpContext.Items["UserId"] as string;
                        var createUser = await _context.UserProfile.Where(x => x.UserNo == int.Parse(userId)).FirstOrDefaultAsync();

                        if (createUser == null)
                        {
                                return _responseService.ApiRequestResult<object>(400, "使用者不存在", false);
                        }

                        var createOrder = await _payPalService.CreatePayment(value.TotalCost);
                        if (createOrder.orderId == null)
                        {
                                return _responseService.ApiRequestResult<object>(400, "訂單建立失敗", false);
                        }

                        var TicketDetail = new TicketDetailMain
                        {
                                Id = Guid.NewGuid(),
                                MovieId = value.MovieId,
                                MovieName = value.MovieName,
                                TicketDate = value.TicketDateTime,
                                TicketLanguageCode = value.TicketLanguageCode,
                                TicketLanguageName = value.TicketLanguageName,
                                TicketStatusId = 2,
                                CreateUserNo = createUser.UserNo,
                                CreateDateTime = DateTime.UtcNow,
                                CreateOrderId = createOrder.orderId
                        };

                        var mapTick = value.TicketCategory
                                .Select(item => new TicketDetail
                                {
                                        Id = Guid.NewGuid(),
                                        TicketDetailMainId = TicketDetail.Id,
                                        TicketCategoryCode = item.CategoryCode,
                                        TicketCategoryName = item.CategoryName,
                                        TicketColumn = item.Column,
                                        TicketSeat = item.Seat,
                                        TicketMoney = item.Cost
                                }).ToList();

                        TicketDetail.TicketTotalPrice = mapTick.Sum(x => x.TicketMoney);
                        TicketDetail.TicketDetail = mapTick;

                        await _context.TicketDetailMain.AddAsync(TicketDetail);
                        await _context.SaveChangesAsync();

                        return _responseService.RequestResult(new RequestResultOutputDto<object>
                        {
                                StatusCode = HttpContext.Response.StatusCode,
                                Message = "建立成功",
                                Result = createOrder.approvalUrl
                        });
                }

                // 取得個人票券
                [HttpGet("PersonalTicketList")]
                [Authorize]
                public async Task<ActionResult<RequestResultOutputDto<object>>> PersonalTicketList([FromQuery] TicketPersonalInputDto value)
                {
                        var userId = HttpContext.Items["UserId"] as string;
                        var ticketDetailQuery = _context.TicketDetailMain.Where(x => x.CreateUserNo == int.Parse(userId));
                        var totalCount = await ticketDetailQuery.CountAsync();

                        int pageIndex = value.PageIndex < 1 ? 1 : value.PageIndex;
                        int pageSize = value.PageSize < 1 ? 10 : value.PageSize;

                        var result = await ticketDetailQuery
                                .Include(x => x.TicketPaymentStatus)
                                .OrderByDescending(x => x.CreateDateTime)
                                .Skip((pageIndex - 1) * pageSize)
                                .Take(pageSize)
                                .Select(y => new TicketPersonalOutputDto
                                {
                                        MovieName = y.MovieName,
                                        TicketDate = y.TicketDate,
                                        TicketLanguageName = y.TicketLanguageName,
                                        TicketStatusId = y.TicketPaymentStatus.StatusId,
                                        TicketStatusName = y.TicketPaymentStatus.StatusName,
                                        TicketPersonalItem = y.TicketDetail.Select(td => new TicketPersonalItemOutputDto
                                        {
                                                TicketCategoryName = td.TicketCategoryName,
                                                TicketColumn = td.TicketColumn,
                                                TicketSeat = td.TicketSeat,
                                                TicketMoney = td.TicketMoney
                                        }).ToList(),
                                        CreateOrderId = y.CreateOrderId
                                }).ToListAsync();

                        return _responseService.RequestResult(new RequestResultOutputDto<object>
                        {
                                StatusCode = HttpContext.Response.StatusCode,
                                Message = "",
                                Result = new
                                {
                                        TotalPage = Math.Ceiling((double)totalCount / pageSize),
                                        PageIndex = value.PageIndex,
                                        PageSize = value.PageSize,
                                        Result = result
                                }
                        });
                }
        }
}
