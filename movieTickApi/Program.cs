using Microsoft.EntityFrameworkCore;
using movieTickApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using movieTickApi.Service;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddCors(options =>
{
        options.AddPolicy("AllowSpecificOrigins", policy =>
        {
                policy.WithOrigins("http://localhost:8080").AllowAnyHeader().AllowAnyMethod();
        });
});

builder.Services.AddDbContext<WebDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

// Add services to the container.

builder.Services.AddControllers();

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
                    // jwt套件設置token沒過期觸發的生命週期
                    OnTokenValidated = async context =>
                    {
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
                                                    isReNewToken = true
                                            }
                                    };

                                    await context.Response.WriteAsJsonAsync(errorResponse);

                                    context.Fail("token已過期");
                            }
                    },
                    // jwt套件設置token過期觸發的生命週期
                    OnAuthenticationFailed = async context =>
                    {
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

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
        app.UseSwagger();
        app.UseSwaggerUI();
}

app.UseCors("AllowSpecificOrigins");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
