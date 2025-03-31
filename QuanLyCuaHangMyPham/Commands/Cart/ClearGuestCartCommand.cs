// ClearGuestCartCommand.cs
using QuanLyCuaHangMyPham.Commands.Cart;
using QuanLyCuaHangMyPham.Repositories.Cart;

public class ClearGuestCartCommand : ICartCommand
{
    private readonly CartRepository _repository;

    public ClearGuestCartCommand(CartRepository repository)
    {
        _repository = repository;
    }

    public async Task<CartCommandResult> ExecuteAsync()
    {
        try
        {
            bool success = await _repository.ClearGuestCart();

            if (success)
            {
                return CartCommandResult.SuccessResult("Giỏ hàng đã được xóa sạch.");
            }
            else
            {
                return CartCommandResult.FailResult("Giỏ hàng trống.");
            }
        }
        catch (Exception ex)
        {
            return CartCommandResult.FailResult($"Lỗi khi xóa giỏ hàng: {ex.Message}");
        }
    }
}