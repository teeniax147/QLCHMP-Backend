// AddToCartCommand.cs
using QuanLyCuaHangMyPham.Repositories;
using QuanLyCuaHangMyPham.Repositories.Cart;
using System;
using System.Threading.Tasks;

namespace QuanLyCuaHangMyPham.Commands.Cart
{
    public class AddToCartCommand : ICartCommand
    {
        private readonly CartRepository _repository;
        private readonly int _userId;
        private readonly int _productId;
        private readonly int _quantity;

        public AddToCartCommand(
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
                bool success = await _repository.AddToCart(_userId, _productId, _quantity);

                if (success)
                {
                    return CartCommandResult.SuccessResult("Sản phẩm đã được thêm vào giỏ hàng.");
                }
                else
                {
                    return CartCommandResult.FailResult("Không thể thêm sản phẩm vào giỏ hàng.");
                }
            }
            catch (Exception ex)
            {
                return CartCommandResult.FailResult($"Lỗi khi thêm sản phẩm vào giỏ hàng: {ex.Message}");
            }
        }
    }
}