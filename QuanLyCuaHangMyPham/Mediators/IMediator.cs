using System.Threading.Tasks;

namespace QuanLyCuaHangMyPham.Mediators
{
    public interface IMediator
    {
        // Phương thức để thông báo thay đổi giỏ hàng
        Task NotifyCartChanged(int userId, string action, int? productId = null, int? quantity = null);

        // Phương thức để xem trước đơn hàng
        Task<object> PreviewOrder(
            int userId,
            string couponCode,
            int? shippingCompanyId,
            int? paymentMethodId = null,
            string shippingAddress = null,
            string phoneNumber = null,
            string email = null);

        // Phương thức để lấy chi tiết giỏ hàng
        Task<object> GetCartDetails(int userId);

        // Phương thức để đăng ký colleague
        void RegisterColleague(IColleague colleague);
    }
}