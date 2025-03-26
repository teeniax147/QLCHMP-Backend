using Microsoft.EntityFrameworkCore;
using QuanLyCuaHangMyPham.Data;
using System.Threading.Tasks;

namespace QuanLyCuaHangMyPham.Handlers.Favorites
{
    public class FavoriteExistsHandler : FavoriteHandlerBase
    {
        private readonly QuanLyCuaHangMyPhamContext _context;

        public FavoriteExistsHandler(QuanLyCuaHangMyPhamContext context)
        {
            _context = context;
        }

        public override async Task<HandlerResult> HandleAsync(FavoriteRequestData requestData)
        {
            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == requestData.UserId && f.ProductId == requestData.ProductId);

            if (favorite == null)
            {
                return HandlerResult.FailureResult("Không tìm thấy sản phẩm trong danh sách yêu thích.");
            }

            return await RunNextAsync(requestData);
        }
    }
}