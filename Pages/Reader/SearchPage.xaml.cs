using book.Models;
using book.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace book.Pages.Reader
{
    public partial class SearchPage : ContentPage
    {
        private readonly ApiService _apiService;
        public ObservableCollection<Book> SearchResults { get; } = new();

        public SearchPage(ApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;
            BindingContext = this;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            // Check if we have a query parameter
            if (Shell.Current.CurrentState.Location.OriginalString.Contains("search"))
            {
                var query = Shell.Current.CurrentState.Location.Query;
                if (query.Contains("query="))
                {
                    var queryValue = Uri.UnescapeDataString(query.Split("query=")[1].Split("&")[0]);
                    if (SearchBar != null)
                    {
                        SearchBar.Text = queryValue;
                        PerformSearch(queryValue);
                    }
                }
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Cleanup timer
            _searchTimer?.Dispose();
            _searchTimer = null;
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("reader/home");
        }

        protected override bool OnBackButtonPressed()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.GoToAsync("reader/home");
            });
            return true;
        }

        private System.Threading.Timer? _searchTimer;

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            // Debounce search: đợi 500ms sau khi người dùng ngừng gõ
            _searchTimer?.Dispose();
            
            var searchText = e.NewTextValue;
            if (string.IsNullOrWhiteSpace(searchText))
            {
                SearchResults.Clear();
                if (NoResultsLabel != null)
                {
                    NoResultsLabel.IsVisible = false;
                }
                return;
            }

            _searchTimer = new System.Threading.Timer(async _ =>
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await PerformSearchAsync(searchText);
                });
            }, null, 500, Timeout.Infinite);
        }

        public ICommand SearchCommand => new Command<string>(async (query) =>
        {
            if (!string.IsNullOrWhiteSpace(query))
            {
                await PerformSearchAsync(query);
            }
            else
            {
                SearchResults.Clear();
                if (NoResultsLabel != null)
                {
                    NoResultsLabel.IsVisible = false;
                }
            }
        });

        private async Task PerformSearchAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return;
            }

            try
            {
                if (LoadingIndicator != null)
                {
                    LoadingIndicator.IsRunning = true;
                    LoadingIndicator.IsVisible = true;
                }
                if (NoResultsLabel != null)
                {
                    NoResultsLabel.IsVisible = false;
                }

                var searchQuery = $"?search={Uri.EscapeDataString(query)}";
                var response = await _apiService.GetAsync<BooksResponse>($"Books{searchQuery}");
                
                SearchResults.Clear();
                
                if (response != null && response.Data != null && response.Data.Any())
                {
                    foreach (var book in response.Data)
                    {
                        SearchResults.Add(book);
                    }
                    if (NoResultsLabel != null)
                    {
                        NoResultsLabel.IsVisible = false;
                    }
                }
                else
                {
                    if (NoResultsLabel != null)
                    {
                        NoResultsLabel.IsVisible = true;
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Lỗi", $"Tìm kiếm thất bại: {ex.Message}", "OK");
                if (NoResultsLabel != null)
                {
                    NoResultsLabel.IsVisible = true;
                }
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

        private void PerformSearch(string query)
        {
            Task.Run(async () => await PerformSearchAsync(query));
        }

        private async void OnBookTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is Book book)
            {
                NavigationDataService.SetData("BookId", book.Id);
                await Shell.Current.GoToAsync("reader/book-detail");
            }
        }
    }
}

