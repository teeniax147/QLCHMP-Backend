using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuanLyCuaHangMyPham.Services;
using QuanLyCuaHangMyPham.Data;
using System.Text;
using MimeKit;
using MailKit.Net.Smtp;
using QuanLyCuaHangMyPham.Models;
using QuanLyCuaHangMyPham.Services;
using QuanLyCuaHangMyPham.IdentityModels;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
var builder = WebApplication.CreateBuilder(args);

// Đặt tên cho CORS Policy
var MyAllowSpecificOrigins = "AllowSpecificOrigins";
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
// Thêm CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(MyAllowSpecificOrigins,
        policyBuilder =>
        {
            policyBuilder.WithOrigins("http://localhost:5173")  // Địa chỉ ứng dụng React (Vite)
                          .AllowAnyMethod()
                          .AllowAnyHeader();
        });
});

// Đăng ký dịch vụ DbContext với chuỗi kết nối đến cơ sở dữ liệu của bạn
builder.Services.AddDbContext<QuanLyCuaHangMyPhamContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("QuanLyCuaHangMyPhamContext")));

// Thêm ASP.NET Identity để quản lý người dùng
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
    .AddEntityFrameworkStores<QuanLyCuaHangMyPhamContext>()
    .AddDefaultTokenProviders();

// Cấu hình JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var keyString = builder.Configuration["Jwt:Key"];
    if (string.IsNullOrEmpty(keyString))
    {
        throw new InvalidOperationException("JWT key is not set in configuration");
    }
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = key
    };
});

// Đăng ký các dịch vụ cho API controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new DateTimeFormatConverter("dd/MM/yyyy HH:mm:ss")); // Add custom DateTime format here
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
    });
builder.Services.AddTransient<ExportService>();
// Cấu hình Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Quản Lý Cửa Hàng Mỹ Phẩm API -", Version = "v1" });

    // Thêm phần cấu hình cho JWT Authentication
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please insert JWT with Bearer into field",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
    

});

// Đăng ký EmailService để sử dụng qua Dependency Injection (DI)
builder.Services.AddScoped<IEmailService, EmailService>();

// Đăng ký MemoryCache
builder.Services.AddMemoryCache();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsStaging()) // Cấu hình môi trường tùy theo nhu cầu
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        c.DefaultModelsExpandDepth(-1); // Ẩn các model chi tiết nếu muốn
    });
}

// Sử dụng Https
app.UseHttpsRedirection();

// Sử dụng CORS
app.UseCors(MyAllowSpecificOrigins);

// Sử dụng Authentication (JWT)
app.UseAuthentication();

// Sử dụng Authorization (Identity roles, claims)
app.UseAuthorization();

// Cấu hình route cho controllers
app.MapControllers();

app.Run();