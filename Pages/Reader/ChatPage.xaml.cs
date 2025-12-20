using book.Models;
using book.Services;
using System.Collections.ObjectModel;

namespace book.Pages.Reader
{
    public partial class ChatPage : ContentPage
    {
        private readonly ApiService _apiService;

        // Sửa: Thêm BindingContext để XAML nhận diện được Binding Messages
        public ObservableCollection<ChatMessage> Messages { get; } = new();

        public ChatPage(ApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;

            // QUAN TRỌNG: Gán BindingContext là chính trang này
            BindingContext = this;

            // Xóa dòng này vì đã bind trong XAML: MessagesCollection.ItemsSource = Messages; 

            Messages.Add(new ChatMessage { Text = "Xin chào! Tôi là trợ lý AI. Bạn muốn tìm sách gì?", IsBot = true });
        }

        // THÊM: Hàm xử lý nút quay lại
        private async void OnBackClicked(object sender, EventArgs e)
        {
            // Quay lại trang trước đó trong Navigation Stack
            await Shell.Current.GoToAsync("reader/home");
        }

        private async void OnSendClicked(object sender, EventArgs e)
        {
            var text = MessageEntry.Text;
            if (string.IsNullOrWhiteSpace(text)) return;

            // 1. Hiện tin nhắn user
            Messages.Add(new ChatMessage { Text = text, IsUser = true });
            MessageEntry.Text = string.Empty;

            // 2. Hiện loading (Giả lập typing)
            var loadingMsg = new ChatMessage { Text = "...", IsBot = true };
            Messages.Add(loadingMsg);

            // Scroll xuống ngay lập tức
            if (Messages.Count > 0)
                MessagesCollection.ScrollTo(Messages.Count - 1);

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

                // Kiểm tra sách gợi ý
                if (response.SuggestedBooks != null && response.SuggestedBooks.Any())
                {
                    botMsg.HasSuggestion = true;
                    botMsg.SuggestedBook = response.SuggestedBooks.First();
                }

                Messages.Add(botMsg);

                // Scroll xuống cuối
                if (Messages.Count > 0)
                {
                    // Thêm delay nhỏ để UI kịp render trước khi scroll
                    await Task.Delay(100);
                    MessagesCollection.ScrollTo(Messages.Count - 1, animate: true);
                }
            }
        }

        private async void OnViewBookDetailClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Book book)
            {
                NavigationDataService.SetData("BookId", book.Id);
                await Shell.Current.GoToAsync("reader/book-detail");
            }
        }
    }
}