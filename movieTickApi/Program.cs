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
                    // jwt�M��]�mtoken�S�L��Ĳ�o���ͩR�g��
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
                    },
                    // jwt�M��]�mtoken�L��Ĳ�o���ͩR�g��
                    OnAuthenticationFailed = async context =>
                    {
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
