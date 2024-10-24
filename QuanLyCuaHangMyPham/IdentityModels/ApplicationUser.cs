using Microsoft.AspNetCore.Identity;

namespace QuanLyCuaHangMyPham.IdentityModels
{
    public class ApplicationUser : IdentityUser<int>
    {
        // Bạn có thể thêm các thuộc tính bổ sung vào đây nếu cần
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Address { get; set; }
    }
}