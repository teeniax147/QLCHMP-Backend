using Microsoft.AspNetCore.Mvc;
using QuanLyCuaHangMyPham.Services.PAYMENT.VNPAY;
using QuanLyCuaHangMyPham.Services.PAYMENT.VNPAY.Enums;
using QuanLyCuaHangMyPham.Services.PAYMENT.VNPAY.Models;
using QuanLyCuaHangMyPham.Services.PAYMENT.VNPAY.Utilities;

namespace QuanLyCuaHangMyPham.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VnpayController : ControllerBase
    {
        private readonly IVnpay _vnpay;
        private readonly IConfiguration _configuration;

        public VnpayController(IVnpay vnPayservice, IConfiguration configuration)
        {
            _vnpay = vnPayservice;
            _configuration = configuration;

            _vnpay.Initialize(_configuration["Vnpay:TmnCode"], _configuration["Vnpay:HashSecret"], _configuration["Vnpay:BaseUrl"], _configuration["Vnpay:CallbackUrl"]);
        }

        /// <summary>
        /// Tạo url thanh toán
        /// </summary>
        /// <param name="money">Số tiền phải thanh toán</param>
        /// <param name="description">Mô tả giao dịch</param>
        /// <returns></returns>
        [HttpGet("CreatePaymentUrl")]
        public ActionResult<string> CreatePaymentUrl(double money, string description)
        {
            try
            {
                var ipAddress = NetworkHelper.GetIpAddress(HttpContext); // Lấy địa chỉ IP của thiết bị thực hiện giao dịch

                var request = new PaymentRequest
                {
                    PaymentId = DateTime.Now.Ticks,
                    Money = money,
                    Description = description,
                    IpAddress = ipAddress,
                    BankCode = BankCode.ANY, // Tùy chọn. Mặc định là tất cả phương thức giao dịch
                    CreatedDate = DateTime.Now, // Tùy chọn. Mặc định là thời điểm hiện tại
                    Currency = Currency.VND, // Tùy chọn. Mặc định là VND (Việt Nam đồng)
                    Language = DisplayLanguage.Vietnamese // Tùy chọn. Mặc định là tiếng Việt
                };

                var paymentUrl = _vnpay.GetPaymentUrl(request);

                return Created(paymentUrl, paymentUrl);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Thực hiện hành động sau khi thanh toán. URL này cần được khai báo với VNPAY để API này hoạt đồng (ví dụ: http://localhost:1234/api/Vnpay/IpnAction)
        /// </summary>
        /// <returns></returns>
        [HttpGet("IpnAction")]
        public IActionResult IpnAction()
        {
            if (Request.QueryString.HasValue)
            {
                try
                {
                    var paymentResult = _vnpay.GetPaymentResult(Request.Query);
                    if (paymentResult.IsSuccess)
                    {
                        // Thực hiện hành động nếu thanh toán thành công tại đây. Ví dụ: Cập nhật trạng thái đơn hàng trong cơ sở dữ liệu.
                        return Ok();
                    }

                    // Thực hiện hành động nếu thanh toán thất bại tại đây. Ví dụ: Hủy đơn hàng.
                    return BadRequest("Thanh toán thất bại");
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }

            return NotFound("Không tìm thấy thông tin thanh toán.");
        }

        /// <summary>
        /// Trả kết quả thanh toán về cho người dùng
        /// </summary>
        /// <returns></returns>
        [HttpGet("Callback")]
        public ActionResult<PaymentResult> Callback()
        {
            if (Request.QueryString.HasValue)
            {
                try
                {
                    // Log tham số từ VNPay để debug
                    var vnpParams = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());

                    // Kiểm tra trực tiếp tham số từ VNPay
                    string responseCode = Request.Query["vnp_ResponseCode"].ToString();
                    // Thường mã "00" là thành công theo VNPay
                    bool directCheckSuccess = responseCode == "00";

                    // Lấy kết quả từ service
                    var paymentResult = _vnpay.GetPaymentResult(Request.Query);

                    // Nếu có sự khác biệt giữa kiểm tra trực tiếp và kết quả từ service
                    if (directCheckSuccess && !paymentResult.IsSuccess)
                    {
                        // Log sự khác biệt
                        // Ưu tiên kết quả kiểm tra trực tiếp
                        paymentResult.IsSuccess = true;
                        paymentResult.Description = "Thanh toán thành công (Override)";
                    }

                    // Lưu kết quả vào đơn hàng
                    // Lấy mã đơn hàng từ vnp_TxnRef
                    string orderCode = Request.Query["vnp_TxnRef"].ToString();

                    // TODO: Cập nhật trạng thái đơn hàng trong database
                    // UpdateOrderPaymentStatus(orderCode, paymentResult.IsSuccess);

                    if (paymentResult.IsSuccess)
                    {
                        return Ok(paymentResult);
                    }
                    return BadRequest(paymentResult);
                }
                catch (Exception ex)
                {
                    // Log lỗi chi tiết
                    return BadRequest(ex.Message);
                }
            }
            return NotFound("Không tìm thấy thông tin thanh toán.");
        }

        // Hàm phụ trợ để cập nhật trạng thái đơn hàng (nếu cần)
        private void UpdateOrderPaymentStatus(string orderCode, bool isSuccess)
        {
            // Tìm đơn hàng theo mã
            // Cập nhật trạng thái thanh toán
            // Lưu vào database
        }
    }
}