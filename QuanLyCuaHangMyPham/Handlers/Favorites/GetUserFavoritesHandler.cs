using Microsoft.EntityFrameworkCore;
using QuanLyCuaHangMyPham.Data;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyCuaHangMyPham.Handlers.Favorites
{
    public class GetUserFavoritesHandler : FavoriteHandlerBase
    {
        private readonly QuanLyCuaHangMyPhamContext _context;

        public GetUserFavoritesHandler(QuanLyCuaHangMyPhamContext context)
        {
            _context = context;
        }

        public override async Task<HandlerResult> HandleAsync(FavoriteRequestData requestData)
        {
            var favorites = await _context.Favorites
                .Where(f => f.UserId == requestData.UserId)
                .Include(f => f.Product)
                 .ThenInclude(p => p.Brand)
                .Select(f => new
                {
                    f.Product.Id,
                    f.Product.Name,
                    f.Product.Price,
                    f.Product.OriginalPrice,
                    f.Product.Description,
                    f.Product.ImageUrl,
                    f.Product.FavoriteCount,
                    f.Product.ReviewCount,
                    f.Product.AverageRating,
                    f.Product.CreatedAt,
                    BrandName = f.Product.Brand.Name,
                    ShockPrice = f.Product.ShockPrice,
                    Stock = f.Product.GetCurrentStock(),
                })
                .ToListAsync();

            if (!favorites.Any())
            {
                return HandlerResult.FailureResult("Không có sản phẩm yêu thích nào.");
            }

            return HandlerResult.SuccessResult("Lấy danh sách yêu thích thành công", favorites);
        }
    }
}