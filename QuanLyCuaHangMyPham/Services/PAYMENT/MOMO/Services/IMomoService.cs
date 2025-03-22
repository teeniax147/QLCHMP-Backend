using Microsoft.AspNetCore.Http;
using QuanLyCuaHangMyPham.Services.PAYMENT.MOMO.Models.Momo;
using QuanLyCuaHangMyPham.Services.PAYMENT.MOMO.Models.Order;

namespace QuanLyCuaHangMyPham.Services.PAYMENT.MOMO.Services
{
    public interface IMomoService
    {
        Task<MomoCreatePaymentResponseModel> CreatePaymentAsync(OrderInfoModel model);
        MomoExecuteResponseModel PaymentExecuteAsync(IQueryCollection collection);
    }
}
