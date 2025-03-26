using System.Threading.Tasks;

namespace QuanLyCuaHangMyPham.Handlers.Favorites
{
    // Handler kiểm tra người dùng đã xác thực
    public class AuthenticatedUserHandler : FavoriteHandlerBase
    {
        public override async Task<HandlerResult> HandleAsync(FavoriteRequestData requestData)
        {
            if (requestData.UserId <= 0)
            {
                return HandlerResult.FailureResult("Người dùng chưa đăng nhập");
            }

            return await RunNextAsync(requestData);
        }
    }
}