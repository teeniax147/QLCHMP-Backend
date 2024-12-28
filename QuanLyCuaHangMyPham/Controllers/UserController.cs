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
using Microsoft.AspNetCore.Authorization;
using static QuanLyCuaHangMyPham.Controllers.UsersController;

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

            // Generate OTP and send email
            var otp = _emailService.GenerateOTP();
            _cache.Set(request.Email + "_otp", otp, TimeSpan.FromMinutes(5));
            _cache.Set(request.Email + "_data", request, TimeSpan.FromMinutes(5));

            // Send OTP email
            var subject = "Xác thực đăng ký tài khoản của bạn";
            var message = $"Vui lòng sử dụng mã OTP sau để đăng ký tài khoản của bạn: {otp}.\nLưu ý: Mã OTP chỉ có hiệu lực trong vòng 5 phút.";
            await _emailService.SendEmailAsync(request.Email, subject, message);

            return Ok("Vui lòng kiểm tra email của bạn để xác thực đăng ký.");
        }

        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.OtpPurpose))
            {
                return BadRequest("Email và mục đích là bắt buộc.");
            }

            _logger.LogInformation($"Resend OTP requested for {request.Email}, Purpose: {request.OtpPurpose}");

            if (request.OtpPurpose == "register")
            {
                if (!_cache.TryGetValue(request.Email + "_data", out RegisterRequest cachedRequest))
                {
                    _logger.LogWarning($"Register cache not found for {request.Email}.");
                    return BadRequest("Thông tin đăng ký không tồn tại hoặc đã hết hạn.");
                }
            }
            else if (request.OtpPurpose == "forgot-password")
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    _logger.LogWarning($"User not found for email {request.Email}.");
                    return BadRequest("Không tìm thấy người dùng với email này.");
                }
            }
            else
            {
                _logger.LogError($"Invalid OTP purpose: {request.OtpPurpose}");
                return BadRequest("Mục đích OTP không hợp lệ.");
            }

            var otp = _emailService.GenerateOTP();
            _cache.Set(request.Email + "_otp", otp, TimeSpan.FromMinutes(5));

            var subject = request.OtpPurpose == "register"
                    ? "Xác thực đăng ký tài khoản của bạn"
                    : "Mã OTP để khôi phục mật khẩu";
            var message = $"Vui lòng sử dụng mã OTP sau để {(request.OtpPurpose == "register" ? "đăng ký tài khoản của bạn" : "khôi phục mật khẩu của bạn")}: {otp}.\nMã OTP có hiệu lực trong 5 phút.";

            await _emailService.SendEmailAsync(request.Email, subject, message);

            _logger.LogInformation($"OTP resent successfully to {request.Email}.");
            return Ok("Mã OTP đã được gửi lại. Vui lòng kiểm tra email của bạn.");
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

                            // Tạo bản ghi Customer tương ứng
                            var customer = new Customer
                            {
                                UserId = user.Id,
                                Address = cachedRequest.Address,
                                TotalSpending = 0, // Giá trị mặc định
                                MembershipLevelId = 1, // Mức thành viên mặc định
                                CreatedAt = DateTime.Now
                            };

                            // Lưu bản ghi Customer vào cơ sở dữ liệu
                            _context.Customers.Add(customer);
                            await _context.SaveChangesAsync();

                            // Xóa thông tin đăng ký khỏi cache sau khi tạo tài khoản thành công
                            _cache.Remove(request.Email + "_data");
                            _logger.LogInformation($"User registered successfully for {request.Email}, removed cached data.");

                            return Ok("Xác thực thành công. Tài khoản của bạn đã được tạo và thông tin khách hàng đã được thêm.");
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
        private async Task<string> GenerateJwtToken(ApplicationUser user)
        {
            var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
    };

            // Lấy các vai trò của người dùng và thêm chúng vào claims
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

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
        // Đăng nhập người dùng với JWT
        [HttpPost("login")]
        public async Task<IActionResult> LoginUser([FromBody] LoginRequest request)
        {
            // Tìm người dùng dựa trên tên đăng nhập hoặc email
            var user = await _userManager.FindByNameAsync(request.EmailOrUsername)
                       ?? await _userManager.FindByEmailAsync(request.EmailOrUsername);

            // Nếu không tìm thấy người dùng
            if (user == null)
            {
                return Unauthorized(new { message = "Tên đăng nhập hoặc email không đúng." });
            }

            // Nếu tìm thấy người dùng nhưng mật khẩu không đúng
            if (!await _userManager.CheckPasswordAsync(user, request.Password))
            {
                return Unauthorized(new { message = "Mật khẩu không đúng." });
            }

            // Lấy danh sách vai trò của người dùng
            var roles = await _userManager.GetRolesAsync(user);

            // Tạo token JWT nếu đăng nhập thành công
            var token = GenerateJwtToken(user);

            // Trả về thông báo thành công, token và thông tin vai trò
            return Ok(new
            {
                message = "Đăng nhập thành công.",
                token = token,
                userId = user.Id,
                userName = user.UserName,
                email = user.Email,
                roles = roles // Trả về danh sách vai trò của người dùng
            });
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
        [Authorize]
        [HttpPut("update")]
        public async Task<IActionResult> UpdateUserInfo([FromBody] UpdateUserInfoRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Lấy thông tin UserId từ token JWT
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized("Người dùng chưa được xác thực.");
            }

            // Tìm người dùng dựa trên UserId
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("Không tìm thấy người dùng.");
            }

            // Cập nhật thông tin người dùng
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.PhoneNumber = request.Phone;
            user.Address = request.Address;

            // Lưu thay đổi vào cơ sở dữ liệu
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogError($"Error updating user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi khi cập nhật thông tin người dùng.");
            }

            _logger.LogInformation($"User {userId} updated successfully.");
            return Ok("Cập nhật thông tin người dùng thành công.");
        }

        [Authorize]
        [HttpGet("get-user-info")]
        public async Task<IActionResult> GetUserInfo()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized("Người dùng chưa được xác thực.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("Không tìm thấy người dùng.");
            }

            var userInfo = new
            {
                user.UserName,
                user.Email,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.Address
            };

            return Ok(userInfo);
        }
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Kiểm tra sự khớp giữa mật khẩu mới và mật khẩu xác nhận
            if (request.NewPassword != request.ConfirmNewPassword)
            {
                return BadRequest(new { message = "Mật khẩu mới và mật khẩu xác nhận không khớp." });
            }

            // Lấy thông tin UserId từ token JWT
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized("Người dùng chưa được xác thực.");
            }

            // Tìm người dùng dựa trên UserId
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("Không tìm thấy người dùng.");
            }

            // Kiểm tra mật khẩu hiện tại có khớp hay không
            var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(user, request.CurrentPassword);
            if (!isCurrentPasswordValid)
            {
                return BadRequest(new { message = "Mật khẩu hiện tại không chính xác." });
            }

            // Thực hiện đổi mật khẩu
            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok("Mật khẩu đã được thay đổi thành công.");
        }
        [Authorize(Roles = "Admin")]
        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            // Kiểm tra xem người dùng hiện tại có phải là Admin không
            if (!User.IsInRole("Admin"))
            {
                return Forbid("Bạn không có quyền thực hiện thao tác này."); // Trả về 403 Forbidden nếu không phải Admin
            }

            // Tìm kiếm người dùng dựa trên email
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return Conflict("Email này đã được sử dụng.");
            }

            // Tạo người dùng mới
            var user = new ApplicationUser
            {
                UserName = request.Username,
                Email = request.Email,
                PhoneNumber = request.Phone,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Address = request.Address,
                EmailConfirmed = true
            };

            // Tạo người dùng trong Identity
            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            // Thêm vai trò cho người dùng
            if (request.Role == "Admin" || request.Role == "Staff")
            {
                await _userManager.AddToRoleAsync(user, request.Role);

                // Tạo bản ghi tương ứng trong bảng Staff khi vai trò là Staff
                if (request.Role == "Staff")
                {
                    var staff = new Staff
                    {
                        UserId = user.Id,
                        Position = "Hỗ trợ khách hàng", // hoặc giá trị từ yêu cầu
                        HireDate = DateTime.Now // ngày thuê hiện tại
                    };
                    _context.Staff.Add(staff);
                }
                else if (request.Role == "Admin")
                {
                    var admin = new Admin
                    {
                        UserId = user.Id,
                        RoleDescription = "Quản trị viên cấp cao" // hoặc mô tả vai trò phù hợp
                    };
                    _context.Admins.Add(admin);
                }

                await _context.SaveChangesAsync();
            }
            else
            {
                return BadRequest("Vai trò không hợp lệ.");
            }

            return Ok($"Tài khoản {request.Role} đã được tạo thành công.");
        }
        [Authorize(Roles = "Admin")]
        [HttpPost("assign-role")]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
            {
                return NotFound("Không tìm thấy người dùng.");
            }

            var result = await _userManager.AddToRoleAsync(user, request.Role);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok($"Đã gán vai trò {request.Role} cho người dùng thành công.");
        }
        [Authorize(Roles = "Admin")]
        [HttpPut("lock")]
        public async Task<IActionResult> LockUser([FromBody] LockUserRequest request)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
            {
                _logger.LogWarning($"Không tìm thấy người dùng với ID: {request.UserId}");
                return NotFound(new { message = "Không tìm thấy người dùng." });
            }

            // Bật hoặc tắt tính năng Lockout cho người dùng
            user.LockoutEnabled = request.EnableLockout;

            var lockoutEnd = DateTimeOffset.Parse(request.LockoutEnd);
            var setLockoutResult = await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);

            if (!setLockoutResult.Succeeded)
            {
                _logger.LogError($"Lỗi khi khóa tài khoản của {user.UserName}");
                return StatusCode(500, new { message = "Không thể khóa tài khoản." });
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogError($"Lỗi khi cập nhật người dùng: {string.Join(", ", updateResult.Errors.Select(e => e.Description))}");
                return StatusCode(500, new { message = "Lỗi khi cập nhật thông tin tài khoản." });
            }

            _logger.LogInformation($"Tài khoản của {user.UserName} đã bị khóa đến {lockoutEnd}.");
            return Ok(new { message = $"Tài khoản đã bị khóa đến {lockoutEnd}." });
        }
        [Authorize(Roles = "Admin")]
        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                _logger.LogWarning($"Không tìm thấy người dùng với ID: {userId}");
                return NotFound(new { message = "Không tìm thấy người dùng." });
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogError($"Lỗi khi xóa người dùng: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                return StatusCode(500, new { message = "Lỗi khi xóa người dùng." });
            }

            _logger.LogInformation($"Đã xóa người dùng {user.UserName} thành công.");
            return Ok(new { message = "Xóa người dùng thành công." });
        }
        [Authorize]
        [HttpPut("change-email")]
        public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy người dùng." });
            }

            var token = await _userManager.GenerateChangeEmailTokenAsync(user, request.NewEmail);
            var result = await _userManager.ChangeEmailAsync(user, request.NewEmail, token);

            if (!result.Succeeded)
            {
                _logger.LogError($"Lỗi khi thay đổi email cho {user.UserName}");
                return StatusCode(500, new { message = "Không thể thay đổi email." });
            }

            return Ok(new { message = "Email đã được thay đổi thành công." });
        }
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy người dùng." });
            }

            // Đặt lại SecurityStamp để hủy các token hiện tại
            await _userManager.UpdateSecurityStampAsync(user);
            _logger.LogInformation($"Đã đăng xuất người dùng {user.UserName} thành công.");

            return Ok(new { message = "Đăng xuất thành công." });
        }
        [Authorize(Roles = "Admin")]
        [HttpGet("list")]
        public async Task<IActionResult> GetUsersList(
    int page = 1,
    int pageSize = 10,
    string? role = null,
    string? search = null)
        {
            var query = _userManager.Users.AsQueryable();

            // Tìm kiếm theo tên hoặc email nếu có từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.UserName.Contains(search) || u.Email.Contains(search));
            }

            // Lọc theo vai trò nếu có yêu cầu
            if (!string.IsNullOrEmpty(role))
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(role);
                query = usersInRole.AsQueryable();
            }

            // Tính tổng số lượng người dùng để hỗ trợ frontend phân trang
            var totalCount = await query.CountAsync();

            // Lấy danh sách người dùng theo phân trang
            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.PhoneNumber,
                    u.LockoutEnd,
                    u.TwoFactorEnabled,
                    Roles = _userManager.GetRolesAsync(u).Result
                })
                .ToListAsync();

            _logger.LogInformation($"Truy vấn danh sách người dùng, trang {page}, kích thước {pageSize}.");

            return Ok(new
            {
                TotalUsers = totalCount,
                CurrentPage = page,
                PageSize = pageSize,
                Users = users
            });
        }
        [Authorize(Roles = "Admin")]
        [HttpPut("unlock")]
        public async Task<IActionResult> UnlockUser([FromBody] UnlockUserRequest request)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
            {
                _logger.LogWarning($"Không tìm thấy người dùng với ID: {request.UserId}");
                return NotFound(new { message = "Không tìm thấy người dùng." });
            }

            // Kiểm tra xem tài khoản đã bị khóa chưa
            var isLockedOut = await _userManager.IsLockedOutAsync(user);
            if (!isLockedOut)
            {
                return BadRequest(new { message = "Tài khoản này không bị khóa." });
            }

            // Đặt lại trạng thái khóa tài khoản
            var resetLockoutResult = await _userManager.SetLockoutEndDateAsync(user, null);
            if (!resetLockoutResult.Succeeded)
            {
                _logger.LogError($"Lỗi khi mở khóa tài khoản của {user.UserName}");
                return StatusCode(500, new { message = "Không thể mở khóa tài khoản." });
            }

            // Đặt lại số lần đăng nhập thất bại
            var resetFailedCountResult = await _userManager.ResetAccessFailedCountAsync(user);
            if (!resetFailedCountResult.Succeeded)
            {
                _logger.LogError($"Lỗi khi đặt lại số lần đăng nhập thất bại của {user.UserName}");
                return StatusCode(500, new { message = "Không thể đặt lại số lần đăng nhập thất bại." });
            }

            _logger.LogInformation($"Đã mở khóa tài khoản của {user.UserName} thành công.");
            return Ok(new { message = "Tài khoản đã được mở khóa thành công." });
        }

        // Các lớp request khác
        public class RegisterRequest
        {
            [Required(ErrorMessage = "Email là bắt buộc")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Email phải chứa ký tự '@'.")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
            [MinLength(6, ErrorMessage = "Tên đăng nhập phải từ 6 đến 24 ký tự.")]
            [MaxLength(24, ErrorMessage = "Tên đăng nhập phải từ 6 đến 24 ký tự.")]
            [RegularExpression(@"^(?!.*[._])(?!.*\s)[a-zA-Z0-9]{6,24}$", ErrorMessage = "Tên đăng nhập không được có khoảng trắng, ký tự đặc biệt, dấu chấm hoặc dấu gạch dưới.")]
            public string Username { get; set; }

            [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
            [MinLength(8, ErrorMessage = "Vui lòng nhập mật khẩu dài từ 8 đến 32 ký tự.")]
            [MaxLength(32, ErrorMessage = "Vui lòng nhập mật khẩu dài từ 8 đến 32 ký tự.")]
            [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,32}$", ErrorMessage = "Mật khẩu phải có ít nhất một ký tự chữ hoa, một chữ số và một ký tự đặc biệt.")]
            public string Password { get; set; }

            [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
            [Compare("Password", ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp")]
            public string ConfirmPassword { get; set; }

            [Required(ErrorMessage = "Họ là bắt buộc")]
            [MinLength(2, ErrorMessage = "Họ phải từ 2 đến 40 ký tự.")]
            [MaxLength(40, ErrorMessage = "Họ phải từ 2 đến 40 ký tự.")]
            public string LastName { get; set; }

            [Required(ErrorMessage = "Tên là bắt buộc")]
            [MinLength(2, ErrorMessage = "Tên phải từ 2 đến 40 ký tự.")]
            [MaxLength(40, ErrorMessage = "Tên phải từ 2 đến 40 ký tự.")]
            public string FirstName { get; set; }

            [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
            [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Số điện thoại phải bao gồm 10 chữ số từ 0 đến 9.")]
            public string Phone { get; set; }

            public string Address { get; set; } // Địa chỉ khách hàng
        }

        public class ResendOtpRequest
        {
            [Required(ErrorMessage = "Vui lòng cung cấp email.")]
            [EmailAddress(ErrorMessage = "Email không khớp")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Mục đích OTP là bắt buộc")]
            public string OtpPurpose { get; set; } // "register" hoặc "forgot-password"
        }

        public class OtpVerifyRequest
        {
            [Required(ErrorMessage = "Vui lòng cung cấp email.")]
            [EmailAddress(ErrorMessage = "Email không khớp")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Mã OTP là bắt buộc")]
            public string Otp { get; set; }

            [Required(ErrorMessage = "Mục đích OTP là bắt buộc")]
            public string OtpPurpose { get; set; } // "register" hoặc "forgot-password"
        }

        public class ForgotPasswordRequest
        {
            [Required(ErrorMessage = "Vui lòng cung cấp email.")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ. Vui lòng nhập một địa chỉ email đúng định dạng.")]
            public string Email { get; set; }
        }

        public class ResetPasswordRequest
        {
            [Required(ErrorMessage = "Vui lòng cung cấp email.")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ. Vui lòng nhập một địa chỉ email đúng định dạng.")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
            [MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự.")]
            [MaxLength(32, ErrorMessage = "Mật khẩu không được vượt quá 32 ký tự.")]
            [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,32}$",
                ErrorMessage = "Mật khẩu phải có ít nhất một chữ cái viết hoa, một chữ số và một ký tự đặc biệt.")]
            public string NewPassword { get; set; }

            [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới.")]
            [Compare("NewPassword", ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp.")]
            public string ConfirmPassword { get; set; }
        }

        public class LoginRequest
        {
            public string EmailOrUsername { get; set; }  // Cho phép nhập vào cả email hoặc username
            public string Password { get; set; }
        }
        public class UpdateUserInfoRequest
        {
            [Required(ErrorMessage = "Vui lòng cung cấp họ.")]
            [StringLength(40, MinimumLength = 2, ErrorMessage = "Họ phải từ 2 đến 40 ký tự.")]
            public string LastName { get; set; }

            [Required(ErrorMessage = "Vui lòng cung cấp tên.")]
            [StringLength(40, MinimumLength = 2, ErrorMessage = "Tên phải từ 2 đến 40 ký tự.")]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "Vui lòng cung cấp số điện thoại.")]
            [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Số điện thoại không hợp lệ. Số điện thoại phải bao gồm 10 chữ số.")]
            public string Phone { get; set; }

            [StringLength(255, ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự.")]
            public string Address { get; set; } // Địa chỉ người dùng
        }
        public class ChangePasswordRequest
        {
            [Required(ErrorMessage = "Vui lòng cung cấp mật khẩu hiện tại.")]
            public string CurrentPassword { get; set; }

            [Required(ErrorMessage = "Vui lòng cung cấp mật khẩu mới.")]
            [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,32}$", ErrorMessage = "Mật khẩu mới phải từ 8-32 ký tự, bao gồm ít nhất một ký tự in hoa, một chữ số và một ký tự đặc biệt.")]
            public string NewPassword { get; set; }

            [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới.")]
            [Compare("NewPassword", ErrorMessage = "Mật khẩu mới và xác nhận mật khẩu không khớp.")]
            public string ConfirmNewPassword { get; set; }
        }
        // Model cho API tạo người dùng mới
        public class CreateUserRequest
        {
            [Required(ErrorMessage = "Vui lòng cung cấp email.")]
            [EmailAddress(ErrorMessage = "Email phải hợp lệ, bao gồm '@' và tên miền.")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Tên đăng nhập là bắt buộc.")]
            [MinLength(6, ErrorMessage = "Tên đăng nhập phải có từ 6 đến 24 ký tự.")]
            [MaxLength(24, ErrorMessage = "Tên đăng nhập phải có từ 6 đến 24 ký tự.")]
            [RegularExpression("^[a-zA-Z0-9]{6,24}$", ErrorMessage = "Tên đăng nhập không được có khoảng trắng, ký tự đặc biệt, dấu chấm, hoặc dấu gạch dưới.")]
            public string Username { get; set; }

            [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
            [StringLength(32, MinimumLength = 8, ErrorMessage = "Vui lòng nhập mật khẩu dài 8-32 ký tự.")]
            [RegularExpression("^(?=.*[A-Z])(?=.*[a-z])(?=.*[0-9])(?=.*[@$!%*?&])[A-Za-z0-9@$!%*?&]{8,32}$", ErrorMessage = "Mật khẩu phải có ít nhất một ký tự hoa, một chữ số, và một ký tự đặc biệt.")]
            public string Password { get; set; }

            [Required(ErrorMessage = "Họ là bắt buộc.")]
            [StringLength(40, MinimumLength = 2, ErrorMessage = "Họ phải có từ 2 đến 40 ký tự.")]
            public string LastName { get; set; }

            [Required(ErrorMessage = "Tên là bắt buộc.")]
            [StringLength(40, MinimumLength = 2, ErrorMessage = "Tên phải có từ 2 đến 40 ký tự.")]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
            [RegularExpression("^[0-9]{10}$", ErrorMessage = "Số điện thoại phải gồm 10 chữ số từ 0 đến 9.")]
            public string Phone { get; set; }

            public string Address { get; set; }

            [Required(ErrorMessage = "Vai trò là bắt buộc.")]
            [RegularExpression("^(Admin|Staff)$", ErrorMessage = "Vai trò chỉ có thể là 'Admin' hoặc 'Staff'.")]
            public string Role { get; set; }
        }
        public class AssignRoleRequest
        {
            [Required] public int UserId { get; set; }
            [Required] public string Role { get; set; }
        }
        public class LockUserRequest
        {
            [Required] public int UserId { get; set; }
            [Required] public string LockoutEnd { get; set; }
            public bool EnableLockout { get; set; } = true;  // Giá trị mặc định là bật Lockout
        }
        public class ChangeEmailRequest
        {
            [Required(ErrorMessage = "Vui lòng cung cấp email mới.")]
            [EmailAddress(ErrorMessage = "Email phải hợp lệ, bao gồm '@' và tên miền.")]
            public string NewEmail { get; set; }
        }
        public class UnlockUserRequest
        {
            [Required] public int UserId { get; set; }
        }
    }
}