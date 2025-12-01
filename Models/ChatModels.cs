using System.Collections.ObjectModel;

namespace book.Models
{
    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    public class ChatResponse
    {
        public string Response { get; set; } = string.Empty;
        public List<Book>? SuggestedBooks { get; set; }
    }

    // Model dùng cho giao diện Chat
    public class ChatMessage
    {
        public string Text { get; set; } = string.Empty;
        public bool IsBot { get; set; }
        public bool IsUser { get; set; } = false;

        // Thêm các trường này để xử lý nút "Xem chi tiết"
        public bool HasSuggestion { get; set; } = false;
        public Book? SuggestedBook { get; set; }
    }
}