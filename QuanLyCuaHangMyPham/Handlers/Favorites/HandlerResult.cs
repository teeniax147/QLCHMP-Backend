namespace QuanLyCuaHangMyPham.Handlers.Favorites
{
    // Class chứa kết quả từ handler chain
    public class HandlerResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        // Thêm thuộc tính để chứa dữ liệu trả về

        public object Data { get; set; }
        // Đổi tên phương thức từ Success sang SuccessResult
        public static HandlerResult SuccessResult(string message = "Thành công", object data = null)
        {
            return new HandlerResult { Success = true, Message = message, Data = data };
        }

        // Đổi tên phương thức từ Failure sang FailureResult
        public static HandlerResult FailureResult(string message)
        {
            return new HandlerResult { Success = false, Message = message };
        }
    }
}