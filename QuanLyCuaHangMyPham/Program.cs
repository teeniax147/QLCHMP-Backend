using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuanLyCuaHangMyPham.Services;
using QuanLyCuaHangMyPham.Data;
using System.Text;
using MimeKit;
using MailKit.Net.Smtp;
using QuanLyCuaHangMyPham.Models;
using QuanLyCuaHangMyPham.IdentityModels;
var builder = WebApplication.CreateBuilder(args);

// Đặt tên cho CORS Policy
var MyAllowSpecificOrigins = "AllowSpecificOrigins";

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
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

// Đăng ký các dịch vụ cho API controllers
builder.Services.AddControllers();

// Cấu hình Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Đăng ký EmailService để sử dụng qua Dependency Injection (DI)
builder.Services.AddScoped<IEmailService, EmailService>();

// Đăng ký MemoryCache
builder.Services.AddMemoryCache();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsStaging()) // Cấu hình môi trường tùy theo nhu cầu
{
    app.UseSwagger();
    app.UseSwaggerUI();
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