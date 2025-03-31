// RemoveItemCommand.cs
using QuanLyCuaHangMyPham.Repositories;
using QuanLyCuaHangMyPham.Repositories.Cart;
using System;
using System.Threading.Tasks;

namespace QuanLyCuaHangMyPham.Commands.Cart
{
    public class RemoveItemCommand : ICartCommand
    {
        private readonly CartRepository _repository;
        private readonly int _userId;
        private readonly int _productId;

        public RemoveItemCommand(
            CartRepository repository,
            int userId,
            int productId)
        {
            _repository = repository;
            _userId = userId;
            _productId = productId;
        }

        public async Task<CartCommandResult> ExecuteAsync()
        {
            try
            {
                bool success = await _repository.RemoveItem(_userId, _productId);

                if (success)
                {
                    return CartCommandResult.SuccessResult("Sản phẩm đã được xóa khỏi giỏ hàng.");
                }
                else
                {
                    return CartCommandResult.FailResult("Sản phẩm không tồn tại trong giỏ hàng.");
                }
            }
            catch (Exception ex)
            {
                return CartCommandResult.FailResult($"Lỗi khi xóa sản phẩm khỏi giỏ hàng: {ex.Message}");
            }
        }
    }
}