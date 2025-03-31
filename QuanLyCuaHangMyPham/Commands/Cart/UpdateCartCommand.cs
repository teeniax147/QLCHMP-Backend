// UpdateCartCommand.cs
using QuanLyCuaHangMyPham.Repositories;
using QuanLyCuaHangMyPham.Repositories.Cart;
using System;
using System.Threading.Tasks;

namespace QuanLyCuaHangMyPham.Commands.Cart
{
    public class UpdateCartCommand : ICartCommand
    {
        private readonly CartRepository _repository;
        private readonly int _userId;
        private readonly int _productId;
        private readonly int _quantity;

        public UpdateCartCommand(
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
                bool success = await _repository.UpdateCart(_userId, _productId, _quantity);

                if (success)
                {
                    return CartCommandResult.SuccessResult("Số lượng sản phẩm đã được cập nhật.");
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
