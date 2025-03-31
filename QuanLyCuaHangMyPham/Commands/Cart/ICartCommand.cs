// ICartCommand.cs
using System.Threading.Tasks;

namespace QuanLyCuaHangMyPham.Commands.Cart
{
    public interface ICartCommand
    {
        Task<CartCommandResult> ExecuteAsync();
    }

    public class CartCommandResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }

        public static CartCommandResult SuccessResult(string message = "Thành công", object data = null)
        {
            return new CartCommandResult { Success = true, Message = message, Data = data };
        }

        public static CartCommandResult FailResult(string message)
        {
            return new CartCommandResult { Success = false, Message = message };
        }
    }
}