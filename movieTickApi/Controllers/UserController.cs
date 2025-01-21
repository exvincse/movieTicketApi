using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using movieTickApi.Dtos;
using movieTickApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using movieTickApi.Service;

namespace movieTickApi.Controllers
{
        [ApiController]
        [Route("api/[controller]")]
        public class UserController : ControllerBase
        {
                private readonly WebDbContext _context;
                private readonly IConfiguration _configuration;
                private readonly IMapper _mapper;
                private readonly TokenService _tokenService;
                private readonly ResponseService _responseService;

                public UserController(
                        WebDbContext context,
                        IMapper mapper, 
                        IConfiguration  configuration, 
                        TokenService tokenService,
                        ResponseService responseService)
                {
                        _context = context;
                        _mapper = mapper;
                        _configuration = configuration;
                        _tokenService = tokenService;
                        _responseService = responseService;
                }

                // 註冊帳號
                [HttpPost("Register")]
                public async Task<RequestResultDto<object>> PostRegister([FromBody] RegisterDto value)
                {
                        var passwordHash = BCrypt.Net.BCrypt.HashPassword(value.Password);

                        var addUser = new User
                        {
                                Id = new Guid(),
                                Email = value.Email,
                                Password = passwordHash,
                                CreateDatetime = DateTime.UtcNow,
                                ModifyDatetime = DateTime.UtcNow,
                                UserProfile = new List<UserProfile>
                                {
                                        new UserProfile
                                        {
                                                Id = Guid.NewGuid(),
                                                Name = value.Name,
                                                CreateDatetime = DateTime.UtcNow,
                                                ModifyDatetime = DateTime.UtcNow,
                                        }
                                }
                        };

                        _context.User.Add(addUser);
                        await _context.SaveChangesAsync();

                        var user = await _context.User.Where(a => a.Email == value.Email && a.Password == addUser.Password).SingleOrDefaultAsync();

                        var claim = new List<Claim>
                                {
                                        new Claim(JwtRegisteredClaimNames.Email, user.Email.ToString()),
                                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
                                };

                        var AccessToken = _tokenService.CreateAccessToken(claim);

                        var RefreshToken = _tokenService.CreateRefreshToken();

                        var addAccessToken = new Token
                        {
                                Id = Guid.NewGuid(),
                                UserId = user.Id,
                                token = AccessToken,
                                CreatedAt = DateTime.UtcNow,
                                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                                IsRevoked = false
                        };

                        var addRefreshToken = new UserRefreshTokens
                        {
                                UserId = user.Id,
                                RefreshToken = RefreshToken,
                                ExpiryDate = DateTime.UtcNow.AddMinutes(15)
                        };

                        _context.Token.Add(addAccessToken);

                        _context.UserRefreshTokens.Add(addRefreshToken);

                        await _context.SaveChangesAsync();

                        Response.Cookies.Append("refreshToken", RefreshToken, new CookieOptions
                        {
                                HttpOnly = true,
                                Secure = false,
                                SameSite = SameSiteMode.Strict,
                                Expires = DateTime.UtcNow.AddMinutes(15)
                        });


                        return _responseService.RequestResult<object>(new RequestResultDto<object>
                        {
                                StatusCode = HttpContext.Response.StatusCode.ToString(),
                                Message = "註冊成功",
                                Result = new
                                {
                                        accessToken = AccessToken
                                }
                        });
                }

