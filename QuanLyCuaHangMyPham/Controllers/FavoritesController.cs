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
using QuanLyCuaHangMyPham.Handlers.Favorites;
using QuanLyCuaHangMyPham.Models;

namespace QuanLyCuaHangMyPham.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FavoritesController : ControllerBase
    {
        private readonly QuanLyCuaHangMyPhamContext _context;
        private readonly FavoriteHandlerChain _favoriteHandlerChain;

        public FavoritesController(QuanLyCuaHangMyPhamContext context, FavoriteHandlerChain favoriteHandlerChain)

        {

            _context = context;

            _favoriteHandlerChain = favoriteHandlerChain;

        }

        // Lấy danh sách sản phẩm yêu thích của người dùng

        [Authorize]

        [HttpGet("user-favorites")]

        public async Task<IActionResult> GetUserFavorites()

        {

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));



            // Tạo request data

            var requestData = new FavoriteRequestData

            {

                UserId = userId

            };



            // Khởi tạo và thực thi chuỗi handler

            var handlerChain = new FavoriteHandlerChain(_context);

            var result = await handlerChain.ProcessGetUserFavorites(requestData);



            if (!result.Success)

            {

                return NotFound(result.Message);

            }



            // Trả về danh sách yêu thích

            return Ok(result.Data);

        }

        // Thêm sản phẩm vào danh sách yêu thích
        [HttpPost("add")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> AddToFavorites([FromBody] AddFavoriteRequest request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Tạo request data
            var requestData = new FavoriteRequestData
            {
                UserId = userId,
                ProductId = request.ProductId
            };

            // Khởi tạo và thực thi chuỗi handler
            var handlerChain = new FavoriteHandlerChain(_context);
            var result = await handlerChain.ProcessAddToFavorite(requestData);

            if (!result.Success)
            {
                return BadRequest(result.Message);
            }

            return Ok(result.Message);
        }
        // Xóa sản phẩm khỏi danh sách yêu thích

        [HttpDelete("remove")]

        [Authorize(Roles = "Customer")]

        public async Task<IActionResult> RemoveFromFavorites([FromBody] RemoveFavoriteRequest request)

        {

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));



            // Tạo request data

            var requestData = new FavoriteRequestData

            {

                UserId = userId,

                ProductId = request.ProductId

            };



            // Khởi tạo và thực thi chuỗi handler

            var handlerChain = new FavoriteHandlerChain(_context);

            var result = await handlerChain.ProcessRemoveFromFavorites(requestData);



            if (!result.Success)

            {

                return NotFound(result.Message);

            }



            return Ok(result.Message);

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
