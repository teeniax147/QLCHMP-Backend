using Microsoft.EntityFrameworkCore;
using QuanLyCuaHangMyPham.Services;
using System.Configuration;
using MailKit.Net.Smtp;
using MimeKit;
using QuanLyCuaHangMyPham.Data;
var builder = WebApplication.CreateBuilder(args);

// Đặt tên cho CORS Policy
var MyAllowSpecificOrigins = "AllowSpecificOrigins";

// Thêm CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(MyAllowSpecificOrigins,
        builder =>
        {
            builder.WithOrigins("http://localhost:5173")  // Địa chỉ ứng dụng React (Vite)
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// Đăng ký dịch vụ DbContext với chuỗi kết nối đến cơ sở dữ liệu của bạn
builder.Services.AddDbContext<QuanLyCuaHangMyPhamContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("QuanLyCuaHangMyPhamContext"))); 
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

app.UseHttpsRedirection();

// Sử dụng CORS
app.UseCors(MyAllowSpecificOrigins);

app.UseAuthorization();

app.MapControllers();

app.Run();