using QuanLyCuaHangMyPham.Data;
using QuanLyCuaHangMyPham.Models;
using System.Threading.Tasks;

namespace QuanLyCuaHangMyPham.Handlers.Favorites
{
    // Handler thêm sản phẩm vào favorites
    public class AddFavoriteHandler : FavoriteHandlerBase
    {
        private readonly QuanLyCuaHangMyPhamContext _context;

        public AddFavoriteHandler(QuanLyCuaHangMyPhamContext context)
        {
            _context = context;
        }

        public override async Task<HandlerResult> HandleAsync(FavoriteRequestData requestData)
        {
            var favorite = new Favorite
            {
                UserId = requestData.UserId,
                ProductId = requestData.ProductId,
                AddedAt = requestData.AddedAt
            };

            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();

            return HandlerResult.SuccessResult("Đã thêm sản phẩm vào danh sách yêu thích");
        }
    }
}