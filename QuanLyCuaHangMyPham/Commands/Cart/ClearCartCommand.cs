// ClearCartCommand.cs
using QuanLyCuaHangMyPham.Repositories;
using QuanLyCuaHangMyPham.Repositories.Cart;
using System;
using System.Threading.Tasks;

namespace QuanLyCuaHangMyPham.Commands.Cart
{
    public class ClearCartCommand : ICartCommand
    {
        private readonly CartRepository _repository;
        private readonly int _userId;

        public ClearCartCommand(CartRepository repository, int userId)
        {
            _repository = repository;
            _userId = userId;
        }

        public async Task<CartCommandResult> ExecuteAsync()
        {
            try
            {
                bool success = await _repository.ClearCart(_userId);

                if (success)
                {
                    return CartCommandResult.SuccessResult("Giỏ hàng đã được xóa sạch.");
                }
                else
                {
                    return CartCommandResult.FailResult("Giỏ hàng không tồn tại.");
                }
            }
            catch (Exception ex)
            {
                return CartCommandResult.FailResult($"Lỗi khi xóa giỏ hàng: {ex.Message}");
            }
        }
    }
}