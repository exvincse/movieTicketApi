using Microsoft.EntityFrameworkCore;
using movieTickApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using movieTickApi.Service;
using movieTickApi.Helper;
using movieTickApi.Models.Users;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

//builder.Services.AddDistributedMemoryCache();
//builder.Services.AddSession(options =>
//{
//        options.IdleTimeout = TimeSpan.FromMinutes(30); // 設定 Session 30 分鐘有效
//        options.Cookie.HttpOnly = true; // 防止 XSS 攻擊
//        options.Cookie.IsEssential = true; // GDPR 規範下仍然可用
//        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // 只有 HTTPS 下才會傳遞
//        options.Cookie.SameSite = SameSiteMode.None; // 避免瀏覽器阻擋 Cookie
//});

builder.Services.AddCors(options =>
{
        options.AddPolicy("AllowSpecificOrigins", builder =>
        {
                builder.WithOrigins("http://localhost:8080")
                       .AllowCredentials()
                       .AllowAnyHeader()
                       .AllowAnyMethod();
        });
});

// 註冊背景服務
builder.Services.AddHostedService<TokenCleanupService>();

builder.Services.AddDbContext<WebDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
            // 忽略預設值 (例如 Guid 的 00000000-0000-0000-0000-000000000000)
            options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault;
    });


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:KEY"]))
            };

            options.Events = new JwtBearerEvents
            {
                    // 客製化事件處理
                    OnMessageReceived = context =>
                    {
                            // 在這裡可以攔截請求檢查 Authorization 標頭
                            if (
                                    !context.Request.Headers.ContainsKey("Authorization") &&
                                    !context.Request.Path.StartsWithSegments("/api/User/Login") && 
                                    !context.Request.Path.StartsWithSegments("/api/User/PostSendMail") && 
                                    !context.Request.Path.StartsWithSegments("/api/User/PostValidOtp") &&
                                    !context.Request.Path.StartsWithSegments("/api/User/PostValidEmail") &&
                                    !context.Request.Path.StartsWithSegments("/api/User/GetOtpEmail") &&
                                    !context.Request.Path.StartsWithSegments("/api/User/PostRegister")
                            )
                            {
                                    if (context.Request.Path.StartsWithSegments("/api/User/ReFreshToken"))
                                    {
                                            return Task.CompletedTask;
                                    }

                                    context.NoResult(); // 表示驗證失敗

                                    // 過期回給前端isRepeatLogin，讓前端重新導入到登入頁
                                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                    context.Response.ContentType = "application/json";
                                    var errorResponse = new
                                    {
                                            StatusCode = 401,
                                            Message = "請重新登入",
                                            Result = new
                                            {
                                                    isRepeatLogin = true,
                                                    isReNewToken = false
                                            }
                                    };

                                    if (string.IsNullOrEmpty(context.Request.Cookies["refreshToken"]) == false)
                                    {
                                            errorResponse = new
                                            {
                                                    StatusCode = 401,
                                                    Message = "請重新登入",
                                                    Result = new
                                                    {
                                                            isRepeatLogin = false,
                                                            isReNewToken = true
                                                    }
                                            };
                                    }

                                    return context.Response.WriteAsJsonAsync(errorResponse);
                            }
                            return Task.CompletedTask;
                    },
                    // jwt套件設置token沒過期觸發的生命週期
                    OnTokenValidated = async context =>
                    {
                            var request = context.HttpContext.Request;

                            // 檢查是否是刷新 token 的請求
                            if (
                                    request.Path.StartsWithSegments("/api/User/RefreshToken") ||
                                    request.Path.StartsWithSegments("/api/User/Login") ||
                                    request.Path.StartsWithSegments("/api/User/PostSendMail") ||
                                    request.Path.StartsWithSegments("/api/User/PostValidOtp") ||
                                    request.Path.StartsWithSegments("/api/User/PostValidEmail") ||
                                    request.Path.StartsWithSegments("/api/User/GetOtpEmail") ||
                                    request.Path.StartsWithSegments("/api/User/PostRegister")
                            )
                            {
                                    // 這是刷新 token 請求，直接跳過檢查
                                    return;
                            }

                            var tokenService = context.HttpContext.RequestServices.GetRequiredService<TokenService>();
                            var IsAccessTokenRevoked = await tokenService.IsAccessTokenRevoked();

                            if (IsAccessTokenRevoked == true)
                            {
                                    
                                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                    context.Response.ContentType = "application/json";
                                    var errorResponse = new
                                    {
                                            StatusCode = 401,
                                            Message = "token已過期",
                                            Result = new
                                            {
                                                    isRepeatLogin = false,
                                                    isReNewToken = true,
                                                    path = request.Path
                                            }
                                    };

                                    await context.Response.WriteAsJsonAsync(errorResponse);

                                    context.Fail("token已過期");
                            }
                    },
                    // jwt套件設置token過期觸發的生命週期
                    OnAuthenticationFailed = async context =>
                    {
                            var request = context.HttpContext.Request;
                            var tokenService = context.HttpContext.RequestServices.GetRequiredService<TokenService>();

                            // 判斷http  cookie設置refresh token是否過期
                            var IsTokenRevoked = await tokenService.IsTokenRevokedAsync();
                            if (IsTokenRevoked == true)
                            {
                                    // 過期回給前端isRepeatLogin，讓前端重新導入到登入頁
                                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                    context.Response.ContentType = "application/json";
                                    var errorResponse = new
                                    {
                                            StatusCode = 401,
                                            Message = "請重新登入",
                                            Result = new
                                            {
                                                    isRepeatLogin = true,
                                                    isReNewToken = false
                                            }
                                    };
                                    await context.Response.WriteAsJsonAsync(errorResponse);
                                    context.Fail("請重新登入");
                            } 
                            else
                            {
                                    // 檢查是否是刷新 token 的請求
                                    if (
                                            request.Path.StartsWithSegments("/api/User/RefreshToken") ||
                                            request.Path.StartsWithSegments("/api/User/Login") ||
                                            request.Path.StartsWithSegments("/api/User/PostSendMail") ||
                                            request.Path.StartsWithSegments("/api/User/PostValidOtp") ||
                                            request.Path.StartsWithSegments("/api/User/PostValidEmail") ||
                                            request.Path.StartsWithSegments("/api/User/GetOtpEmail") ||
                                            request.Path.StartsWithSegments("/api/User/PostRegister")
                                    )
                                    {
                                            // 這是刷新 token 請求，直接跳過檢查
                                            return;
                                    }
                                    // refresh token沒過期，但jwt套件設置token過期。則回給前端isReNewToken讓前端重新換發新的token
                                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                    context.Response.ContentType = "application/json";
                                    var errorResponse = new
                                    {
                                            StatusCode = 401,
                                            Message = "token已過期",
                                            Result = new
                                            {
                                                    isRepeatLogin = false,
                                                    isReNewToken = true
                                            }
                                    };
                                    await context.Response.WriteAsJsonAsync(errorResponse);
                                    context.Fail("token已過期");
                            }
                    }
            };
    });

builder.Services.AddAutoMapper(typeof(Program).Assembly);
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<ResponseService>();

var smtpSettings = configuration.GetSection("SmtpSettings").Get<SmtpSettings>();
if (smtpSettings != null)
{
        builder.Services.AddSingleton(smtpSettings);
        builder.Services.AddScoped<MailHelper>();
}

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
        app.UseSwagger();
        app.UseSwaggerUI();
}

//app.UseSession();

//app.UseMiddleware<TokenValidation>();

app.UseCors("AllowSpecificOrigins");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
