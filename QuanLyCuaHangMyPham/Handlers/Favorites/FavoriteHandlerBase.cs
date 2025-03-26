using System.Threading.Tasks;

namespace QuanLyCuaHangMyPham.Handlers.Favorites
{
    // Lớp cơ sở cho tất cả handler trong chuỗi
    public abstract class FavoriteHandlerBase
    {
        protected FavoriteHandlerBase _nextHandler;

        public FavoriteHandlerBase SetNext(FavoriteHandlerBase handler)
        {
            _nextHandler = handler;
            return handler;
        }

        public abstract Task<HandlerResult> HandleAsync(FavoriteRequestData requestData);

        protected async Task<HandlerResult> RunNextAsync(FavoriteRequestData requestData)
        {
            if (_nextHandler != null)
            {
                return await _nextHandler.HandleAsync(requestData);
            }

            return HandlerResult.SuccessResult();
        }
    }
}