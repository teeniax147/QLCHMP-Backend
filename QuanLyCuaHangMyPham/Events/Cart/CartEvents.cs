// File: Events/CartEvents.cs
namespace QuanLyCuaHangMyPham.Events
{
    public class CartUpdatedEvent
    {
        public int UserId { get; set; }
        public string Action { get; set; } // "Add", "Remove", "Update", "Clear"
        public int? ProductId { get; set; }
        public int? Quantity { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}