                // 取得登入token
                [HttpPost("Login")]
                public async Task<RequestResultDto<object>> PostLogin([FromBody] LoginDto value)
                {
                        var selectUser= await _context.User.Where(a => a.Email == value.Email).FirstOrDefaultAsync();

                        if (selectUser == null) {
                                return _responseService.RequestResult<object>(new RequestResultDto<object>
                                {
                                        StatusCode = HttpContext.Response.StatusCode.ToString(),
                                        Message = "帳號密碼錯誤",
                                        Result = null
                                });
                        }
                        else
                        {
                                var claim = new List<Claim>
                                {
                                        new Claim(ClaimTypes.Email, selectUser.Email.ToString()),
                                        new Claim(ClaimTypes.NameIdentifier, selectUser.Id.ToString())
                                };

                                var AccessToken = _tokenService.CreateAccessToken(claim);

                                var RefreshToken = _tokenService.CreateRefreshToken();

                                var addAccessToken = new Token
                                {
                                        Id = Guid.NewGuid(),
                                        UserId = selectUser.Id,
                                        token = AccessToken,
                                        CreatedAt = DateTime.UtcNow,
                                        ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                                        IsRevoked = false
                                };

                                var addRefreshToken = new UserRefreshTokens
                                {
                                        UserId = selectUser.Id,
                                        RefreshToken = RefreshToken,
                                        ExpiryDate = DateTime.UtcNow.AddMinutes(15)
                                };

                                _context.Token.Add(addAccessToken);

                                _context.UserRefreshTokens.Add(addRefreshToken);

                                await _context.SaveChangesAsync();

                                Response.Cookies.Append("refreshToken", RefreshToken, new CookieOptions
                                {
                                        HttpOnly = true,
                                        Secure = false, 
                                        SameSite = SameSiteMode.Strict,
                                        Expires = DateTime.UtcNow.AddMinutes(15)
                                });

                                return _responseService.RequestResult<object>(new RequestResultDto<object>
                                {
                                        StatusCode = HttpContext.Response.StatusCode.ToString(),
                                        Message = "",
                                        Result = new
                                        {
                                                accessToken = AccessToken
                                        }
                                });
                        }
                }

                // 取得登出token
                [HttpPut("Logout")]
                [Authorize]
                public async Task<RequestResultDto<object>> PostLogout([FromBody] RefreshTokenRequestDto param)
                {
                        var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                        var result = await _context.Token.Where(x => x.token == token && x.IsRevoked == false).FirstOrDefaultAsync();

                        if (result != null) {
                                result.IsRevoked = true;
                                result.ExpiresAt = DateTime.UtcNow;
                                _context.Token.Update(result);


                                if (param.RefreshToken != null && param.RefreshToken.Any())
                                {
                                        var refreshTokens = _context.UserRefreshTokens
                                            .Where(x => param.RefreshToken.Contains(x.RefreshToken));
                                        _context.UserRefreshTokens.RemoveRange(refreshTokens);
                                }

                                _context.SaveChanges();

                                return _responseService.RequestResult<object>(new RequestResultDto<object>
                                {
                                        StatusCode = HttpContext.Response.StatusCode.ToString(),
                                        Message = "已登出",
                                        Result = null
                                });
                        } 
                        else
                        {
                                return _responseService.RequestResult<object>(new RequestResultDto<object>
                                {
                                        StatusCode = HttpContext.Response.StatusCode.ToString(),
                                        Message = "請重新登入",
                                        Result = null
                                });
                        }
                }

