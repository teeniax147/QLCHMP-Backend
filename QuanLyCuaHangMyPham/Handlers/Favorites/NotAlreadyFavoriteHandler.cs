using QuanLyCuaHangMyPham.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace QuanLyCuaHangMyPham.Handlers.Favorites
{
    // Handler kiểm tra sản phẩm chưa có trong yêu thích
    public class NotAlreadyFavoriteHandler : FavoriteHandlerBase
    {
        private readonly QuanLyCuaHangMyPhamContext _context;

        public NotAlreadyFavoriteHandler(QuanLyCuaHangMyPhamContext context)
        {
            _context = context;
        }

        public override async Task<HandlerResult> HandleAsync(FavoriteRequestData requestData)
        {
            var existingFavorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == requestData.UserId && f.ProductId == requestData.ProductId);

            if (existingFavorite != null)
            {
                return HandlerResult.FailureResult("Sản phẩm đã có trong danh sách yêu thích");
            }

            return await RunNextAsync(requestData);
        }
    }
}