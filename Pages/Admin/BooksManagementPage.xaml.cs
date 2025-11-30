using book.Models;
using book.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;

namespace book.Pages.Admin
{
    public partial class BooksManagementPage : ContentPage
    {
        private readonly ApiService _apiService;
        public ObservableCollection<Book> Books { get; } = new();

        public BooksManagementPage(ApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;
            BindingContext = this;
            LoadBooksAsync();
        }

        private async void LoadBooksAsync()
        {
            try
            {
                var response = await _apiService.GetAsync<BooksResponse>("Books");
                if (response != null && response.Data != null)
                {
                    Books.Clear();
                    foreach (var book in response.Data)
                    {
                        Books.Add(book);
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Lỗi", $"Không thể tải danh sách sách: {ex.Message}", "OK");
            }
        }

        private async void OnAddBookClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("admin/add-book");
        }

        private async void OnEditBookClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Book book)
            {
                // Store book ID in navigation service
                NavigationDataService.SetEditBookId(book.Id);
                System.Diagnostics.Debug.WriteLine($"Navigating to edit-book with ID: {book.Id}");
                await Shell.Current.GoToAsync("admin/edit-book");
            }
        }

        private async void OnDeleteBookClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Book book)
            {
                var confirm = await DisplayAlert("Xác nhận", 
                    $"Xóa {book.Title}?", "Có", "Không");
                if (confirm)
                {
                    try
                    {
                        await _apiService.DeleteAsync($"admin/books/{book.Id}");
                        Books.Remove(book);
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlertAsync("Lỗi", $"Không thể xóa: {ex.Message}", "OK");
                    }
                }
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("admin/dashboard");
        }

        public ICommand SearchCommand => new Command<string>(async (query) =>
        {
            try
            {
                var searchQuery = string.IsNullOrWhiteSpace(query) ? "" : $"?search={Uri.EscapeDataString(query)}";
                var response = await _apiService.GetAsync<BooksResponse>($"Books{searchQuery}");
                if (response != null && response.Data != null)
                {
                    Books.Clear();
                    foreach (var book in response.Data)
                    {
                        Books.Add(book);
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Lỗi", $"Tìm kiếm thất bại: {ex.Message}", "OK");
            }
        });

        protected override bool OnBackButtonPressed()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.GoToAsync("admin/dashboard");
            });
            return true;
        }
    }
}

