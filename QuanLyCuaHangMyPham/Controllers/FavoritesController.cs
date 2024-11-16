using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyCuaHangMyPham.Data;
using QuanLyCuaHangMyPham.Models;

namespace QuanLyCuaHangMyPham.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FavoritesController : ControllerBase
    {
        private readonly QuanLyCuaHangMyPhamContext _context;

        public FavoritesController(QuanLyCuaHangMyPhamContext context)
        {
            _context = context;
        }

        // Lấy danh sách sản phẩm yêu thích của người dùng
        [Authorize]
        [HttpGet("user-favorites")]
        public async Task<IActionResult> GetUserFavorites()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var favorites = await _context.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Product)
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
                    f.Product.BrandId,
                    ShockPrice = f.Product.ShockPrice, // Giá khuyến mãi (nếu có)
                    Stock = f.Product.GetCurrentStock(), // Số lượng tồn kho
                })
                .ToListAsync();

            if (!favorites.Any())
            {
                return NotFound("Không có sản phẩm yêu thích nào.");
            }

            return Ok(favorites);
        }

        // Thêm sản phẩm vào danh sách yêu thích
        [HttpPost("add")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> AddToFavorites([FromBody] AddFavoriteRequest request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Kiểm tra sản phẩm đã có trong danh sách yêu thích chưa
            var existingFavorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == request.ProductId);

            if (existingFavorite != null)
            {
                return BadRequest("Sản phẩm đã có trong danh sách yêu thích.");
            }

            var favorite = new Favorite
            {
                UserId = userId,
                ProductId = request.ProductId,
                AddedAt = DateTime.Now
            };

            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();

            return Ok("Đã thêm sản phẩm vào danh sách yêu thích.");
        }

        // Xóa sản phẩm khỏi danh sách yêu thích
        [HttpDelete("remove")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> RemoveFromFavorites([FromBody] RemoveFavoriteRequest request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == request.ProductId);

            if (favorite == null)
            {
                return NotFound("Không tìm thấy sản phẩm trong danh sách yêu thích.");
            }

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();

            return Ok("Đã xóa sản phẩm khỏi danh sách yêu thích.");
        }

        private bool FavoriteExists(int id)
        {
            return _context.Favorites.Any(e => e.UserId == id);
        }
        public class AddFavoriteRequest
        {
            [Required(ErrorMessage = "Vui lòng cung cấp ID sản phẩm.")]
            public int ProductId { get; set; }
        }

        public class RemoveFavoriteRequest
        {
            [Required(ErrorMessage = "Vui lòng cung cấp ID sản phẩm.")]
            public int ProductId { get; set; }
        }
    }
}
