using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
    public class InventoriesController : ControllerBase
    {
        private readonly QuanLyCuaHangMyPhamContext _context;

        public InventoriesController(QuanLyCuaHangMyPhamContext context)
        {
            _context = context;
        }

        // GET: api/Inventory
        [HttpGet]
        
        public async Task<ActionResult<IEnumerable<Inventory>>> GetInventories()
        {
            return await _context.Inventories.Include(i => i.Product).ToListAsync();
        }

        // GET: api/Inventory/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Inventory>> GetInventory(int id)
        {
            var inventory = await _context.Inventories.Include(i => i.Product).FirstOrDefaultAsync(i => i.InventoryId == id);

            if (inventory == null)
            {
                return NotFound("Không tìm thấy thông tin kho hàng.");
            }

            return inventory;
        }

        // POST: api/Inventory
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Inventory>> CreateInventory([FromBody] InventoryCreateRequest request)
        {
            var inventory = new Inventory
            {
                ProductId = request.ProductId,
                WarehouseLocation = request.WarehouseLocation,
                QuantityInStock = request.QuantityInStock,
                LastUpdated = DateTime.Now
            };

            _context.Inventories.Add(inventory);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetInventory", new { id = inventory.InventoryId }, inventory);
        }

        // PUT: api/Inventory/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateInventory(int id, [FromBody] InventoryUpdateRequest request)
        {
            if (id != request.InventoryId)
            {
                return BadRequest("ID kho hàng không khớp.");
            }

            var inventory = await _context.Inventories.FindAsync(id);

            if (inventory == null)
            {
                return NotFound("Không tìm thấy thông tin kho hàng.");
            }

            inventory.ProductId = request.ProductId;
            inventory.WarehouseLocation = request.WarehouseLocation;
            inventory.QuantityInStock = request.QuantityInStock;
            inventory.LastUpdated = DateTime.Now;

            _context.Entry(inventory).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InventoryExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Inventory/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteInventory(int id)
        {
            var inventory = await _context.Inventories.FindAsync(id);

            if (inventory == null)
            {
                return NotFound("Không tìm thấy thông tin kho hàng.");
            }

            _context.Inventories.Remove(inventory);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool InventoryExists(int id)
        {
            return _context.Inventories.Any(e => e.InventoryId == id);
        }
        public class InventoryCreateRequest
        {
            public int ProductId { get; set; }

            public string? WarehouseLocation { get; set; }

            [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho phải lớn hơn hoặc bằng 0.")]
            public int QuantityInStock { get; set; }
        }
        public class InventoryUpdateRequest
        {
            [Required(ErrorMessage = "ID kho hàng là bắt buộc.")]
            public int InventoryId { get; set; }

            public int ProductId { get; set; }

            public string? WarehouseLocation { get; set; }

            [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho phải lớn hơn hoặc bằng 0.")]
            public int QuantityInStock { get; set; }
        }
    }
}
