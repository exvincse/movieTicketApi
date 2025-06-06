using Microsoft.EntityFrameworkCore;
using movieTickApi.Helper;
using movieTickApi.Models;
using movieTickApi.Models.Users;
using movieTickApi.Service;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

// 讀取環境變數
var env = builder.Environment.EnvironmentName;

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// 設定 CORS
builder.Services.AddCors(options =>
{
        options.AddPolicy("AllowSpecificOrigins", policy =>
        {
                policy.WithOrigins("http://localhost:8080", "http://192.168.137.1:8080")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
        });
});

// 註冊背景服務
builder.Services.AddHostedService<TokenCleanupService>();
builder.Services.AddHostedService<PayPalStatusService>();

// 設定資料庫
var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? throw new Exception("資料庫連線字串未設定");

builder.Services.AddDbContext<WebDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddHttpClient();

// 設定 JSON 解析
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
            options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault;
    });

builder.Services.AddAutoMapper(typeof(Program).Assembly);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
                Name = "Authorization",
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Description = "輸入格式為：Bearer  token"
        });

        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] {}
                }
            });
});

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<PayPalService>();
builder.Services.AddScoped<ResponseService>();

//  發信SMTP設定
var smtpSettings = configuration.GetSection("SmtpSettings").Get<SmtpSettings>();
if (smtpSettings != null)
{
        builder.Services.AddSingleton(smtpSettings);
        builder.Services.AddScoped<MailHelper>();
}

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.UseSwagger();

if (app.Environment.IsDevelopment())
{
        app.UseSwaggerUI();
}

app.UseCors("AllowSpecificOrigins");

app.UseDefaultFiles();
app.UseStaticFiles();

// 處理 Angular 部屬路由
app.Use(async (context, next) =>
{
        if (!context.Request.Path.StartsWithSegments("/api") && !System.IO.Path.HasExtension(context.Request.Path.Value))
        {
                context.Request.Path = "/index.html";
        }
        await next();
});

app.UseMiddleware<TokenValidationMiddleware>();

app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");
app.Run();
