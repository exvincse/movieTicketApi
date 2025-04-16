using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using movieTickApi.Dtos.Input.Users;
using movieTickApi.Dtos.Output.Users;
using movieTickApi.Helper;
using movieTickApi.Models;
using movieTickApi.Models.Users;
using movieTickApi.Service;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;


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
                        _mailHelper = mailHelper;
                }

                static string GenerateRandomString(int length)
                {
                        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
                        Random random = new Random();

                        return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
                }

                // 確認是否有登入
                [HttpGet("GetIsCheckLogin")]
                public async Task<ActionResult<RequestResultOutputDto<object>>> GetIsCheckLogin()
                {
                        var isAccessTokenRevoked = await _tokenService.IsAccessTokenRevoked();
                        var isTokenRevoked = await _tokenService.IsRefreshTokenRevoked();
                        var isLogin = true;
                        if (isAccessTokenRevoked && isTokenRevoked)
                        {
                                isLogin = false;
                        }

                        return _responseService.RequestResult(new RequestResultOutputDto<object>
                        {
                                StatusCode = HttpContext.Response.StatusCode,
                                Message = (isLogin == false) ? "未登入" : "已登入",
                                Result = isLogin
                        });
                }
                // 註冊帳號
                [HttpPost("PostRegister")]
                public async Task<ActionResult<RequestResultOutputDto<object>>> PostRegister([FromBody] RegisterInputDto value)
                {
                        var passwordHash = BCrypt.Net.BCrypt.HashPassword(value.Password);

                        var addUser = new User
                        {
                                Id = Guid.NewGuid(),
                                Email = value.Email,
                                Password = passwordHash,
                                CreateDateTime = DateTime.UtcNow,
                                ModifyDateTime = DateTime.UtcNow,
                        };

                        addUser.UserProfile = new List<UserProfile>
                        {
                                new UserProfile
                                {
                                        Id = Guid.NewGuid(),
                                        Name = "NO." + GenerateRandomString(6),
                                        Email = value.Email,
                                        CreateDateTime = DateTime.UtcNow,
                                        ModifyDateTime = DateTime.UtcNow,
                                        UserId = addUser.Id
                                }
                        };

                        _context.User.Add(addUser);
                        await _context.SaveChangesAsync();

                        var claim = new List<Claim>
                                {
                                        new Claim(JwtRegisteredClaimNames.Email, addUser.Email.ToString()),
                                        new Claim(ClaimTypes.NameIdentifier, addUser.UserNo.ToString())
                                };

                        var AccessToken = _tokenService.CreateAccessToken(claim);

                        var RefreshToken = _tokenService.CreateRefreshToken();

                        var addAccessToken = new Token
                        {
                                Id = Guid.NewGuid(),
                                UserId = addUser.Id,
                                AccessToken = AccessToken,
                                CreateDateTime = DateTime.UtcNow,
                                ExpiryDateTime = DateTime.UtcNow.AddHours(1),
                                IsRevoked = false
                        };

                        var addRefreshToken = new UserRefreshTokens
                        {
                                UserId = addUser.Id,
                                RefreshToken = RefreshToken,
                                ExpiryDateTime = DateTime.UtcNow.AddHours(2)
                        };

                        _context.Token.Add(addAccessToken);

                        _context.UserRefreshTokens.Add(addRefreshToken);

                        await _context.SaveChangesAsync();

                        Response.Cookies.Append("refreshToken", RefreshToken, new CookieOptions
                        {
                                HttpOnly = true,
                                Secure = true,
                                SameSite = SameSiteMode.None,
                                Expires = DateTime.UtcNow.AddHours(2)
                        });


                        return _responseService.RequestResult(new RequestResultOutputDto<object>
                        {
                                StatusCode = HttpContext.Response.StatusCode,
                                Message = "註冊成功",
                                Result = new
                                {
                                        accessToken = AccessToken
                                }
                        });
                }

                // 取得登入token
                [HttpPost("Login")]
                public async Task<ActionResult<RequestResultOutputDto<object>>> PostLogin([FromBody] LoginInputDto value)
                {
                        var selectUser = await _context.User.Where(a => a.Email == value.Email).FirstOrDefaultAsync();

                        if (selectUser == null || !BCrypt.Net.BCrypt.Verify(value.Password, selectUser.Password))
                        {
                                return _responseService.RequestResult(new RequestResultOutputDto<object>
                                {
                                        StatusCode = HttpContext.Response.StatusCode,
                                        Message = "帳號密碼錯誤",
                                        Result = new
                                        {
                                                isAccountError = true
                                        }
                                });
                        }
                        else
                        {
                                var claim = new List<Claim>
                                {
                                        new Claim(ClaimTypes.Email, selectUser.Email.ToString()),
                                        new Claim(ClaimTypes.NameIdentifier, selectUser.UserNo.ToString())
                                };

                                var AccessToken = _tokenService.CreateAccessToken(claim);

                                var RefreshToken = _tokenService.CreateRefreshToken();

                                var addAccessToken = new Token
                                {
                                        Id = Guid.NewGuid(),
                                        UserId = selectUser.Id,
                                        AccessToken = AccessToken,
                                        CreateDateTime = DateTime.UtcNow,
                                        ExpiryDateTime = DateTime.UtcNow.AddHours(1),
                                        IsRevoked = false
                                };

                                var addRefreshToken = new UserRefreshTokens
                                {
                                        UserId = selectUser.Id,
                                        RefreshToken = RefreshToken,
                                        ExpiryDateTime = DateTime.UtcNow.AddHours(2)
                                };

                                _context.Token.Add(addAccessToken);

                                _context.UserRefreshTokens.Add(addRefreshToken);

                                await _context.SaveChangesAsync();

                                Response.Cookies.Append("refreshToken", RefreshToken, new CookieOptions
                                {
                                        HttpOnly = true,
                                        Secure = true,
                                        SameSite = SameSiteMode.None,
                                        Expires = DateTime.UtcNow.AddHours(2)
                                });


                                return _responseService.RequestResult(new RequestResultOutputDto<object>
                                {
                                        StatusCode = HttpContext.Response.StatusCode,
                                        Message = "",
                                        Result = new
                                        {
                                                accessToken = AccessToken
                                        }
                                });
                        }
                }

                // 登出
                [HttpPost("Logout")]
                [Authorize]
                public async Task<RequestResultOutputDto<object>> PostLogout()
                {
                        bool isChange = false;

                        var refreshToken = Request.Cookies["refreshToken"];
                        if (string.IsNullOrEmpty(refreshToken) == false)
                        {
                                var refreshTokens = await _context.UserRefreshTokens.Where(x => x.RefreshToken == Request.Cookies["refreshToken"]).FirstOrDefaultAsync();

                                if (refreshTokens != null)
                                {
                                        _context.UserRefreshTokens.Remove(refreshTokens);
                                        isChange = true;
                                }
                        }

                        var accessToken = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

                        if (string.IsNullOrEmpty(accessToken) == false)
                        {
                                var result = await _context.Token.Where(x => x.AccessToken == accessToken && x.IsRevoked == false).FirstOrDefaultAsync();

                                if (result != null)
                                {
                                        result.IsRevoked = true;
                                        result.ExpiryDateTime = DateTime.UtcNow;
                                        _context.Token.Update(result);
                                        isChange = true;
                                }
                        }

                        if (isChange == true)
                        {
                                _context.SaveChanges();
                        }

                        return _responseService.RequestResult(new RequestResultOutputDto<object>
                        {
                                StatusCode = HttpContext.Response.StatusCode,
                                Message = "已登出",
                                Result = true
                        });
                }

                // 刷新token
                [HttpGet("ReFreshToken")]
                public async Task<RequestResultOutputDto<object>> GetReFreshToken()
                {
                        var reFreshToken = await _context.UserRefreshTokens.Where(x => x.RefreshToken == Request.Cookies["refreshToken"]).FirstOrDefaultAsync();

                        if (reFreshToken == null || reFreshToken.ExpiryDateTime < DateTime.UtcNow)
                        {
                                return _responseService.RequestResult(new RequestResultOutputDto<object>
                                {
                                        StatusCode = HttpContext.Response.StatusCode,
                                        Message = "請重新登入",
                                        Result = new
                                        {
                                                isRepeatLogin = true
                                        }
                                });
                        }
                        else
                        {
                                var user = await _context.User.FirstOrDefaultAsync(x => x.Id == reFreshToken.UserId);

                                var claim = new List<Claim>
                                {
                                        new Claim(ClaimTypes.Email, user.Email.ToString()),
                                        new Claim(ClaimTypes.NameIdentifier, user.UserNo.ToString())
                                };

                                var AccessToken = _tokenService.CreateAccessToken(claim);

                                var addAccessToken = new Token
                                {
                                        Id = Guid.NewGuid(),
                                        UserId = user.Id,
                                        AccessToken = AccessToken,
                                        CreateDateTime = DateTime.UtcNow,
                                        ExpiryDateTime = DateTime.UtcNow.AddHours(1),
                                        IsRevoked = false
                                };

                                _context.Token.Add(addAccessToken);
                                await _context.SaveChangesAsync();

                                return _responseService.RequestResult(new RequestResultOutputDto<object>
                                {
                                        StatusCode = HttpContext.Response.StatusCode,
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
                public async Task<ActionResult<RequestResultOutputDto<object>>> GetUserProfile()
                {
                        var userId = HttpContext.Items["UserId"] as string;

                        if (string.IsNullOrEmpty(userId) == true)
                        {
                                return _responseService.ApiRequestResult<object>(HttpContext.Response.StatusCode, "請重新登入", false);
                        }

                        var result = await _context.UserProfile.Where(x => x.UserNo == int.Parse(userId)).FirstOrDefaultAsync();

                        if (result == null)
                        {
                                return _responseService.ApiRequestResult<object>(HttpContext.Response.StatusCode, "找不到資料", false);
                        }
                        else
                        {

                                return _responseService.RequestResult(new RequestResultOutputDto<object>
                                {
                                        StatusCode = HttpContext.Response.StatusCode,
                                        Message = "",
                                        Result = new UserProfileOutputDto
                                        {
                                                UserNo = result.UserNo,
                                                Name = result.Name,
                                                Email = result.Email,
                                                CountyCode = result.CountyCode,
                                                DistrictCode = result.DistrictCode,
                                                PostalCode = result.PostalCode,
                                                Address = result.Address,
                                                SexCode = result.SexCode,
                                                Birthday = result.Birthday,
                                        }
                                });
                        }
                }

                // 註冊帳號發送信件
                [HttpPost("PostSendMail")]
                public async Task<RequestResultOutputDto<object>> PostSendMail([FromBody] PostSendMailInputDto value)
                {
                        var mailHelper = await _mailHelper.SendMail(new EmailRequest
                        {
                                ToEmail = value.Email,
                                ToName = value.Email.Split('@')[0]
                        });

                        if (mailHelper == true)
                        {
                                return _responseService.ApiRequestResult<object>(HttpContext.Response.StatusCode, "發送成功", true);
                        }

                        return _responseService.ApiRequestResult<object>(HttpContext.Response.StatusCode, "發送失敗", false);
                }

                // 驗證OTP
                [HttpPost("PostValidOtp")]
                public async Task<RequestResultOutputDto<object>> PostValidOtp([FromBody] PostValidInputDto validOtp)
                {
                        var otp = _context.OtpVerification
                                .Where(x => x.Email == validOtp.Email && x.Otp == validOtp.Otp && x.ExpirationTime > DateTime.UtcNow && x.IsUsed == false)
                                .OrderByDescending(x => x.ExpirationTime)
                                .FirstOrDefault();

                        if (otp == null)
                        {
                                return _responseService.ApiRequestResult<object>(HttpContext.Response.StatusCode, "驗證碼輸入錯誤或過期", false);
                        }

                        otp.IsUsed = true;
                        otp.UpdateDateTime = DateTime.UtcNow;
                        _context.OtpVerification.Update(otp);
                        await _context.SaveChangesAsync();

                        //HttpContext.Session.Remove("Email");

                        return _responseService.ApiRequestResult<object>(HttpContext.Response.StatusCode, "驗證成功", true);
                }

                // 驗證Email
                [HttpPost("PostValidEmail")]
                public async Task<RequestResultOutputDto<object>> PostValidEmail([FromBody] PostSendMailInputDto value)
                {
                        var validEmail = await _context.User.Where(x => x.Email == value.Email).FirstOrDefaultAsync();

                        if (validEmail == null)
                        {
                                return _responseService.ApiRequestResult<object>(HttpContext.Response.StatusCode, "驗證成功", false);
                        }

                        return _responseService.ApiRequestResult<object>(HttpContext.Response.StatusCode, "此Email已註冊", true);
                }

                // 修改密碼
                [HttpPut("PutResetPassword")]
                public async Task<RequestResultOutputDto<object>> PutResetPassword([FromBody] RegisterInputDto value)
                {
                        var user = await _context.User.Where(x => x.Email == value.Email).FirstOrDefaultAsync();

                        if (user == null)
                        {
                                return _responseService.ApiRequestResult<object>(HttpContext.Response.StatusCode, "此Email尚未註冊", false);
                        }

                        var passwordHash = BCrypt.Net.BCrypt.HashPassword(value.Password);

                        user.Password = passwordHash;
                        user.ModifyDateTime = DateTime.UtcNow;

                        await _context.SaveChangesAsync();

                        return _responseService.ApiRequestResult<object>(HttpContext.Response.StatusCode, "修改成功", true);
                }

                // 取得縣市
                [HttpGet("GetLocation")]
                [Authorize]
                public async Task<RequestResultOutputDto<object>> GetLocation()
                {
                        var locations = await _context.Locations.ToListAsync();

                        var locationsGroupData = locations
                                .GroupBy(d => new { d.CountyName, d.CountyCode })
                                .Select((group) => new LocationOutputDto
                                {
                                        CountyName = group.Key.CountyName,
                                        CountyCode = group.Key.CountyCode,
                                        District = group.Select(d => new DistrictOutputDto
                                        {
                                                DistrictName = d.DistrictName,
                                                DistrictCode = d.DistrictCode,
                                                PostalCode = d.PostalCode
                                        }).ToList()
                                }).ToList();

                        return _responseService.RequestResult(new RequestResultOutputDto<object>
                        {
                                StatusCode = HttpContext.Response.StatusCode,
                                Message = "",
                                Result = locationsGroupData
                        });
                }

                // 修改個人資料
                [HttpPut("PutUserProfile")]
                [Authorize]
                public async Task<RequestResultOutputDto<object>> PutUserProfile([FromBody] UserProfileInputDto value)
                {
                        if (!ModelState.IsValid)
                        {
                                return _responseService.ApiRequestResult<object>(400, "請求格式錯誤", false);
                        }

                        var userId = HttpContext.Items["UserId"] as string;

                        var userProfile = await _context.UserProfile.Where(x => x.UserNo == int.Parse(userId)).FirstOrDefaultAsync();

                        if (userProfile == null)
                        {
                                return _responseService.ApiRequestResult<object>(400, "使用者資料不存在", false);
                        }

                        userProfile.Name = !string.IsNullOrEmpty(value.Name) ? value.Name : userProfile.Name;
                        userProfile.Email = !string.IsNullOrEmpty(value.Email) ? value.Email : userProfile.Email;
                        userProfile.CountyCode = value.CountyCode;
                        userProfile.DistrictCode = value.DistrictCode;
                        userProfile.PostalCode = value.PostalCode;
                        userProfile.Address = !string.IsNullOrEmpty(value.Address) ? value.Address : userProfile.Address;
                        userProfile.SexCode = value.SexCode;
                        userProfile.Birthday = value.Birthday != DateTime.MinValue ? value.Birthday : userProfile.Birthday;
                        userProfile.ModifyDateTime = DateTime.UtcNow;

                        await _context.SaveChangesAsync();

                        return _responseService.ApiRequestResult<object>(HttpContext.Response.StatusCode, "使用者資料更新成功", true);
                }
        }
}
