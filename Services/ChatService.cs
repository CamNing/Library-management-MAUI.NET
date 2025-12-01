using book.Models;
using System.Collections.ObjectModel;

namespace book.Services
{
    public class ChatService
    {
        // Danh sách tin nhắn được lưu ở đây, tồn tại xuyên suốt các trang
        public ObservableCollection<ChatMessage> Messages { get; } = new();

        public ChatService()
        {
            // Tin nhắn mặc định ban đầu
            Reset();
        }

        public void AddMessage(string text, bool isUser, bool isBot, bool hasSuggestion = false, Book? book = null)
        {
            Messages.Add(new ChatMessage
            {
                Text = text,
                IsUser = isUser,
                IsBot = isBot,
                HasSuggestion = hasSuggestion,
                SuggestedBook = book
            });
        }

        public void Clear()
        {
            Messages.Clear();
        }

        public void Reset()
        {
            Clear();
            // Thêm lại tin nhắn chào mừng
            AddMessage("Xin chào! Tôi là trợ lý AI. Bạn muốn tìm sách gì?", false, true);
        }
    }
}