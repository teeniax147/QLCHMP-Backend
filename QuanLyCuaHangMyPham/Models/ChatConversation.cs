using System;
using System.Collections.Generic;

namespace QuanLyCuaHangMyPham.Models;

public partial class ChatConversation
{
    public int ConversationId { get; set; }

    public int? CustomerId { get; set; }

    public int? StaffId { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? LastMessageAt { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    public virtual Customer? Customer { get; set; }

    public virtual User? Staff { get; set; }
}
