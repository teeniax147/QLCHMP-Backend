// GetCartDetailsCommand.cs
using QuanLyCuaHangMyPham.Repositories;
using QuanLyCuaHangMyPham.Repositories.Cart;
using System;
using System.Threading.Tasks;

namespace QuanLyCuaHangMyPham.Commands.Cart
{
    public class GetCartDetailsCommand : ICartCommand
    {
        private readonly CartRepository _repository;
        private readonly int _userId;

        public GetCartDetailsCommand(CartRepository repository, int userId)
        {
            _repository = repository;
            _userId = userId;
        }

        public async Task<CartCommandResult> ExecuteAsync()
        {
            try
            {
                var (success, cartDetails) = await _repository.GetCartDetails(_userId);

                if (success)
                {
                    return CartCommandResult.SuccessResult("Lấy giỏ hàng thành công", cartDetails);
                }
                else
                {
                    return CartCommandResult.FailResult("Không tìm thấy thông tin khách hàng.");
                }
            }
            catch (Exception ex)
            {
                return CartCommandResult.FailResult($"Lỗi khi lấy chi tiết giỏ hàng: {ex.Message}");
            }
        }
    }
}