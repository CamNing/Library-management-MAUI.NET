using book.Models;
using book.Services;
using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.Maui.ApplicationModel;

namespace book.Pages.Admin
{
    public partial class BorrowRequestsPage : ContentPage
    {
        private readonly ApiService _apiService;
        public ObservableCollection<BorrowRequestDisplay> Requests { get; } = new();
        private string? _currentFilter = null; // null = All

        public BorrowRequestsPage(ApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;
            BindingContext = this;
            UpdateFilterButtons("All");
            LoadRequestsAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadRequestsAsync();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("admin/dashboard");
        }

        protected override bool OnBackButtonPressed()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.GoToAsync("admin/dashboard");
            });
            return true;
        }

        private async void OnFilterAllClicked(object sender, EventArgs e)
        {
            _currentFilter = null;
            UpdateFilterButtons("All");
            await LoadRequestsAsync();
        }

        private async void OnFilterPendingClicked(object sender, EventArgs e)
        {
            _currentFilter = "Pending";
            UpdateFilterButtons("Pending");
            await LoadRequestsAsync();
        }

        private async void OnFilterApprovedClicked(object sender, EventArgs e)
        {
            _currentFilter = "Approved";
            UpdateFilterButtons("Approved");
            await LoadRequestsAsync();
        }

        private async void OnFilterRejectedClicked(object sender, EventArgs e)
        {
            _currentFilter = "Rejected";
            UpdateFilterButtons("Rejected");
            await LoadRequestsAsync();
        }

        private void UpdateFilterButtons(string activeFilter)
        {
            if (FilterAllButton != null)
            {
                FilterAllButton.BackgroundColor = activeFilter == "All" ? Colors.Blue : Colors.LightGray;
                FilterAllButton.TextColor = activeFilter == "All" ? Colors.White : Colors.Black;
            }
            if (FilterPendingButton != null)
            {
                FilterPendingButton.BackgroundColor = activeFilter == "Pending" ? Colors.Blue : Colors.LightGray;
                FilterPendingButton.TextColor = activeFilter == "Pending" ? Colors.White : Colors.Black;
            }
            if (FilterApprovedButton != null)
            {
                FilterApprovedButton.BackgroundColor = activeFilter == "Approved" ? Colors.Blue : Colors.LightGray;
                FilterApprovedButton.TextColor = activeFilter == "Approved" ? Colors.White : Colors.Black;
            }
            if (FilterRejectedButton != null)
            {
                FilterRejectedButton.BackgroundColor = activeFilter == "Rejected" ? Colors.Blue : Colors.LightGray;
                FilterRejectedButton.TextColor = activeFilter == "Rejected" ? Colors.White : Colors.Black;
            }
        }

        private async void OnRefreshClicked(object sender, EventArgs e)
        {
            await LoadRequestsAsync();
        }

        private async Task LoadRequestsAsync()
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

                var endpoint = _currentFilter != null 
                    ? $"admin/borrow-requests?status={_currentFilter}" 
                    : "admin/borrow-requests";
                
                var response = await _apiService.GetAsync<JsonElement>(endpoint);
                
                Requests.Clear();

                if (response.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in response.EnumerateArray())
                    {
                        var requestId = item.GetProperty("id").GetInt32();
                        var reader = item.GetProperty("reader");
                        var readerName = reader.GetProperty("fullName").GetString() ?? "";
                        var readerEmail = reader.GetProperty("email").GetString() ?? "";
                        var bookIds = item.GetProperty("bookIds").EnumerateArray().Select(e => e.GetInt32()).ToList();
                        var status = item.GetProperty("status").GetString() ?? "";
                        var dueDate = item.GetProperty("calculatedDueDate").GetDateTime();
                        var createdAt = item.GetProperty("createdAt").GetDateTime();
                        var rejectionReason = item.TryGetProperty("rejectionReason", out var reasonElement) 
                            ? reasonElement.GetString() 
                            : null;
                        
                        // Get book titles from response (already included)
                        string bookTitlesText = "";
                        if (item.TryGetProperty("bookTitles", out var bookTitlesElement))
                        {
                            bookTitlesText = bookTitlesElement.GetString() ?? "";
                        }
                        
                        // Fallback: try to get from Books array
                        if (string.IsNullOrWhiteSpace(bookTitlesText) && item.TryGetProperty("books", out var booksElement))
                        {
                            var bookTitles = new List<string>();
                            foreach (var bookElement in booksElement.EnumerateArray())
                            {
                                if (bookElement.TryGetProperty("title", out var titleElement))
                                {
                                    bookTitles.Add(titleElement.GetString() ?? "");
                                }
                            }
                            bookTitlesText = string.Join(", ", bookTitles);
                        }
                        
                        // Final fallback: use book IDs
                        if (string.IsNullOrWhiteSpace(bookTitlesText))
                        {
                            bookTitlesText = string.Join(", ", bookIds.Select(id => $"Sách ID: {id}"));
                        }

                        // Map status to Vietnamese
                        var statusText = status switch
                        {
                            "Pending" => "Chờ duyệt",
                            "Approved" => "Đã duyệt",
                            "Rejected" => "Đã từ chối",
                            _ => status
                        };

                        Requests.Add(new BorrowRequestDisplay
                        {
                            RequestId = requestId,
                            ReaderName = readerName,
                            ReaderEmail = readerEmail,
                            BookTitles = bookTitlesText,
                            DueDate = dueDate,
                            Status = statusText,
                            CreatedAt = createdAt,
                            CanApprove = status == "Pending",
                            CanReject = status == "Pending",
                            StatusColor = status == "Pending" ? Colors.Orange : 
                                         status == "Approved" ? Colors.Green : Colors.Red,
                            RejectionReason = rejectionReason
                        });
                    }
                }

                if (EmptyLabel != null && Requests.Count == 0)
                {
                    EmptyLabel.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Lỗi", $"Không thể tải danh sách yêu cầu mượn sách: {ex.Message}", "OK");
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

        private async void OnViewDetailsClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is BorrowRequestDisplay request)
            {
                // Show details in alert
                var details = $"Độc giả: {request.ReaderName}\n" +
                             $"Email: {request.ReaderEmail}\n" +
                             $"Sách: {request.BookTitles}\n" +
                             $"Hạn trả: {request.DueDate:dd/MM/yyyy}\n" +
                             $"Trạng thái: {request.Status}\n" +
                             $"Ngày yêu cầu: {request.CreatedAt:dd/MM/yyyy HH:mm}";
                
                if (!string.IsNullOrWhiteSpace(request.RejectionReason))
                {
                    details += $"\nLý do từ chối: {request.RejectionReason}";
                }
                
                await DisplayAlertAsync("Chi tiết yêu cầu", details, "OK");
            }
        }

        private async void OnApproveClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is BorrowRequestDisplay request)
            {
                var confirm = await DisplayAlertAsync("Xác nhận", 
                    $"Bạn có muốn duyệt yêu cầu mượn sách của {request.ReaderName}?", 
                    "Có", "Không");
                
                if (!confirm) return;

                try
                {
                    if (LoadingIndicator != null)
                    {
                        LoadingIndicator.IsRunning = true;
                        LoadingIndicator.IsVisible = true;
                    }

                    await _apiService.PostAsync<object, object>($"admin/borrow-requests/{request.RequestId}/approve", new { });
                    
                    await DisplayAlertAsync("Thành công", "Yêu cầu mượn sách đã được duyệt. Email đã được gửi đến độc giả.", "OK");
                    await LoadRequestsAsync();
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync("Lỗi", $"Không thể duyệt yêu cầu: {ex.Message}", "OK");
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

        private async void OnRejectClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is BorrowRequestDisplay request)
            {
                var reason = await DisplayPromptAsync("Từ chối yêu cầu", 
                    "Nhập lý do từ chối (tùy chọn):", 
                    "Từ chối", "Hủy", 
                    placeholder: "Lý do từ chối");
                
                if (reason == null) return; // User cancelled

                try
                {
                    if (LoadingIndicator != null)
                    {
                        LoadingIndicator.IsRunning = true;
                        LoadingIndicator.IsVisible = true;
                    }

                    var rejectDto = new { reason = reason };
                    await _apiService.PostAsync<object, object>($"admin/borrow-requests/{request.RequestId}/reject", rejectDto);
                    
                    await DisplayAlertAsync("Thành công", "Yêu cầu mượn sách đã bị từ chối. Email đã được gửi đến độc giả.", "OK");
                    await LoadRequestsAsync();
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync("Lỗi", $"Không thể từ chối yêu cầu: {ex.Message}", "OK");
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

    public class BorrowRequestDisplay
    {
        public int RequestId { get; set; }
        public string ReaderName { get; set; } = string.Empty;
        public string ReaderEmail { get; set; } = string.Empty;
        public string BookTitles { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool CanApprove { get; set; }
        public bool CanReject { get; set; }
        public Color StatusColor { get; set; }
        public string? RejectionReason { get; set; }
        public bool HasRejectionReason => !string.IsNullOrWhiteSpace(RejectionReason);
    }
}

