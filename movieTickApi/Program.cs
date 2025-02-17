using Microsoft.EntityFrameworkCore;
using movieTickApi.Models;
using movieTickApi.Service;
using movieTickApi.Helper;
using movieTickApi.Models.Users;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

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

if (app.Environment.IsDevelopment())
{
        app.UseSwagger();
        app.UseSwaggerUI();
}

//app.UseSession();

app.UseCors("AllowSpecificOrigins");

app.UseHttpsRedirection();

app.UseMiddleware<TokenValidationMiddleware>();

//app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
