using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using movieTickApi.Dtos;
using movieTickApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using movieTickApi.Service;
using movieTickApi.Helper;


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
                private readonly MailHelper _mailHelper;

                public UserController(
                        WebDbContext context,
                        IMapper mapper, 
                        IConfiguration  configuration, 
                        TokenService tokenService,
                        ResponseService responseService,
                        MailHelper mailHelper)
                {
                        _context = context;
                        _mapper = mapper;
                        _configuration = configuration;
                        _tokenService = tokenService;
                        _responseService = responseService;
                        _mailHelper = mailHelper;
                }

                // 註冊帳號
                [HttpPost("PostRegister")]
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
                                                Name = value.Email.Split('@')[0],
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
                                Secure = true,
                                SameSite = SameSiteMode.None,
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

                        if (selectUser == null || !BCrypt.Net.BCrypt.Verify(value.Password, selectUser.Password)) {
                                return _responseService.RequestResult<object>(new RequestResultDto<object>
                                {
                                        StatusCode = HttpContext.Response.StatusCode.ToString(),
                                        Message = "帳號密碼錯誤",
                                        Result = new { 
                                                isAccountError = true
                                        }
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
                                        Secure = true,
                                        SameSite = SameSiteMode.None,
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
                [HttpPost("Logout")]
                [Authorize]
                public async Task<RequestResultDto<object>> PostLogout()
                {
                        var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                        var result = await _context.Token.Where(x => x.token == token && x.IsRevoked == false).FirstOrDefaultAsync();

                        if (result != null) {
                                result.IsRevoked = true;
                                result.ExpiresAt = DateTime.UtcNow;
                                _context.Token.Update(result);


                                if (string.IsNullOrEmpty(Request.Cookies["refreshToken"]) == false)
                                {
                                        var refreshTokens = _context.UserRefreshTokens
                                            .Where(x =>  x.RefreshToken == Request.Cookies["refreshToken"]);
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
                [HttpGet("ReFreshToken")]
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

                                var addAccessToken = new Token
                                {
                                        Id = Guid.NewGuid(),
                                        UserId = user.Id,
                                        token = AccessToken,
                                        CreatedAt = DateTime.UtcNow,
                                        ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                                        IsRevoked = false
                                };

                                _context.Token.Add(addAccessToken);
                                await _context.SaveChangesAsync();

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

                // 註冊帳號發送信件
                [HttpPost("PostSendMail")]
                public async Task<RequestResultDto<object>> PostSendMail([FromBody] PostSendMailDto value)
                {
                        var mailHelper = await _mailHelper.SendMail(new EmailRequest
                        {
                                ToEmail = value.Email,
                                ToName = value.Email.Split('@')[0]
                        });

                        if (mailHelper == true)
                        {
                                return _responseService.RequestResult<object>(new RequestResultDto<object>
                                {
                                        StatusCode = HttpContext.Response.StatusCode.ToString(),
                                        Message = "發送成功",
                                        Result = true
                                });
                        }

                        return _responseService.RequestResult<object>(new RequestResultDto<object>
                        {
                                StatusCode = HttpContext.Response.StatusCode.ToString(),
                                Message = "發送失敗",
                                Result = false
                        });
                }

                // 驗證OTP
                [HttpPost("PostValidOtp")]
                public async Task<RequestResultDto<object>> PostValidOtp([FromBody] PostValidOtp validOtp)
                {
                        var otp =_context.OtpVerification
                                .Where(x => x.Email == validOtp.Email && x.Otp == validOtp.Otp && x.ExpirationTime > DateTime.UtcNow && x.IsUsed == false)
                                .OrderByDescending(x => x.ExpirationTime)
                                .FirstOrDefault();

                        if(otp == null)
                        {
                                return _responseService.RequestResult<object>(new RequestResultDto<object>
                                {
                                        StatusCode = HttpContext.Response.StatusCode.ToString(),
                                        Message = "驗證碼輸入錯誤或過期",
                                        Result = false
                                });
                        }

                        otp.IsUsed = true;
                        otp.UpdatedAt = DateTime.UtcNow;
                        _context.OtpVerification.Update(otp);
                        await _context.SaveChangesAsync();

                        //HttpContext.Session.Remove("Email");

                        return _responseService.RequestResult<object>(new RequestResultDto<object>
                        {
                                StatusCode = HttpContext.Response.StatusCode.ToString(),
                                Message = "驗證成功",
                                Result = true
                        });
                }

                // 驗證Email
                [HttpPost("PostValidEmail")]
                public async Task<RequestResultDto<object>> PostValidEmail([FromBody] PostSendMailDto value)
                {
                        var validEmail = await _context.User.Where(x => x.Email == value.Email).FirstOrDefaultAsync();

                        if (validEmail == null)
                        {
                                return _responseService.RequestResult<object>(new RequestResultDto<object>
                                {
                                        StatusCode = HttpContext.Response.StatusCode.ToString(),
                                        Message = "此Email尚未註冊",
                                        Result = false
                                });
                        }

                        return _responseService.RequestResult<object>(new RequestResultDto<object>
                        {
                                StatusCode = HttpContext.Response.StatusCode.ToString(),
                                Message = "此Email已註冊",
                                Result = true
                        });
                }

                //// 取得otp Email
                //[HttpGet("GetOtpEmail")]
                //public RequestResultDto<object> GetOtpEmail()
                //{
                //        var email = HttpContext.Session.GetString("Email");

                //        if (email == null)
                //        {
                //                return _responseService.RequestResult<object>(new RequestResultDto<object>
                //                {
                //                        StatusCode = HttpContext.Response.StatusCode.ToString(),
                //                        Message = "取得Email錯誤",
                //                        Result = new
                //                        {
                //                                email
                //                        }
                //                });
                //        }

                //        return _responseService.RequestResult<object>(new RequestResultDto<object>
                //        {
                //                StatusCode = HttpContext.Response.StatusCode.ToString(),
                //                Message = "取得Email",
                //                Result = new
                //                {
                //                        email
                //                }
                //        });
                //}
        }
}
