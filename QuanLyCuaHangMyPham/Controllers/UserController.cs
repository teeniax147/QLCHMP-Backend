using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using QuanLyCuaHangMyPham.Services;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using QuanLyCuaHangMyPham.Data;
using Microsoft.EntityFrameworkCore;
using QuanLyCuaHangMyPham.Models;
using QuanLyCuaHangMyPham.IdentityModels;

namespace QuanLyCuaHangMyPham.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ILogger<UsersController> _logger;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;
        private readonly QuanLyCuaHangMyPhamContext _context;
        private readonly IConfiguration _configuration; // Inject configuration

        public UsersController(UserManager<ApplicationUser> userManager,
                        RoleManager<ApplicationRole> roleManager,
                        ILogger<UsersController> logger,
                        IEmailService emailService,
                        IMemoryCache cache,
                        QuanLyCuaHangMyPhamContext context,
                        IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _emailService = emailService;
            _cache = cache;
            _context = context;
            _configuration = configuration; // Set configuration
        }

        // Đăng ký người dùng với OTP xác thực qua email (mặc định là Customer)
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                _cache.Remove(request.Email + "_otp");
                _cache.Remove(request.Email + "_data");
                return Conflict("Email này đã được đăng ký.");
            }

            var existingUsername = await _userManager.FindByNameAsync(request.Username);
            if (existingUsername != null)
            {
                _cache.Remove(request.Email + "_otp");
                _cache.Remove(request.Email + "_data");
                return Conflict("Tên đăng nhập đã tồn tại.");
            }

            // Tạo mã OTP và lưu trữ trong cache
            var otp = _emailService.GenerateOTP();
            _cache.Set(request.Email + "_otp", otp, TimeSpan.FromMinutes(1));
            _cache.Set(request.Email + "_data", request, TimeSpan.FromMinutes(1));
            // Lưu thông tin đăng ký vào database với trạng thái "chờ xác nhận"
            
            // Gửi email OTP
            var subject = "Xác thực đăng ký tài khoản của bạn";
            var message = $"Vui lòng sử dụng mã OTP sau để đăng ký tài khoản của bạn: {otp}.\n" +
                          "Lưu ý: Mã OTP chỉ có hiệu lực trong vòng 5 phút.";
            await _emailService.SendEmailAsync(request.Email, subject, message);

            return Ok("Vui lòng kiểm tra email của bạn để xác thực đăng ký.");
        }

        // Xác thực OTP dùng chung cho cả đăng ký và quên mật khẩu
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOTP([FromBody] OtpVerifyRequest request)
        {
            // Kiểm tra các trường bắt buộc
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Otp) || string.IsNullOrEmpty(request.OtpPurpose))
            {
                return BadRequest("Email, mã OTP và mục đích là bắt buộc.");
            }

            // Log để kiểm tra việc nhận request
            _logger.LogInformation($"Received OTP verification request for {request.Email}, Purpose: {request.OtpPurpose}");

            // Kiểm tra OTP trong cache
            if (_cache.TryGetValue(request.Email + "_otp", out string storedOtp))
            {
                // Log OTP từ cache và OTP được gửi từ request
                _logger.LogInformation($"Stored OTP: {storedOtp}, Input OTP: {request.Otp}");

                // So sánh OTP
                if (request.Otp == storedOtp)
                {
                    // Xóa OTP khỏi cache sau khi xác thực thành công
                    _cache.Remove(request.Email + "_otp");
                    _logger.LogInformation($"OTP for {request.Email} successfully validated and removed.");

                    // Kiểm tra mục đích OTP
                    if (request.OtpPurpose == "register")
                    {
                        // Xác thực đăng ký người dùng
                        if (_cache.TryGetValue(request.Email + "_data", out RegisterRequest cachedRequest))
                        {
                            _logger.LogInformation($"Register data found in cache for {request.Email}");

                            // Tạo tài khoản người dùng sau khi xác thực OTP thành công
                            var user = new ApplicationUser
                            {
                                UserName = cachedRequest.Username,
                                Email = cachedRequest.Email,
                                PhoneNumber = cachedRequest.Phone,
                                FirstName = cachedRequest.FirstName,
                                LastName = cachedRequest.LastName,
                                Address = cachedRequest.Address,
                                EmailConfirmed = true  // Đánh dấu email đã được xác nhận
                            };

                            // Lưu tài khoản vào cơ sở dữ liệu
                            var result = await _userManager.CreateAsync(user, cachedRequest.Password);
                            if (!result.Succeeded)
                            {
                                _logger.LogError($"Error creating user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                                return BadRequest(result.Errors);
                            }

                            // Gán vai trò "Customer"
                            await _userManager.AddToRoleAsync(user, "Customer");

                            // Xóa thông tin đăng ký khỏi cache sau khi tạo tài khoản thành công
                            _cache.Remove(request.Email + "_data");
                            _logger.LogInformation($"User registered successfully for {request.Email}, removed cached data.");

                            return Ok("Xác thực thành công. Tài khoản của bạn đã được tạo.");
                        }
                        else
                        {
                            _logger.LogError($"Register data not found in cache for {request.Email}");
                            return BadRequest("Không tìm thấy thông tin người dùng trong cache.");
                        }
                    }
                    else if (request.OtpPurpose == "forgot-password")
                    {
                        // Xử lý quên mật khẩu
                        var user = await _userManager.FindByEmailAsync(request.Email);
                        if (user == null)
                        {
                            _logger.LogError($"User not found with email {request.Email}.");
                            return BadRequest("Không tìm thấy người dùng.");
                        }

                        // Lưu lại trạng thái đã xác thực OTP cho quy trình đặt lại mật khẩu
                        _cache.Set(request.Email + "_otp_verified", true, TimeSpan.FromMinutes(10));

                        _logger.LogInformation($"OTP for forgot-password verified successfully for {request.Email}.");
                        return Ok("OTP hợp lệ. Bạn có thể tiếp tục đặt lại mật khẩu.");
                    }
                    else
                    {
                        _logger.LogError($"Invalid OTP purpose: {request.OtpPurpose}");
                        return BadRequest("Mục đích OTP không hợp lệ.");
                    }
                }
                else
                {
                    // OTP không khớp
                    _logger.LogWarning($"OTP mismatch for {request.Email}. Expected: {storedOtp}, Got: {request.Otp}");
                    return BadRequest("Mã OTP không chính xác.");
                }
            }
            else
            {
                // OTP không tồn tại hoặc đã hết hạn
                _logger.LogWarning($"OTP for {request.Email} not found or expired.");
                return BadRequest("Mã OTP không chính xác hoặc đã hết hạn.");
            }
        }

        // Đăng nhập người dùng với JWT
        [HttpPost("login")]
        public async Task<IActionResult> LoginUser([FromBody] LoginRequest request)
        {
            var user = await _userManager.FindByNameAsync(request.EmailOrUsername)
                       ?? await _userManager.FindByEmailAsync(request.EmailOrUsername);

            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            {
                return Unauthorized("Tên đăng nhập hoặc mật khẩu không đúng.");
            }

            var token = GenerateJwtToken(user);
            return Ok(new { token });
        }

        private string GenerateJwtToken(ApplicationUser user)
        {
            var claims = new List<Claim>
{
    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),  // Chuyển đổi user.Id thành string
    new Claim(JwtRegisteredClaimNames.Email, user.Email),
    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())  // Chuyển đổi user.Id thành string
};

            // Kiểm tra lại giá trị key từ config có đúng không
            var keyString = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(keyString))
            {
                throw new InvalidOperationException("Secret key is not set in configuration");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Kiểm tra lại giá trị thời gian hết hạn từ config có đúng không
            if (!int.TryParse(_configuration["Jwt:ExpireMinutes"], out int expireMinutes))
            {
                throw new InvalidOperationException("Expire time is not valid");
            }

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(expireMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Gửi OTP để khôi phục mật khẩu
        [HttpPost("forgot-password")]
        public IActionResult ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var user = _userManager.Users.FirstOrDefault(u => u.Email == request.Email);
            if (user == null)
            {
                return BadRequest("Không tìm thấy người dùng với email này.");
            }

            var otp = _emailService.GenerateOTP();
            _cache.Set(request.Email + "_otp", otp, TimeSpan.FromMinutes(5));

            var subject = "Mã OTP để khôi phục mật khẩu";
            var message = $"Vui lòng sử dụng mã OTP sau để khôi phục mật khẩu của bạn: {otp}.\nMã OTP chỉ có hiệu lực trong 5 phút.";
            _emailService.SendEmailAsync(request.Email, subject, message);

            return Ok("Vui lòng kiểm tra email của bạn để nhập mã OTP.");
        }

        // Đặt lại mật khẩu sau khi OTP đã được xác thực
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Kiểm tra xem OTP đã được xác nhận hợp lệ chưa
            if (!_cache.TryGetValue(request.Email + "_otp_verified", out bool isOtpVerified) || !isOtpVerified)
            {
                return BadRequest("Bạn chưa xác nhận OTP hoặc OTP không hợp lệ.");
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return BadRequest("Không tìm thấy người dùng.");
            }

            // Đặt lại mật khẩu mới
            var result = await _userManager.ResetPasswordAsync(user, await _userManager.GeneratePasswordResetTokenAsync(user), request.NewPassword);
            if (result.Succeeded)
            {
                // Xóa trạng thái OTP xác nhận sau khi đặt lại mật khẩu thành công
                _cache.Remove(request.Email + "_otp_verified");
                return Ok("Mật khẩu của bạn đã được cập nhật thành công.");
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi khi đặt lại mật khẩu.");
            }
        }

        // Các lớp request khác
        public class RegisterRequest
        {
            [Required(ErrorMessage = "Email là bắt buộc")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
            public string Username { get; set; }

            [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
            [MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự")]
            public string Password { get; set; }

            [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
            [Compare("Password", ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp")]
            public string ConfirmPassword { get; set; }

            [Required(ErrorMessage = "Họ là bắt buộc")]
            public string LastName { get; set; }

            [Required(ErrorMessage = "Tên là bắt buộc")]
            public string FirstName { get; set; }

            [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
            public string Phone { get; set; }

            public string Address { get; set; } // Địa chỉ khách hàng
        }

        public class OtpVerifyRequest
        {
            [Required(ErrorMessage = "Email là bắt buộc")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Mã OTP là bắt buộc")]
            public string Otp { get; set; }

            [Required(ErrorMessage = "Mục đích OTP là bắt buộc")]
            public string OtpPurpose { get; set; } // "register" hoặc "forgot-password"
        }

        public class ForgotPasswordRequest
        {
            [Required(ErrorMessage = "Email là bắt buộc")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            public string Email { get; set; }
        }

        public class ResetPasswordRequest
        {
            [Required(ErrorMessage = "Email là bắt buộc")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
            [MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự")]
            public string NewPassword { get; set; }

            [Required(ErrorMessage = "Xác nhận mật khẩu mới là bắt buộc")]
            [Compare("NewPassword", ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp")]
            public string ConfirmPassword { get; set; }
        }

        public class LoginRequest
        {
            public string EmailOrUsername { get; set; }  // Cho phép nhập vào cả email hoặc username
            public string Password { get; set; }
        }
    }
}