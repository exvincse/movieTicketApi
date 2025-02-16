using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using movieTickApi.Dtos.Input.Ticket;
using movieTickApi.Dtos.Output.Ticket;
using movieTickApi.Dtos.Output.Users;
using movieTickApi.Helper;
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

                public TicketController(
                        WebDbContext context,
                        IMapper mapper,
                        IConfiguration configuration,
                        TokenService tokenService,
                        ResponseService responseService,
                        MailHelper mailHelper)
                {
                        _context = context;
                        _mapper = mapper;
                        _configuration = configuration;
                        _tokenService = tokenService;
                        _responseService = responseService;
                }

                // 取得票種
                [HttpGet("GetTicketCategory")]
                public async Task<ActionResult<RequestResultOutputDto<object>>> GetTicketCategory()
                {
                        var result = await _context.TicketCategory
                                .Select(item => new TicketCategoryDto{
                                        CategoryCode = item.CategoryCode,
                                        CategoryName = item.CategoryName,
                                        Cost = item.Cost
                                }).ToListAsync();
                        return Ok(_responseService.RequestResult<object>(new RequestResultOutputDto<object>
                        {
                                StatusCode = HttpContext.Response.StatusCode,
                                Message = "",
                                Result = result
                        }));
                }

                // 取得票種語言
                [HttpGet("GetTicketLanguage")]
                public async Task<ActionResult<ActionResult<RequestResultOutputDto<object>>>> GetTicketLanguage()
                {
                        var result = await _context.TicketLanguage.Select(item => new TicketLanguageOutputDto
                        {
                                CategoryCode = item.CategoryCode,
                                CategoryName = item.CategoryName
                        }).ToListAsync();

                        return Ok(_responseService.RequestResult<object>(new RequestResultOutputDto<object>
                        {
                                StatusCode = HttpContext.Response.StatusCode,
                                Message = "",
                                Result = result
                        }));
                }

                // 取得已選座位
                [HttpPost("PostSelectSeat")]
                public async Task<ActionResult<ActionResult<RequestResultOutputDto<object>>>> PostSelectSeat([FromBody] TicketSeatInputDto value)
                {
                        var seat = await _context.TicketDetail
                                .Where(x => x.MovieId == value.MovieId && x.TicketDate == value.MovieTicketDateTime)
                                .Select(y => new TicketSeatOutputDto
                                {
                                        Column = y.TicketColumn,
                                        Seat = y.TicketSeat
                                }).ToListAsync();

                        return Ok(_responseService.RequestResult<object>(new RequestResultOutputDto<object>
                        {
                                StatusCode = HttpContext.Response.StatusCode,
                                Message = "",
                                Result = seat
                        }));
                }

                // 送出票卷
                [HttpPost("PostSealTicket")]
                [Authorize]
                public async Task<ActionResult<RequestResultOutputDto<object>>> PostSealTicket([FromBody] TicketDetailInputDto value)
                {
                        if (!ModelState.IsValid)
                        {
                                return _responseService.RequestResult<object>(new RequestResultOutputDto<object>
                                {
                                        StatusCode = 400,
                                        Message = "請求參數不合法",
                                        Result = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                                });
                        }
                        var createUser = await _context.UserProfile.Where(x => x.Email == (HttpContext.Items["UserEmail"] as string)).FirstOrDefaultAsync();

                        if (createUser == null)
                        {
                                return BadRequest(_responseService.RequestResult<object>(new RequestResultOutputDto<object>
                                {
                                        StatusCode = 400,
                                        Message = "使用者不存在",
                                        Result = false
                                }));
                        }

                        var mapTick = new List<TicketDetail>();
                        foreach (var item in value.TicketCategory) {
                                mapTick.Add(new TicketDetail
                                {
                                        MovieId = value.MovieId,
                                        TicketDate = value.TicketDateTime,
                                        TicketLanguageCode = value.TicketLanguageCode,
                                        TicketLanguageName = value.TicketLanguageName,
                                        TicketCategoryCode = item.CategoryCode,
                                        TicketCategoryName = item.CategoryName,
                                        TicketColumn = item.Column,
                                        TicketSeat = item.Seat,
                                        TicketMoney = item.Cost,
                                        CreateUserNo = createUser.UserNo,
                                        CreateDateTime = DateTime.UtcNow
                                });
                        }

                        await _context.TicketDetail.AddRangeAsync(mapTick);
                        await _context.SaveChangesAsync();

                        return Ok(_responseService.RequestResult<object>(new RequestResultOutputDto<object>
                        {
                                StatusCode = HttpContext.Response.StatusCode,
                                Message = "送出成功",
                                Result = true
                        }));
                }
        }
}
