﻿using Microsoft.AspNetCore.Authentication.JwtBearer;
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
using Newtonsoft.Json;
using QuanLyCuaHangMyPham.Services.Email;
using QuanLyCuaHangMyPham.Services.Export;
using QuanLyCuaHangMyPham.Services.PAYMENT.MOMO.Services;
using QuanLyCuaHangMyPham.Services.PAYMENT.MOMO.Models.Momo;
using QuanLyCuaHangMyPham.Services.PAYMENT.VNPAY;
using QuanLyCuaHangMyPham.Utilities;
using QuanLyCuaHangMyPham.Facades;
using QuanLyCuaHangMyPham.Services.ORDERS.Facades;
using QuanLyCuaHangMyPham.Services.ORDERS;
using QuanLyCuaHangMyPham.States;
using QuanLyCuaHangMyPham.Repositories.Cart;
using QuanLyCuaHangMyPham.Mediators;
using QuanLyCuaHangMyPham.Mediators.Cart;
using QuanLyCuaHangMyPham.Handlers.Cart;
using QuanLyCuaHangMyPham.Services.Analytics;
using QuanLyCuaHangMyPham.Services.Cart;
// Thêm các using statements cần thiết
using QuanLyCuaHangMyPham.Services.PROMOTIONS;
using QuanLyCuaHangMyPham.Services.PROMOTIONS.Flyweight;
using QuanLyCuaHangMyPham.Services.PROMOTIONS.Observer;
using QuanLyCuaHangMyPham.Services.PROMOTIONS.Observer.Observers;
var builder = WebApplication.CreateBuilder(args);

// Đặt tên cho CORS Policy
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
// Thêm CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policyBuilder =>
        {
            policyBuilder
                .WithOrigins(
                    "https://glamour.io.vn",
                    "https://api.glamour.io.vn",
                    "https://www.glamour.io.vn",
                    "https://www.api.glamour.io.vn",
                    "http://localhost:5173",
                    "https://localhost:5173",
                    "http://localhost:5001",
                    "https://localhost:5001",
                    "https://cutexiu.teeniax.io.vn",
                    "https://api.cutexiu.teeniax.io.vn",
                    "https://www.cutexiu.teeniax.io.vn",
                    "https://www.api.cutexiu.teeniax.io.vn",
                    "https://bubbles.teeniax.io.vn",
                    "https://api.bubbles.teeniax.io.vn",
                    "https://www.bubbles.teeniax.io.vn",
                    "https://www.api.bubbles.teeniax.io.vn"
                )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();

        });
});

// Đăng ký dịch vụ DbContext với chuỗi kết nối đến cơ sở dữ liệu của bạn
// ĐÃ ĐƯỢC SỬA ĐỔI: Thêm cấu hình timeout và retry
builder.Services.AddDbContext<QuanLyCuaHangMyPhamContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("QuanLyCuaHangMyPhamContext"),
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure();
            sqlOptions.CommandTimeout(6000); // Tăng timeout để giải quyết vấn đề WITH
        }));

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
        options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

    });
builder.Services.AddTransient<IExportService, ExportService>();

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
builder.Services.AddEmailServices();
// Trong Program.cs
// Đăng ký Observer Pattern
builder.Services.AddSingleton<IPromotionSubject, PromotionSubject>();
builder.Services.AddScoped<IPromotionObserver, CustomerNotifier>();
builder.Services.AddScoped<IPromotionObserver, ProductPriceUpdater>();

// Đăng ký Flyweight Pattern
builder.Services.AddSingleton<PromotionFlyweightFactory>();

// Đăng ký Promotion Service
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<QuanLyCuaHangMyPham.Handlers.Favorites.FavoriteHandlerChain>();
// Đăng ký IMomoService với lớp triển khai MomoService
// Trong Program.cs

// Đăng ký OrderStateContext
builder.Services.AddScoped<OrderStateContext>();

// Đăng ký OrderService
builder.Services.AddScoped<IOrderService, OrderService>();

// Đăng ký OrderFacade
builder.Services.AddScoped<IOrderFacade, OrderFacade>();
// Thêm vào phương thức ConfigureServices trong Program.cs
builder.Services.AddScoped<QuanLyCuaHangMyPham.Services.Categories.CategoryCompositeService>();
builder.Services.AddEndpointsApiExplorer();  // Thêm dịch vụ để khám phá các API

// Đăng ký MemoryCache
builder.Services.AddMemoryCache();

// Thêm dịch vụ session
builder.Services.AddDistributedMemoryCache(); // Lưu trữ trong bộ nhớ
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".Glamour.Session";
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;

    // Đảm bảo cookie có thể được chia sẻ giữa các domain khác nhau
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Trong phương thức ConfigureServices của Startup.cs hoặc Program.cs
builder.Services.AddHttpClient();

// Đăng ký HttpContextAccessor (cần thiết cho Session)
builder.Services.AddHttpContextAccessor();

// Command Pattern và Mediator Pattern - Đăng ký các dịch vụ

// Đăng ký CartRepository (Receiver trong Command Pattern)
builder.Services.AddScoped<QuanLyCuaHangMyPham.Repositories.Cart.CartRepository>();

// Đăng ký CartMediator (Mediator trong Mediator Pattern)
builder.Services.AddScoped<QuanLyCuaHangMyPham.Mediators.IMediator, QuanLyCuaHangMyPham.Mediators.Cart.CartMediator>();
// Đăng ký CartCommandHandler (Invoker trong Command Pattern)
builder.Services.AddScoped<QuanLyCuaHangMyPham.Handlers.Cart.CartCommandHandler>();

// Đăng ký các Colleagues cho Mediator Pattern
builder.Services.AddScoped<QuanLyCuaHangMyPham.Services.Cart.CartNotificationService>();
builder.Services.AddScoped<QuanLyCuaHangMyPham.Services.Analytics.CartAnalyticsService>();

// Đăng ký CartMediatorConfigurator để cấu hình kết nối Mediator-Colleague
builder.Services.AddScoped<QuanLyCuaHangMyPham.Mediators.Cart.CartMediatorConfigurator>();

// XÂY DỰNG ỨNG DỤNG - SAU DÒNG NÀY KHÔNG ĐƯỢC THÊM DỊCH VỤ
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    var promotionSubject = serviceProvider.GetRequiredService<IPromotionSubject>();

    // Đăng ký tất cả các observer với subject
    var observers = serviceProvider.GetServices<IPromotionObserver>();
    foreach (var observer in observers)
    {
        promotionSubject.Attach(observer);
    }

    // Log thông tin
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Đã đăng ký {Count} observers cho PromotionSubject", observers.Count());
}
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

// ĐÃ XÓA: Đoạn code đăng ký DbContext ở đây vì nó đã được chuyển lên trên

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