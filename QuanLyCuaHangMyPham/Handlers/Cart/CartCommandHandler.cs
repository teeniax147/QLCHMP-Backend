using System.Threading.Tasks;
using QuanLyCuaHangMyPham.Commands.Cart;
using QuanLyCuaHangMyPham.Mediators;

namespace QuanLyCuaHangMyPham.Handlers.Cart
{
    public class CartCommandHandler
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CartCommandHandler> _logger;

        public CartCommandHandler(
            IMediator mediator,
            ILogger<CartCommandHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<CartCommandResult> HandleAsync(
            ICartCommand command,
            int userId,
            string action,
            int? productId = null,
            int? quantity = null)
        {
            _logger.LogInformation($"Xử lý lệnh {action} cho userId: {userId}");

            // Thực thi command
            var result = await command.ExecuteAsync();

            // Nếu thành công, thông báo thông qua mediator
            if (result.Success)
            {
                _logger.LogInformation($"Lệnh {action} thành công cho userId: {userId}");
                await _mediator.NotifyCartChanged(userId, action, productId, quantity);
            }
            else
            {
                _logger.LogWarning($"Lệnh {action} thất bại cho userId: {userId}: {result.Message}");
            }

            return result;
        }
    }
}
