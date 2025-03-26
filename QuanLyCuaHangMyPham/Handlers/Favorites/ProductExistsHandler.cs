using QuanLyCuaHangMyPham.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace QuanLyCuaHangMyPham.Handlers.Favorites
{
    // Handler kiểm tra sản phẩm tồn tại
    public class ProductExistsHandler : FavoriteHandlerBase
    {
        private readonly QuanLyCuaHangMyPhamContext _context;

        public ProductExistsHandler(QuanLyCuaHangMyPhamContext context)
        {
            _context = context;
        }

        public override async Task<HandlerResult> HandleAsync(FavoriteRequestData requestData)
        {
            var productExists = await _context.Products.AnyAsync(p => p.Id == requestData.ProductId);

            if (!productExists)
            {
                return HandlerResult.FailureResult("Không tìm thấy sản phẩm");
            }

            return await RunNextAsync(requestData);
        }
    }
}