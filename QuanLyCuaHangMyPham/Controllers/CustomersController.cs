using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyCuaHangMyPham.Data;
using QuanLyCuaHangMyPham.Models;
using QuanLyCuaHangMyPham.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Identity.Data;
using Org.BouncyCastle.Crypto.Generators;
using System.ComponentModel.DataAnnotations;
namespace QuanLyCuaHangMyPham.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly QuanLyCuaHangMyPhamContext _context;
        private readonly ILogger<CustomersController> _logger;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;

        public CustomersController(QuanLyCuaHangMyPhamContext context, ILogger<CustomersController> logger,
            IEmailService emailService, IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _cache = cache;
        }

        // GET: api/Customers
        // Hỗ trợ phân trang và tìm kiếm
        [HttpGet("{Phan-trang}")]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers(string searchTerm = "", int page = 1, int pageSize = 10)
        {
            // Giới hạn số lượng phần tử tối đa trả về trong một lần gọi API
            const int maxPageSize = 100; // Ví dụ: Giới hạn tối đa là 100 phần tử
            pageSize = Math.Min(pageSize, maxPageSize); // Đảm bảo pageSize không vượt quá maxPageSize

            try
            {
                // Khởi tạo truy vấn cơ sở dữ liệu
                var customersQuery = _context.Customers.AsQueryable();

                // Nếu có từ khóa tìm kiếm, thêm điều kiện vào truy vấn
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    customersQuery = customersQuery.Where(c => c.FirstName.Contains(searchTerm) || c.Email.Contains(searchTerm));
                }

                // Phân trang dữ liệu
                var customers = await customersQuery
                                         .Skip((page - 1) * pageSize) // Bỏ qua các phần tử của trang trước
                                         .Take(pageSize) // Giới hạn số lượng phần tử trên mỗi trang
                                         .ToListAsync();

                return Ok(customers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách khách hàng.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi hệ thống, vui lòng thử lại sau.");
            }
        }

        // GET: api/Customers/5
        [HttpGet("{tim-theo-id}")]
        public async Task<ActionResult<Customer>> GetCustomer(int id)
        {
            try
            {
                var customer = await _context.Customers.FindAsync(id);

                if (customer == null)
                {
                    return NotFound("Không tìm thấy khách hàng.");
                }
                if (customer.IsSuspended)
                {
                    return BadRequest("Tài khoản của khách hàng đã bị khóa.");
                }
                return Ok(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy khách hàng với ID {id}.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi hệ thống, vui lòng thử lại sau.");
            }
        }
        
        // POST: api/Customers
        [HttpPost("{them-khach-hang}")]
        public async Task<ActionResult<Customer>> PostCustomer(Customer customer)
        {
            try
            {
                _logger.LogInformation($"Thêm mới khách hàng: {customer.Email}");
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                // Kiểm tra username đã tồn tại
                var existingUsername = await _context.Customers.FirstOrDefaultAsync(c => c.Username == customer.Username);
                if (existingUsername != null)
                {
                    return Conflict("Tên đăng nhập này đã tồn tại. Vui lòng chọn tên đăng nhập khác.");
                }
                // Kiểm tra trùng lặp email
                var existingCustomer = await _context.Customers
                                                     .FirstOrDefaultAsync(c => c.Email == customer.Email);
                if (existingCustomer != null)
                {
                    return Conflict("Địa chỉ email này đã tồn tại. Vui lòng sử dụng email khác.");
                }
                // Mã hóa mật khẩu trước khi lưu
                customer.Password = BCrypt.Net.BCrypt.HashPassword(customer.Password);
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }


                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Khách hàng {customer.Email} đã được thêm thành công.");
                return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm khách hàng.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi hệ thống, vui lòng thử lại sau.");
            }
        }
        // Sử dụng EmailService để gửi email
        [HttpPost("gui-email")]
        public async Task<IActionResult> SendEmail([FromBody] EmailRequest request)
        {
            if (string.IsNullOrEmpty(request.Email))
            {
                return BadRequest("Email is required.");
            }

            // Sử dụng phương thức GenerateOTP từ EmailService
            var otp = _emailService.GenerateOTP();

            // Lưu mã OTP vào cache với thời gian hết hạn là 5 phút
            _cache.Set(request.Email, otp, TimeSpan.FromMinutes(5));

            var subject = "Xác nhận tài khoản";
            var message = $"Glamer Cosmic xin chào! \n " +
                $"Vui lòng sử dụng mã OTP sau để xác thực tài khoản của bạn: {otp} \n" +
                $"Lưu ý: Mã xác thực chỉ có hiệu lực trong vòng 5 phút \n " +
                $"Chúc một ngày tốt lành...";

            try
            {
                await _emailService.SendEmailAsync(request.Email, subject, message);
                return Ok("Email đã được gửi.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi khi gửi email: {ex.Message}");
            }
        }

        [HttpPost("verify-otp")]
        public IActionResult VerifyOTP([FromBody] OtpRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Otp))
            {
                return BadRequest("Email và mã OTP là bắt buộc.");
            }

            // Lấy OTP từ cache
            if (_cache.TryGetValue(request.Email + "_otp", out string storedOtp))
            {
                // So sánh OTP
                if (request.Otp == storedOtp)
                {
                    // Xóa OTP sau khi xác thực thành công
                    _cache.Remove(request.Email + "_otp");

                    // Lấy thông tin khách hàng tạm thời từ cache
                    if (_cache.TryGetValue(request.Email + "_data", out RegisterRequest cachedRequest))
                    {
                        // Tạo người dùng mới và lưu vào database
                        var customer = new Customer
                        {
                            FirstName = cachedRequest.FirstName,
                            LastName = cachedRequest.LastName,
                            Email = cachedRequest.Email,
                            Username = cachedRequest.Username,
                            Password = BCrypt.Net.BCrypt.HashPassword(cachedRequest.Password), // Mã hóa mật khẩu
                            Phone = cachedRequest.Phone,
                            Address = cachedRequest.Address,
                            IsVerified = true,
                            MembershipLevelId = 1
                        };

                        _context.Customers.Add(customer);
                        _context.SaveChanges();

                        // Xóa thông tin khách hàng tạm thời
                        _cache.Remove(request.Email + "_data");

                        return Ok("Xác thực thành công. Tài khoản của bạn đã được tạo.");
                    }
                    else
                    {
                        return BadRequest("Không tìm thấy thông tin khách hàng trong cache.");
                    }
                }
                else
                {
                    return BadRequest("Mã OTP không chính xác.");
                }
            }
            else
            {
                return BadRequest("Mã OTP đã hết hạn hoặc không tồn tại.");
            }
        }

        [HttpPost("dang-ky")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Email, Tên đăng nhập và Mật khẩu là bắt buộc.");
            }

            // Kiểm tra trùng lặp email hoặc tên đăng nhập
            var existingCustomer = _context.Customers.FirstOrDefault(c => c.Email == request.Email || c.Username == request.Username);
            if (existingCustomer != null)
            {
                return Conflict("Email hoặc Tên đăng nhập đã tồn tại.");
            }

            // Tạo mã OTP
            var otp = _emailService.GenerateOTP();

            // Lưu OTP riêng
            _cache.Set(request.Email + "_otp", otp, TimeSpan.FromMinutes(5));

            // Lưu thông tin tạm thời của khách hàng riêng
            _cache.Set(request.Email + "_data", new RegisterRequest
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Username = request.Username,
                Password = request.Password, // Lưu mật khẩu dưới dạng plain text tạm thời
                Phone = request.Phone,
                Address = request.Address
            }, TimeSpan.FromMinutes(5));

            // Gửi email OTP
            var subject = "Xác thực đăng ký tài khoản của bạn";
            var message = $"Glamer Cosmic xin chào! \n " +
                $"Vui lòng sử dụng mã OTP sau để đăng ký tài khoản của bạn: {otp} \n" +
                $"Lưu ý: Mã xác thực chỉ có hiệu lực trong vòng 5 phút \n " +
                $"Chúc một ngày tốt lành...";
            _emailService.SendEmailAsync(request.Email, subject, message);

            return Ok("Vui lòng kiểm tra email của bạn để xác thực đăng ký.");
        }
        [HttpPost("dang-nhap")]
        public async Task<IActionResult> LoginCustomer([FromBody] LoginRequest request)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == request.Email);
            if (customer == null)
            {
                return Unauthorized("Email  không đúng.");
            }

            // Kiểm tra mật khẩu
            if (!BCrypt.Net.BCrypt.Verify(request.Password, customer.Password))
            {
                return Unauthorized("mật khẩu không đúng.");
            }

            return Ok("Đăng nhập thành công.");
        }
        [HttpPost("quen-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] EmailRequest request)
        {
            if (string.IsNullOrEmpty(request.Email))
            {
                return BadRequest("Email is required.");
            }

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == request.Email);
            if (customer == null)
            {
                return NotFound("Không tìm thấy khách hàng với email này.");
            }

            // Tạo mã OTP
            var otp = _emailService.GenerateOTP();

            // Lưu mã OTP vào cache với thời gian hết hạn là 5 phút
            _cache.Set(request.Email, otp, TimeSpan.FromMinutes(5));

            var subject = "Đặt lại mật khẩu";
            var message = $"Glamer Cosmic xin chào! \n " +
                $"Vui lòng sử dụng mã OTP sau để đặt lại mật khẩu của bạn: {otp} \n" +
                $"Lưu ý: Mã xác thực chỉ có hiệu lực trong vòng 5 phút \n " +
                $"Chúc một ngày tốt lành...";

            try
            {
                await _emailService.SendEmailAsync(request.Email, subject, message);
                return Ok("Email xác nhận đã được gửi.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi khi gửi email: {ex.Message}");
            }
        }
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Otp) || string.IsNullOrEmpty(request.NewPassword))
            {
                return BadRequest("Email, OTP và mật khẩu mới là bắt buộc.");
            }

            // Kiểm tra mã OTP có trong cache không
            if (_cache.TryGetValue(request.Email, out string storedOtp))
            {
                if (request.Otp != storedOtp)
                {
                    return BadRequest("Mã OTP không chính xác.");
                }

                // Xóa mã OTP khỏi cache sau khi xác thực thành công
                _cache.Remove(request.Email);

                // Tìm người dùng theo email
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == request.Email);
                if (customer == null)
                {
                    return NotFound("Không tìm thấy khách hàng với email này.");
                }

                // Cập nhật mật khẩu mới
                customer.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword); // Sử dụng BCrypt để mã hóa mật khẩu
                _context.Customers.Update(customer);
                await _context.SaveChangesAsync();

                return Ok("Mật khẩu đã được đặt lại thành công.");
            }
            else
            {
                return BadRequest("Mã OTP đã hết hạn hoặc không tồn tại.");
            }
        }
        // PUT: api/Customers/5
        [HttpPut("{cap-nhat-khach-hang-theo-id}")]
        public async Task<IActionResult> PutCustomer(int id, Customer customer)
        {
            if (id != customer.Id)
            {
                return BadRequest("ID khách hàng không khớp.");
            }

            _context.Entry(customer).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                var existingCustomer = await _context.Customers.FindAsync(id);
                if (existingCustomer == null)
                {
                    return NotFound("Không tìm thấy khách hàng.");
                }

                return Conflict("Có xung đột dữ liệu. Khách hàng đã được cập nhật bởi người dùng khác. Vui lòng thử lại.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi cập nhật khách hàng với ID {id}.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi hệ thống, vui lòng thử lại sau.");
            }

            return NoContent();
        }



        // DELETE: api/Customers/5
        [HttpDelete("{xoa-khach-hang-theo-id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            try
            {
                var customer = await _context.Customers.Include(c => c.Orders).FirstOrDefaultAsync(c => c.Id == id);
                if (customer == null)
                {
                    return NotFound("Không tìm thấy khách hàng.");
                }

                // Kiểm tra xem khách hàng có đơn hàng hay không, không cho phép xóa nếu có
                if (customer.Orders.Any())
                {
                    return BadRequest("Không thể xóa khách hàng vì khách hàng có đơn hàng liên kết.");
                }

                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi xóa khách hàng với ID {id}.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi hệ thống, vui lòng thử lại sau.");
            }
        }
        public class RegisterRequest
        {
            [Required(ErrorMessage = "Email là bắt buộc")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
            public string Username { get; set; }

            [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
            [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
            public string Password { get; set; }

            [Required(ErrorMessage = "Họ là bắt buộc")]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "Tên là bắt buộc")]
            public string LastName { get; set; }

            [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
            public string Phone { get; set; }

            public string Address { get; set; }
        }
        public class LoginRequest
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }
        // Model để nhận email từ yêu cầu gửi POST
        public class EmailRequest
        {
            public string Email { get; set; }
        }
        public class OtpRequest
        {
            [Required]
            public string Email { get; set; }

            [Required]
            public string Otp { get; set; }
        }
        public class ResetPasswordRequest
        {
            public string Email { get; set; }
            public string Otp { get; set; }
            public string NewPassword { get; set; }
        }
        // Kiểm tra khách hàng tồn tại
        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.Id == id);
        }
    }
}
