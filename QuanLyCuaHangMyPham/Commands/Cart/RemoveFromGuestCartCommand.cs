// RemoveFromGuestCartCommand.cs
using QuanLyCuaHangMyPham.Commands.Cart;
using QuanLyCuaHangMyPham.Repositories.Cart;

public class RemoveFromGuestCartCommand : ICartCommand
{
    private readonly CartRepository _repository;
    private readonly int _productId;
    private readonly int _quantity;

    public RemoveFromGuestCartCommand(
        CartRepository repository,
        int productId,
        int quantity)
    {
        _repository = repository;
        _productId = productId;
        _quantity = quantity;
    }

    public async Task<CartCommandResult> ExecuteAsync()
    {
        try
        {
            var (success, cartItems) = await _repository.RemoveFromGuestCart(_productId, _quantity);

            if (success)
            {
                return CartCommandResult.SuccessResult("Cập nhật giỏ hàng thành công.", cartItems);
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