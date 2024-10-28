using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using QuanLyCuaHangMyPham.Models; // Đảm bảo đã có namespace của Models

namespace QuanLyCuaHangMyPham.IdentityModels
{
    public class ApplicationUser : IdentityUser<int>
    {
        // Các thuộc tính mở rộng cho ApplicationUser
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Address { get; set; }

        // Thêm các collection properties cho Admins, Customers, Staffs, và Favorites
        public virtual ICollection<Admin> Admins { get; set; }
        public virtual ICollection<Customer> Customers { get; set; }
        public virtual ICollection<Staff> Staffs { get; set; }
        public virtual ICollection<Favorite> Favorites { get; set; } // Thêm Favorites để khớp với lỗi
    }
}