                // 刷新token
                [HttpPost("ReFreshToken")]
                [Authorize]
                public async Task<RequestResultDto<object>> GetReFreshToken()
                {
                        var reFreshToken = await _context.UserRefreshTokens.Where(x => x.RefreshToken == Request.Cookies["refreshToken"]).FirstOrDefaultAsync();
                        if (reFreshToken == null || reFreshToken.ExpiryDate < DateTime.UtcNow)
                        {
                                return _responseService.RequestResult<object>(new RequestResultDto<object>
                                {
                                        StatusCode = HttpContext.Response.StatusCode.ToString(),
                                        Message = "請重新登入",
                                        Result = null
                                });
                        }
                        else
                        {
                                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                                var userEmail = User.FindFirstValue(ClaimTypes.Email);

                                if (string.IsNullOrEmpty(userId))
                                {
                                        return _responseService.RequestResult<object>(new RequestResultDto<object>
                                        {
                                                StatusCode = HttpContext.Response.StatusCode.ToString(),
                                                Message = "無效的請求",
                                                Result = null
                                        });
                                }

                                var user = await _context.User.FirstOrDefaultAsync(x => x.Id.ToString() == userId && x.Email == userEmail);
                                if (user == null)
                                {
                                        return _responseService.RequestResult<object>(new RequestResultDto<object>
                                        {
                                                StatusCode = HttpContext.Response.StatusCode.ToString(),
                                                Message = "用戶不存在",
                                                Result = null
                                        });
                                }

                                var authToken = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                                var revokeAccessToken = await _context.Token.FirstOrDefaultAsync(x => x.token == authToken);

                                if (revokeAccessToken != null) {
                                        revokeAccessToken.IsRevoked = true;
                                        _context.Token.Update(revokeAccessToken);
                                        _context.SaveChanges();
                                }

                                 var claim = new List<Claim>
                                {
                                        new Claim(ClaimTypes.Email, userEmail.ToString()),
                                        new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                                };

                                var AccessToken = _tokenService.CreateAccessToken(claim);

                                return _responseService.RequestResult<object>(new RequestResultDto<object>
                                {
                                        StatusCode = HttpContext.Response.StatusCode.ToString(),
                                        Message = "",
                                        Result = new
                                        {
                                                AccessToken
                                        }
                                });
                        }
                }

                // 取得使用者資料
                [HttpGet("GetUserProfile")]
                [Authorize]
                public async Task<ActionResult<RequestResultDto<object>>> GetUserProfile()
                {
                        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                        if (userId == null) {
                                return _responseService.RequestResult<object>(new RequestResultDto<object>
                                {
                                        StatusCode = HttpContext.Response.StatusCode.ToString(),
                                        Message = "請重新登入",
                                        Result = null
                                });
                        }

                        var result = await _context.UserProfile.Where(x => x.UserId == Guid.Parse(userId)).FirstOrDefaultAsync();

                        if (result == null)
                        {
                                return _responseService.RequestResult<object>(new RequestResultDto<object>
                                {
                                        StatusCode = HttpContext.Response.StatusCode.ToString(),
                                        Message = "找不到資料",
                                        Result = null
                                });
                        }
                        else {
                                return _responseService.RequestResult<object>(new RequestResultDto<object>
                                {
                                        StatusCode = HttpContext.Response.StatusCode.ToString(),
                                        Message = "",
                                        Result = new UserProfileDto
                                        {
                                                Id = result.Id,
                                                Name = result.Name
                                        }
                                });
                        }
                }

               // 取得所有資料
               //[HttpPost("postAddUser")]
               // public async Task<ActionResult<UserDto>> PostAddUser([FromBody] string name)
               // {
               //         if (string.IsNullOrEmpty(name))
               //         {
               //                 return BadRequest(new
               //                 {
               //                         message = "請輸入參數"
               //                 });
               //         }

               //         User insertUser = new User
               //         {
               //                 Id = Guid.NewGuid(),
               //                 Name = name,
               //                 TestUsers = new List<TestUser>
               //                 {
               //                         new TestUser
               //                         {
               //                                 Id = Guid.NewGuid(),
               //                                 Name = name
               //                         }
               //                 }
               //         };

               //         _context.User.Add(insertUser);
               //         await _context.SaveChangesAsync();

               //         var userData = new UserDto
               //         {
               //                 Id = insertUser.Id,
               //                 Name = insertUser.Name,
               //                 TestName = insertUser.TestUsers.Select(x => new TestUserDto
               //                 {
               //                         Id = x.Id,
               //                         Name = x.Name
               //                 }).ToList()
               //         };

               //         return Ok();
               // }


