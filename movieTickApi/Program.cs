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
//        options.IdleTimeout = TimeSpan.FromMinutes(30); // �]�w Session 30 ��������
//        options.Cookie.HttpOnly = true; // ���� XSS ����
//        options.Cookie.IsEssential = true; // GDPR �W�d�U���M�i��
//        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // �u�� HTTPS �U�~�|�ǻ�
//        options.Cookie.SameSite = SameSiteMode.None; // �קK�s�������� Cookie
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

// ���U�I���A��
builder.Services.AddHostedService<TokenCleanupService>();

builder.Services.AddDbContext<WebDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
            // �����w�]�� (�Ҧp Guid �� 00000000-0000-0000-0000-000000000000)
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
                    // �Ȼs�ƨƥ�B�z
                    OnMessageReceived = context =>
                    {
                            // �b�o�̥i�H�d�I�ШD�ˬd Authorization ���Y
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

                                    context.NoResult(); // ������ҥ���

                                    // �L���^���e��isRepeatLogin�A���e�ݭ��s�ɤJ��n�J��
                                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                    context.Response.ContentType = "application/json";
                                    var errorResponse = new
                                    {
                                            StatusCode = 401,
                                            Message = "�Э��s�n�J",
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
                                                    Message = "�Э��s�n�J",
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
                    // jwt�M��]�mtoken�S�L��Ĳ�o���ͩR�g��
                    OnTokenValidated = async context =>
                    {
                            var request = context.HttpContext.Request;

                            // �ˬd�O�_�O��s token ���ШD
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
                                    // �o�O��s token �ШD�A�������L�ˬd
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
                                            Message = "token�w�L��",
                                            Result = new
                                            {
                                                    isRepeatLogin = false,
                                                    isReNewToken = true,
                                                    path = request.Path
                                            }
                                    };

                                    await context.Response.WriteAsJsonAsync(errorResponse);

                                    context.Fail("token�w�L��");
                            }
                    },
                    // jwt�M��]�mtoken�L��Ĳ�o���ͩR�g��
                    OnAuthenticationFailed = async context =>
                    {
                            var request = context.HttpContext.Request;
                            var tokenService = context.HttpContext.RequestServices.GetRequiredService<TokenService>();

                            // �P�_http  cookie�]�mrefresh token�O�_�L��
                            var IsTokenRevoked = await tokenService.IsTokenRevokedAsync();
                            if (IsTokenRevoked == true)
                            {
                                    // �L���^���e��isRepeatLogin�A���e�ݭ��s�ɤJ��n�J��
                                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                    context.Response.ContentType = "application/json";
                                    var errorResponse = new
                                    {
                                            StatusCode = 401,
                                            Message = "�Э��s�n�J",
                                            Result = new
                                            {
                                                    isRepeatLogin = true,
                                                    isReNewToken = false
                                            }
                                    };
                                    await context.Response.WriteAsJsonAsync(errorResponse);
                                    context.Fail("�Э��s�n�J");
                            } 
                            else
                            {
                                    // �ˬd�O�_�O��s token ���ШD
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
                                            // �o�O��s token �ШD�A�������L�ˬd
                                            return;
                                    }
                                    // refresh token�S�L���A��jwt�M��]�mtoken�L���C�h�^���e��isReNewToken���e�ݭ��s���o�s��token
                                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                    context.Response.ContentType = "application/json";
                                    var errorResponse = new
                                    {
                                            StatusCode = 401,
                                            Message = "token�w�L��",
                                            Result = new
                                            {
                                                    isRepeatLogin = false,
                                                    isReNewToken = true
                                            }
                                    };
                                    await context.Response.WriteAsJsonAsync(errorResponse);
                                    context.Fail("token�w�L��");
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
