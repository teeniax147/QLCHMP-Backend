// GetGuestCartDetailsCommand.cs
using QuanLyCuaHangMyPham.Commands.Cart;
using QuanLyCuaHangMyPham.Repositories.Cart;

public class GetGuestCartDetailsCommand : ICartCommand
{
    private readonly CartRepository _repository;

    public GetGuestCartDetailsCommand(CartRepository repository)
    {
        _repository = repository;
    }

    public async Task<CartCommandResult> ExecuteAsync()
    {
        try
        {
            var (success, cartDetails) = await _repository.GetGuestCartDetails();

            if (success)
            {
                return CartCommandResult.SuccessResult("Lấy chi tiết giỏ hàng thành công", cartDetails);
            }
            else
            {
                return CartCommandResult.FailResult($"Lỗi khi lấy chi tiết giỏ hàng: {cartDetails}");
            }
        }
        catch (Exception ex)
        {
            return CartCommandResult.FailResult($"Lỗi khi lấy chi tiết giỏ hàng: {ex.Message}");
        }
    }
}