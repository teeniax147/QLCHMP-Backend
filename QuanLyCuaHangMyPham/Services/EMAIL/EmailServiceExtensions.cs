using Microsoft.Extensions.DependencyInjection;
using QuanLyCuaHangMyPham.Services.Email.Strategies;
using System;

namespace QuanLyCuaHangMyPham.Services.Email
{
    /// <summary>
    /// Extension methods để đăng ký các dịch vụ liên quan đến Email với DI container
    /// </summary>
    public static class EmailServiceExtensions
    {
        /// <summary>
        /// Đăng ký dịch vụ Email và các chiến lược
        /// </summary>
        public static IServiceCollection AddEmailServices(this IServiceCollection services)
        {
            // Đăng ký các chiến lược cơ bản
            services.AddSingleton<PlainTextBodyStrategy>();
            services.AddSingleton<HtmlBodyStrategy>();

            // Đăng ký factory để chọn chiến lược theo loại
            services.AddSingleton<Func<string, IEmailBodyStrategy>>(serviceProvider =>
                strategyType =>
                {
                    return strategyType.ToLower() switch
                    {
                        "plain" => serviceProvider.GetRequiredService<PlainTextBodyStrategy>(),
                        "text" => serviceProvider.GetRequiredService<PlainTextBodyStrategy>(),
                        "html" => serviceProvider.GetRequiredService<HtmlBodyStrategy>(),
                        _ => throw new ArgumentException($"Không hỗ trợ loại chiến lược: {strategyType}")
                    };
                });

            // Đăng ký dịch vụ Email
            services.AddScoped<IEmailService, EmailService>();

            return services;
        }
    }
}