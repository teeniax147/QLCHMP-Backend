
using Microsoft.AspNetCore.Mvc;
using QuanLyCuaHangMyPham;
using QuanLyCuaHangMyPham.Services.PAYMENT.MOMO.Models;
using QuanLyCuaHangMyPham.Services.PAYMENT.MOMO.Models.Momo;
using QuanLyCuaHangMyPham.Services.PAYMENT.MOMO.Models.Order;
using QuanLyCuaHangMyPham.Services.PAYMENT.MOMO.Services;
using System.Threading.Tasks;

namespace QuanLyCuaHangMyPham.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MomoController : ControllerBase
    {
        private readonly IMomoService _momoService;

        public MomoController(IMomoService momoService)
        {
            _momoService = momoService;
        }

        // DTO class defined inside the controller, without OrderId
        public class OrderInfoModelDto
        {
            public string FullName { get; set; }
            public string OrderInfo { get; set; }
            public double Amount { get; set; }
        }

        // Tạo payment URL và trả về URL
        [HttpPost("CreatePaymentUrl")]
        public async Task<IActionResult> CreatePaymentUrl([FromBody] OrderInfoModelDto model)
        {
            try
            {
                if (model == null || string.IsNullOrEmpty(model.FullName) || string.IsNullOrEmpty(model.OrderInfo) || model.Amount <= 0)
                {
                    return BadRequest("Dữ liệu đầu vào không hợp lệ.");
                }

                // Tạo OrderId tự động
                var orderId = DateTime.UtcNow.Ticks.ToString();

                // Chuyển DTO sang model gốc để sử dụng với dịch vụ
                var orderInfoModel = new OrderInfoModel
                {
                    FullName = model.FullName,
                    OrderId = orderId, // Đặt OrderId tự động
                    OrderInfo = model.OrderInfo,
                    Amount = model.Amount
                };

                var response = await _momoService.CreatePaymentAsync(orderInfoModel);

                // Trả về kết quả dưới dạng JSON với URL thanh toán
                return Ok(new { PayUrl = response.PayUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã có lỗi xảy ra", details = ex.Message });
            }
        }

        // Xử lý callback từ Momo
        [HttpGet("PaymentCallBack")]
        public IActionResult PaymentCallBack()
        {
            try
            {
                var amount = HttpContext.Request.Query["amount"];
                var orderInfo = HttpContext.Request.Query["orderInfo"];
                var orderId = HttpContext.Request.Query["orderId"];

                if (string.IsNullOrEmpty(amount) || string.IsNullOrEmpty(orderInfo) || string.IsNullOrEmpty(orderId))
                {
                    return BadRequest("Dữ liệu trả về từ Momo không hợp lệ.");
                }

                // Xử lý thông tin từ callback trả về
                var response = _momoService.PaymentExecuteAsync(HttpContext.Request.Query);

                // Trả về dữ liệu trả về dưới dạng JSON
                return Ok(new
                {
                    OrderId = response.OrderId,
                    Amount = response.Amount,
                    OrderInfo = response.OrderInfo
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã có lỗi xảy ra", details = ex.Message });
            }
        }
    }
}
