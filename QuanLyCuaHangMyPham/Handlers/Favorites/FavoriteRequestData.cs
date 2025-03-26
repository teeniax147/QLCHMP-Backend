using System;

namespace QuanLyCuaHangMyPham.Handlers.Favorites
{
    // Class chứa dữ liệu cho request yêu thích sản phẩm
    public class FavoriteRequestData
    {
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.Now;
    }
}