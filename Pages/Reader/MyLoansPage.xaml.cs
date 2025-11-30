using book.Models;
using book.Services;
using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.Maui.ApplicationModel;

namespace book.Pages.Reader
{
    public partial class MyLoansPage : ContentPage
    {
        private readonly ApiService _apiService;
        public ObservableCollection<LoanDisplay> Loans { get; } = new();

        public MyLoansPage(ApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;
            BindingContext = this;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadLoansAsync();
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

        private async void OnRefreshClicked(object sender, EventArgs e)
        {
            LoadLoansAsync();
        }

        private async void LoadLoansAsync()
        {
            try
            {
                if (LoadingIndicator != null)
                {
                    LoadingIndicator.IsRunning = true;
                    LoadingIndicator.IsVisible = true;
                }
                if (EmptyLabel != null)
                {
                    EmptyLabel.IsVisible = false;
                }

                // API returns a list of loans with nested items
                var response = await _apiService.GetAsync<JsonElement>("reader/my-loans");
                Loans.Clear();

                if (response.ValueKind == JsonValueKind.Array)
                {
                    foreach (var loanElement in response.EnumerateArray())
                    {
                        var borrowDate = loanElement.GetProperty("borrowDate").GetDateTime();
                        var dueDate = loanElement.GetProperty("dueDate").GetDateTime();
                        var status = loanElement.GetProperty("status").GetString() ?? "";

                        if (loanElement.TryGetProperty("items", out var items))
                        {
                            foreach (var item in items.EnumerateArray())
                            {
                                var title = item.GetProperty("title").GetString() ?? "";
                                var managementCode = item.GetProperty("managementCode").GetString() ?? "";
                                var itemStatus = item.GetProperty("status").GetString() ?? "";
                                
                                // Lấy coverImageUrl (API trả về camelCase)
                                string? coverImageUrl = null;
                                if (item.TryGetProperty("coverImageUrl", out var ciu) && ciu.ValueKind != JsonValueKind.Null)
                                {
                                    coverImageUrl = ciu.GetString();
                                }
                                
                                var authors = new List<string>();
                                if (item.TryGetProperty("authors", out var authorsArray))
                                {
                                    foreach (var author in authorsArray.EnumerateArray())
                                    {
                                        authors.Add(author.GetString() ?? "");
                                    }
                                }

                                Loans.Add(new LoanDisplay
                                {
                                    Title = title,
                                    BorrowDate = borrowDate,
                                    DueDate = dueDate,
                                    Status = itemStatus,
                                    Authors = string.Join(", ", authors),
                                    ManagementCode = managementCode,
                                    CoverImageUrl = coverImageUrl ?? "" // Đảm bảo không null
                                });
                            }
                        }
                    }
                }

                if (EmptyLabel != null && Loans.Count == 0)
                {
                    EmptyLabel.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Lỗi", $"Không thể tải danh sách sách mượn: {ex.Message}", "OK");
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
    }

}

