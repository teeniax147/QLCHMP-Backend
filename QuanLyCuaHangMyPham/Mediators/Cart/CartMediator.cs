using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyCuaHangMyPham.Repositories.Cart;

namespace QuanLyCuaHangMyPham.Mediators.Cart
{
    public class CartMediator : IMediator
    {
        private readonly List<IColleague> _colleagues = new List<IColleague>();
        private readonly CartRepository _cartRepository;

        public CartMediator(CartRepository cartRepository)
        {
            _cartRepository = cartRepository;
        }

        public void RegisterColleague(IColleague colleague)
        {
            if (!_colleagues.Contains(colleague))
            {
                _colleagues.Add(colleague);
                colleague.Mediator = this;
            }
        }

        public async Task NotifyCartChanged(int userId, string action, int? productId = null, int? quantity = null)
        {
            foreach (var colleague in _colleagues)
            {
                await colleague.ReceiveCartNotification(userId, action, productId, quantity);
            }
        }

        public async Task<object> GetCartDetails(int userId)
        {
            if (userId > 0)
            {
                var (success, details) = await _cartRepository.GetCartDetails(userId);
                return details;
            }
            else
            {
                var (success, details) = await _cartRepository.GetGuestCartDetails();
                return details;
            }
        }

        public async Task<object> PreviewOrder(
            int userId,
            string couponCode,
            int? shippingCompanyId,
            int? paymentMethodId = null,
            string shippingAddress = null,
            string phoneNumber = null,
            string email = null)
        {
            if (userId > 0)
            {
                var (success, preview) = await _cartRepository.PreviewOrder(
                    userId,
                    couponCode,
                    shippingCompanyId,
                    paymentMethodId,
                    shippingAddress,
                    phoneNumber,
                    email);

                return preview;
            }
            else
            {
                var (success, preview) = await _cartRepository.PreviewGuestOrder(
                    couponCode,
                    shippingCompanyId,
                    paymentMethodId,
                    shippingAddress,
                    phoneNumber,
                    email);

                return preview;
            }
        }
    }
}
