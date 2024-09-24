using System;
using System.Collections.Generic;

namespace QuanLyCuaHangMyPham.Models;

public partial class ChatMessage
{
    public int MessageId { get; set; }

    public int? ConversationId { get; set; }

    public int? SenderId { get; set; }

    public string? SenderRole { get; set; }

    public string? MessageText { get; set; }

    public DateTime? SentAt { get; set; }

    public virtual ChatConversation? Conversation { get; set; }
}
