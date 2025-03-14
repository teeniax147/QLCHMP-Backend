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
using QuanLyCuaHangMyPham.IdentityModels;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using QuanLyCuaHangMyPham.Services.VNPAY;
using QuanLyCuaHangMyPham.Services.MOMO.Services;
using QuanLyCuaHangMyPham.Services.MOMO.Models.Momo;
var builder = WebApplication.CreateBuilder(args);

// Đặt tên cho CORS Policy
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
// Thêm CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policyBuilder =>
        {
            policyBuilder.AllowAnyOrigin()
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
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // Giới hạn 50MB
});
builder.Services.AddSingleton<IVnpay, Vnpay>();
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
        Description = "Nhap JWT cung voi Bearer vao truong du lieu",
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
//Momo API Payment
builder.Services.Configure<MomoOptionModel>(builder.Configuration.GetSection("MomoAPI"));
builder.Services.AddScoped<IMomoService, MomoService>();
// Đăng ký EmailService để sử dụng qua Dependency Injection (DI)
builder.Services.AddScoped<IEmailService, EmailService>();
// Đăng ký IMomoService với lớp triển khai MomoService
builder.Services.AddEndpointsApiExplorer();  // Thêm dịch vụ để khám phá các API
// Đăng ký MemoryCache
builder.Services.AddMemoryCache();
// Thêm dịch vụ session
builder.Services.AddDistributedMemoryCache(); // Lưu trữ trong bộ nhớ
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Thời gian hết hạn của session
    options.Cookie.HttpOnly = true; // Chỉ có thể truy cập session qua HTTP
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsStaging() || app.Environment.IsProduction()) // Cấu hình môi trường tùy theo nhu cầu
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        c.DefaultModelsExpandDepth(-1); // Ẩn các model chi tiết nếu muốn
        c.RoutePrefix = "swagger"; // Swagger ở root URL
    });
}
//// Cấu hình ứng dụng trong môi trường Production
//if (app.Environment.IsProduction())
//{
//    // Thực hiện cấu hình đặc biệt cho môi trường Production
//    // Ví dụ: Cấu hình logging, bảo mật, caching, v.v.
//    app.UseHttpsRedirection();
//    app.UseHsts();  // Enable HTTP Strict Transport Security
//}
Console.WriteLine($"Moi truong hien tai: {app.Environment.EnvironmentName}");
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = new FileExtensionContentTypeProvider
    {
        Mappings =
        {
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg"
        }
    }
});


// Sử dụng Https
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting(); // Đảm bảo routing được thiết lập sau UseStaticFiles
// Sử dụng CORS
app.UseCors("AllowFrontend");

// Sử dụng Authentication (JWT)
app.UseAuthentication();

// Sử dụng Authorization (Identity roles, claims)
app.UseAuthorization();
app.UseSession();
app.MapControllers();

app.Run();