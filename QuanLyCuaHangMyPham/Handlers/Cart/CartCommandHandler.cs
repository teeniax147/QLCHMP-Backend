using System.Threading.Tasks;
using QuanLyCuaHangMyPham.Commands.Cart;
using QuanLyCuaHangMyPham.Mediators;

namespace QuanLyCuaHangMyPham.Handlers.Cart
{
    public class CartCommandHandler
    {
        private readonly IMediator _mediator;

        public CartCommandHandler(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<CartCommandResult> HandleAsync(
            ICartCommand command,
            int userId,
            string action,
            int? productId = null,
            int? quantity = null)
        {
            // Thực thi command
            var result = await command.ExecuteAsync();

            // Nếu thành công, thông báo thông qua mediator
            if (result.Success)
            {
                await _mediator.NotifyCartChanged(userId, action, productId, quantity);
            }

            return result;
        }
    }
}
