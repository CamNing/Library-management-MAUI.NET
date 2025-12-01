using book.Models;
using book.Services;
using System.Collections.ObjectModel;

namespace book.Pages.Reader
{
    public partial class ChatPage : ContentPage
    {
        private readonly ApiService _apiService;
        public ObservableCollection<ChatMessage> Messages { get; } = new();

        public ChatPage(ApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;
            MessagesCollection.ItemsSource = Messages;
            Messages.Add(new ChatMessage { Text = "Xin chào! Tôi là trợ lý AI. Bạn muốn tìm sách gì?", IsBot = true });
        }

        private async void OnSendClicked(object sender, EventArgs e)
        {
            var text = MessageEntry.Text;
            if (string.IsNullOrWhiteSpace(text)) return;

            // 1. Hiện tin nhắn user
            Messages.Add(new ChatMessage { Text = text, IsUser = true });
            MessageEntry.Text = string.Empty;

            // 2. Hiện loading
            var loadingMsg = new ChatMessage { Text = "Đang tra cứu dữ liệu...", IsBot = true };
            Messages.Add(loadingMsg);

            // 3. Gọi API
            var response = await _apiService.ChatWithAiAsync(text);
            
            Messages.Remove(loadingMsg);

            if (response != null)
            {
                var botMsg = new ChatMessage 
                { 
                    Text = response.Response, 
                    IsBot = true 
                };

                // Kiểm tra xem AI có trả về sách gợi ý không
                if (response.SuggestedBooks != null && response.SuggestedBooks.Any())
                {
                    botMsg.HasSuggestion = true;
                    botMsg.SuggestedBook = response.SuggestedBooks.First();
                }

                Messages.Add(botMsg);
                
                // Scroll xuống cuối
                if (Messages.Count > 0)
                {
                    MessagesCollection.ScrollTo(Messages.Count - 1);
                }
            }
        }

        // Sự kiện khi bấm nút "Xem chi tiết"
        private async void OnViewBookDetailClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Book book)
            {
                // Lưu ID sách vào service điều hướng
                NavigationDataService.SetData("BookId", book.Id);
                
                // Chuyển sang trang chi tiết sách
                // Lưu ý: Cần đăng ký route này trong AppShell
                await Shell.Current.GoToAsync("reader/book-detail");
            }
        }
    }
}