               // 取得所有資料
               //[HttpGet]
               //[Authorize]
               // public async Task<ActionResult<UserDto>> GetAll()
               // {
               //         var map = await _context.User.Include(x => x.TestUsers).Select(a => new UserDto
               //         {
               //                 Id = a.Id,
               //                 Name = a.Name,
               //                 TestName = a.TestUsers.Select(x => new TestUserDto
               //                 {
               //                         Name = x.Name,
               //                         UserId = x.UserId
               //                 }).ToList()
               //         }).ToListAsync();

               //         return Ok();
               // }

               // 取得所有資料
               //[HttpGet("{id}")]
               // public async Task<ActionResult<UserDto>> Get(Guid id)
               // {
               //         var result = await _context.User.Where(item => item.Id == id).ToListAsync();
               //         return Ok(result);
               // }

               // 取得所有資料
               //[HttpPost]
               // public async Task<ActionResult<UserDto>> GetDefaultData([FromBody] string name)
               // {
               //         if (string.IsNullOrEmpty(name))
               //         {
               //                 return BadRequest(new
               //                 {
               //                         message = "請輸入參數"
               //                 });
               //         }
               //         var result = await _context.User.Where(item => item.Name.Contains(name)).ToListAsync();
               //         var map = _mapper.Map<IEnumerable<UserDto>>(result);
               //         return Ok();
               // }

               // 取得所有資料
               //[HttpPut("putUserData")]
               // public async Task<ActionResult<UpdateUserDto>> PutUserData([FromBody] UpdateUserDto param)
               // {
               //         var item = _context.User.Include(x => x.TestUsers).FirstOrDefault(x => x.Id == param.Id);

               //         var item1 = (from a in _context.User
               //                      join b in _context.TestUser on a.Id equals b.UserId
               //                      where a.Id == param.Id
               //                      select new UserDto
               //                      {
               //                              Id = a.Id,
               //                              Name = a.Name,
               //                              TestName = new List<TestUserDto>
               //                             {
               //                                     new TestUserDto
               //                                     {
               //                                             Id = b.Id,
               //                                             Name = b.Name,
               //                                             UserId = a.Id,
               //                                     }
               //                             }
               //                      }).FirstOrDefault();


               //         if (item == null)
               //         {
               //                 return NotFound(new { Message = "User not found." });
               //         }

               //         item.Name = param.Name;

               //         foreach (var testUser in item.TestUsers)
               //         {
               //                 testUser.Name = param.Name;
               //         }

               //         _context.User.Update(item);
               //         await _context.SaveChangesAsync();

               //         var user = new UserDto
               //         {
               //                 Id = item.Id,
               //                 Name = item.Name,
               //                 TestName = item.TestUsers.Select(x => new TestUserDto
               //                 {
               //                         Id = x.Id,
               //                         Name = x.Name
               //                 }).ToList()
               //         };

               //         return Ok();
               // }


               // 取得所有資料
               //[HttpDelete]
               // public async Task<ActionResult<UserDto>> DeleteUserData([FromBody] List<Guid> id)
               // {
               //         if (id.IsNullOrEmpty())
               //         {
               //                 return BadRequest(new
               //                 {
               //                         message = "請輸入參數"
               //                 });
               //         }

               //         var result = _context.User.Include(x => x.TestUsers).FirstOrDefault(x => x.Id == id);

               //         var result = (from a in _context.User where id.Contains(a.Id) select a).Include(x => x.TestUsers);

               //         var result1 = _context.User.Include(x => x.TestUsers).Where(x => id.Contains(x.Id));

               //         if (result1 == null)
               //         {
               //                 return BadRequest(new
               //                 {
               //                         message = "沒有任何資料"
               //                 });
               //         }
               //         else
               //         {
               //                 _context.User.RemoveRange(result1);
               //                 await _context.SaveChangesAsync();
               //                 return Ok(new
               //                 {
               //                         message = "刪除成功"
               //                 });
               //         }

               //         return Ok();
               // }


               // public static UserDto userDtoItem(User item)
               // {
               //         return new UserDto
               //         {
               //                 Id = item.Id,
               //                 Name = item.Name
               //         };
               // }
        }
}
