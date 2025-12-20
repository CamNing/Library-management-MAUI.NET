using book.Models;
using book.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace book.Pages.Reader
{
    public partial class ReaderHomePage : ContentPage
    {
        private readonly ApiService _apiService;
        private readonly AuthService _authService;

        public ObservableCollection<Book> PopularBooks { get; } = new();
        public ObservableCollection<Book> NewBooks { get; } = new();
        public ObservableCollection<Book> MostAccessedBooks { get; } = new();

        public ReaderHomePage(ApiService apiService, AuthService authService)
        {
            InitializeComponent();
            _apiService = apiService;
            _authService = authService;
            BindingContext = this;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadBooksAsync();
        }

        protected override bool OnBackButtonPressed()
        {
            // Prevent going back from home page, show logout option instead
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                var result = await DisplayAlertAsync("Đăng xuất", "Bạn có muốn đăng xuất?", "Có", "Không");
                if (result)
                {
                    await _authService.LogoutAsync();
                    await Shell.Current.GoToAsync("///login");
                }
            });
            return true;
        }

        private async void LoadBooksAsync()
        {
            try
            {
                if (LoadingIndicator != null)
                {
                    LoadingIndicator.IsRunning = true;
                    LoadingIndicator.IsVisible = true;
                }

                PopularBooks.Clear();
                NewBooks.Clear();
                MostAccessedBooks.Clear();

                var popular = await _apiService.GetAsync<List<Book>>("Books/popular");
                if (popular != null)
                {
                    foreach (var book in popular)
                    {
                        PopularBooks.Add(book);
                    }
                }

                var newBooks = await _apiService.GetAsync<List<Book>>("Books/new");
                if (newBooks != null)
                {
                    foreach (var book in newBooks)
                    {
                        NewBooks.Add(book);
                    }
                }

                var mostAccessed = await _apiService.GetAsync<List<Book>>("Books/most-accessed");
                if (mostAccessed != null)
                {
                    foreach (var book in mostAccessed)
                    {
                        MostAccessedBooks.Add(book);
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Lỗi", $"Không thể tải sách: {ex.Message}", "OK");
            }
            finally
            {
                if (LoadingIndicator != null)
                {
                    LoadingIndicator.IsRunning = false;
                    LoadingIndicator.IsVisible = false;
                }
            }
        }

        private async void OnMenuClicked(object sender, EventArgs e)
        {
            var action = await DisplayActionSheet("Menu", "Hủy", null, "Sách của tôi", "Tìm kiếm Sách", "Đăng xuất");
            switch (action)
            {
                case "Sách của tôi":
                    await Shell.Current.GoToAsync("reader/loans");
                    break;
                case "Tìm kiếm Sách":
                    await Shell.Current.GoToAsync("reader/search");
                    break;
                case "Đăng xuất":
                    await _authService.LogoutAsync();
                    await Shell.Current.GoToAsync("///login");
                    break;
            }
        }

        private async void OnMyLoansClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("reader/loans");
        }

        private async void OnSearchClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("reader/search");
        }
        private async void OnChatBubbleClicked(object sender, EventArgs e)
        {
            // Điều hướng đến trang chat
            // Đảm bảo bạn đã đăng ký ChatPage trong MauiProgram.cs và AppShell.xaml.cs
            await Navigation.PushAsync(new ChatPage(_apiService));
        }
        private async void OnBookTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is Book book)
            {
                NavigationDataService.SetData("BookId", book.Id);
                await Shell.Current.GoToAsync("reader/book-detail");
            }
        }
        // Thêm hàm này vào class ReaderHomePage
        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            // Hỏi xác nhận trước khi đăng xuất cho chuyên nghiệp
            bool answer = await DisplayAlert("Đăng xuất", "Bạn có chắc chắn muốn đăng xuất?", "Có", "Không");
            if (answer)
            {
                await _authService.LogoutAsync();
                // Quay về trang Login
                await Shell.Current.GoToAsync("///login");
            }
        }

        private async void OnViewDetailClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Book book)
            {
                NavigationDataService.SetData("BookId", book.Id);
                await Shell.Current.GoToAsync("reader/book-detail");
            }
        }
    }
}

