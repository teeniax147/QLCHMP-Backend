// UpdateGuestCartCommand.cs
using QuanLyCuaHangMyPham.Commands.Cart;
using QuanLyCuaHangMyPham.Repositories.Cart;

public class UpdateGuestCartCommand : ICartCommand
{
    private readonly CartRepository _repository;
    private readonly int _productId;
    private readonly int _quantity;

    public UpdateGuestCartCommand(
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
            var (success, cartItems) = await _repository.UpdateGuestCart(_productId, _quantity);

            if (success)
            {
                return CartCommandResult.SuccessResult("Số lượng sản phẩm đã được cập nhật.", cartItems);
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
