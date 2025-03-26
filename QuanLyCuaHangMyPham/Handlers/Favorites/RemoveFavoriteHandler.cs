using Microsoft.EntityFrameworkCore;
using QuanLyCuaHangMyPham.Data;
using System.Threading.Tasks;

namespace QuanLyCuaHangMyPham.Handlers.Favorites
{
    public class RemoveFavoriteHandler : FavoriteHandlerBase
    {
        private readonly QuanLyCuaHangMyPhamContext _context;

        public RemoveFavoriteHandler(QuanLyCuaHangMyPhamContext context)
        {
            _context = context;
        }

        public override async Task<HandlerResult> HandleAsync(FavoriteRequestData requestData)
        {
            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == requestData.UserId && f.ProductId == requestData.ProductId);

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();

            return HandlerResult.SuccessResult("Đã xóa sản phẩm khỏi danh sách yêu thích.");
        }
    }
}