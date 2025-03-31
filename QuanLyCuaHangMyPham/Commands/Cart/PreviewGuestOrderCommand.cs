// PreviewGuestOrderCommand.cs
using QuanLyCuaHangMyPham.Repositories;
using QuanLyCuaHangMyPham.Repositories.Cart;
using System;
using System.Threading.Tasks;

namespace QuanLyCuaHangMyPham.Commands.Cart
{
    public class PreviewGuestOrderCommand : ICartCommand
    {
        private readonly CartRepository _repository;
        private readonly string _couponCode;
        private readonly int? _shippingCompanyId;
        private readonly int? _paymentMethodId;
        private readonly string _shippingAddress;
        private readonly string _phoneNumber;
        private readonly string _email;

        public PreviewGuestOrderCommand(
            CartRepository repository,
            string couponCode,
            int? shippingCompanyId,
            int? paymentMethodId,
            string shippingAddress,
            string phoneNumber,
            string email)
        {
            _repository = repository;
            _couponCode = couponCode;
            _shippingCompanyId = shippingCompanyId;
            _paymentMethodId = paymentMethodId;
            _shippingAddress = shippingAddress;
            _phoneNumber = phoneNumber;
            _email = email;
        }

        public async Task<CartCommandResult> ExecuteAsync()
        {
            try
            {
                var (success, previewData) = await _repository.PreviewGuestOrder(
                    _couponCode,
                    _shippingCompanyId,
                    _paymentMethodId,
                    _shippingAddress,
                    _phoneNumber,
                    _email);

                if (success)
                {
                    return CartCommandResult.SuccessResult("Xem trước đơn hàng thành công", previewData);
                }
                else
                {
                    return CartCommandResult.FailResult(previewData?.ToString() ?? "Lỗi khi xem trước đơn hàng");
                }
            }
            catch (Exception ex)
            {
                return CartCommandResult.FailResult($"Lỗi khi xem trước đơn hàng: {ex.Message}");
            }
        }
    }
}
