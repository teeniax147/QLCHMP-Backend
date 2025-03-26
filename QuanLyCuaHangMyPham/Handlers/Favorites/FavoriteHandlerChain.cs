using QuanLyCuaHangMyPham.Data;
using System.Threading.Tasks;

namespace QuanLyCuaHangMyPham.Handlers.Favorites
{
    public class FavoriteHandlerChain
    {
        private readonly QuanLyCuaHangMyPhamContext _context;

        public FavoriteHandlerChain(QuanLyCuaHangMyPhamContext context)
        {
            _context = context;
        }

        public async Task<HandlerResult> ProcessAddToFavorite(FavoriteRequestData requestData)
        {
            // Tạo và cấu hình chuỗi handler
            var authHandler = new AuthenticatedUserHandler();
            var productHandler = new ProductExistsHandler(_context);
            var duplicateHandler = new NotAlreadyFavoriteHandler(_context);
            var addHandler = new AddFavoriteHandler(_context);

            // Thiết lập chuỗi
            authHandler
                .SetNext(productHandler)
                .SetNext(duplicateHandler)
                .SetNext(addHandler);

            // Thực thi chuỗi và trả về kết quả
            return await authHandler.HandleAsync(requestData);
        }

        public async Task<HandlerResult> ProcessGetUserFavorites(FavoriteRequestData requestData)
        {
            // Tạo và cấu hình chuỗi handler
            var authHandler = new AuthenticatedUserHandler();
            var getUserFavoritesHandler = new GetUserFavoritesHandler(_context);

            // Thiết lập chuỗi
            authHandler.SetNext(getUserFavoritesHandler);

            // Thực thi chuỗi và trả về kết quả
            return await authHandler.HandleAsync(requestData);
        }

        public async Task<HandlerResult> ProcessRemoveFromFavorites(FavoriteRequestData requestData)
        {
            // Tạo và cấu hình chuỗi handler
            var authHandler = new AuthenticatedUserHandler();
            var favoriteExistsHandler = new FavoriteExistsHandler(_context);
            var removeHandler = new RemoveFavoriteHandler(_context);

            // Thiết lập chuỗi
            authHandler
                .SetNext(favoriteExistsHandler)
                .SetNext(removeHandler);

            // Thực thi chuỗi và trả về kết quả
            return await authHandler.HandleAsync(requestData);
        }
    }
}