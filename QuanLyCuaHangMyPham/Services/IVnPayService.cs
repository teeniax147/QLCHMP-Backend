using QuanLyCuaHangMyPham.Models;

namespace QuanLyCuaHangMyPham.Services;
public interface IVnPayService
{
    string CreatePaymentUrl(PaymentInformationModel model, HttpContext context);
    PaymentResponseModel PaymentExecute(IQueryCollection collections);
}