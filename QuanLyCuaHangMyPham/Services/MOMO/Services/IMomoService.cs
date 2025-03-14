using QuanLyCuaHangMyPham.Services.MOMO.Models.Momo;
using QuanLyCuaHangMyPham.Services.MOMO.Models.Order;
using Microsoft.AspNetCore.Http;

namespace QuanLyCuaHangMyPham.Services.MOMO.Services
{
    public interface IMomoService
    {
        Task<MomoCreatePaymentResponseModel> CreatePaymentAsync(OrderInfoModel model);
        MomoExecuteResponseModel PaymentExecuteAsync(IQueryCollection collection);
    }
}
