// RemoveFromCartCommand.cs
using QuanLyCuaHangMyPham.Repositories;
using QuanLyCuaHangMyPham.Repositories.Cart;
using System;
using System.Threading.Tasks;

namespace QuanLyCuaHangMyPham.Commands.Cart
{
    public class RemoveFromCartCommand : ICartCommand
    {
        private readonly CartRepository _repository;
        private readonly int _userId;
        private readonly int _productId;
        private readonly int _quantity;

        public RemoveFromCartCommand(
            CartRepository repository,
            int userId,
            int productId,
            int quantity)
        {
            _repository = repository;
            _userId = userId;
            _productId = productId;
            _quantity = quantity;
        }

        public async Task<CartCommandResult> ExecuteAsync()
        {
            try
            {
                bool success = await _repository.RemoveFromCart(_userId, _productId, _quantity);

                if (success)
                {
                    return CartCommandResult.SuccessResult("Cập nhật giỏ hàng thành công.");
                }
                else
                {
                    return CartCommandResult.FailResult("Sản phẩm không tồn tại trong giỏ hàng.");
                }
            }
            catch (Exception ex)
            {
                return CartCommandResult.FailResult($"Lỗi khi cập nhật giỏ hàng: {ex.Message}");
            }
        }
    }
}
