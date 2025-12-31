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

                        string bookTitlesText = "";
                        if (item.TryGetProperty("bookTitles", out var bookTitlesElement))
                        {
                            bookTitlesText = bookTitlesElement.GetString() ?? "";
                        }

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

                        if (string.IsNullOrWhiteSpace(bookTitlesText))
                        {
                            bookTitlesText = string.Join(", ", bookIds.Select(id => $"Sách ID: {id}"));
                        }

                        // Map status
                        var statusText = status;
                        Color statusColor = Colors.Gray;

                        switch (status)
                        {
                            case "Pending":
                                statusText = "Chờ duyệt";
                                statusColor = Colors.Orange;
                                break;
                            case "Approved":
                                statusText = "Chờ lấy sách";
                                statusColor = Color.FromArgb("#3B82F6");
                                break;
                            case "Rejected":
                                statusText = "Đã từ chối/Hủy";
                                statusColor = Colors.Red;
                                break;
                            case "PickedUp":
                                statusText = "Đã lấy sách";
                                statusColor = Colors.Green;
                                break;
                        }

                        Requests.Add(new BorrowRequestDisplay
                        {
                            RequestId = requestId,
                            ReaderName = readerName,
                            ReaderEmail = readerEmail,
                            BookTitles = bookTitlesText,
                            DueDate = dueDate,
                            Status = status,
                            StatusText = statusText,
                            CreatedAt = createdAt,

                            // Logic hiển thị nút
                            IsPending = status == "Pending",
                            IsApproved = status == "Approved",

                            StatusColor = statusColor,
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
                await DisplayAlertAsync("Lỗi", $"Không thể tải danh sách: {ex.Message}", "OK");
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
                var details = $"Độc giả: {request.ReaderName}\n" +
                              $"Email: {request.ReaderEmail}\n" +
                              $"Sách: {request.BookTitles}\n" +
                              $"Hạn trả: {request.DueDate:dd/MM/yyyy}\n" +
                              $"Trạng thái: {request.StatusText}\n" +
                              $"Ngày yêu cầu: {request.CreatedAt:dd/MM/yyyy HH:mm}";

                if (!string.IsNullOrWhiteSpace(request.RejectionReason))
                {
                    details += $"\nLý do hủy/từ chối: {request.RejectionReason}";
                }

                await DisplayAlertAsync("Chi tiết yêu cầu", details, "OK");
            }
        }

        private async void OnApproveClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is BorrowRequestDisplay request)
            {
                var confirm = await DisplayAlertAsync("Xác nhận",
                    $"Duyệt lịch hẹn cho {request.ReaderName}?\n(Sách sẽ được giữ lại)",
                    "Có", "Không");

                if (!confirm) return;

                try
                {
                    if (LoadingIndicator != null) { LoadingIndicator.IsRunning = true; LoadingIndicator.IsVisible = true; }

                    await _apiService.PostAsync<object, object>($"admin/borrow-requests/{request.RequestId}/approve", new { });

                    await DisplayAlertAsync("Thành công", "Đã duyệt hẹn. Email xác nhận đã được gửi.", "OK");
                    await LoadRequestsAsync();
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync("Lỗi", $"Lỗi: {ex.Message}", "OK");
                }
                finally
                {
                    if (LoadingIndicator != null) { LoadingIndicator.IsRunning = false; LoadingIndicator.IsVisible = false; }
                }
            }
        }

        private async void OnRejectClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is BorrowRequestDisplay request)
            {
                var reason = await DisplayPromptAsync("Từ chối yêu cầu",
                    "Nhập lý do:", "Từ chối", "Hủy", placeholder: "Lý do");

                if (reason == null) return;

                try
                {
                    if (LoadingIndicator != null) { LoadingIndicator.IsRunning = true; LoadingIndicator.IsVisible = true; }

                    var rejectDto = new { reason = reason };
                    await _apiService.PostAsync<object, object>($"admin/borrow-requests/{request.RequestId}/reject", rejectDto);

                    await DisplayAlertAsync("Thành công", "Đã từ chối yêu cầu.", "OK");
                    await LoadRequestsAsync();
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync("Lỗi", $"Lỗi: {ex.Message}", "OK");
                }
                finally
                {
                    if (LoadingIndicator != null) { LoadingIndicator.IsRunning = false; LoadingIndicator.IsVisible = false; }
                }
            }
        }

        private async void OnPickupClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is BorrowRequestDisplay request)
            {
                // BƯỚC 1: HỎI XÁC NHẬN GỬI OTP
                bool confirm = await DisplayAlert("Xác nhận lấy sách",
                    $"Độc giả {request.ReaderName} đang nhận sách?\n\nHệ thống sẽ gửi mã OTP đến email {request.ReaderEmail} để xác minh.",
                    "Gửi OTP", "Hủy");

                if (!confirm) return;

                try
                {
                    if (LoadingIndicator != null)
                    {
                        LoadingIndicator.IsRunning = true;
                        LoadingIndicator.IsVisible = true;
                    }

                    // Gọi API gửi OTP
                    await _apiService.PostAsync<object, dynamic>($"admin/borrow-requests/{request.RequestId}/send-pickup-otp", new { });

                    if (LoadingIndicator != null)
                    {
                        LoadingIndicator.IsRunning = false;
                        LoadingIndicator.IsVisible = false;
                    }

                    // BƯỚC 2: HIỂN THỊ HỘP THOẠI NHẬP OTP
                    string otpInput = await DisplayPromptAsync("Nhập mã OTP",
                        $"Vui lòng nhập mã 6 số đã gửi đến {request.ReaderEmail}:",
                        "Xác nhận", "Hủy",
                        placeholder: "123456",
                        maxLength: 6,
                        keyboard: Keyboard.Numeric);

                    // Nếu bấm hủy hoặc không nhập
                    if (string.IsNullOrWhiteSpace(otpInput)) return;

                    // BƯỚC 3: GỌI API XÁC NHẬN OTP
                    if (LoadingIndicator != null)
                    {
                        LoadingIndicator.IsRunning = true;
                        LoadingIndicator.IsVisible = true;
                    }

                    var payload = new { otp = otpInput };
                    await _apiService.PostAsync<object, object>($"admin/borrow-requests/{request.RequestId}/pickup-confirm", payload);

                    await DisplayAlert("Thành công", "Mã OTP chính xác!\nĐã tạo phiếu mượn thành công.", "OK");
                    await LoadRequestsAsync(); // Tải lại danh sách
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync("Lỗi", $"Xác thực thất bại: {ex.Message}", "OK");
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

        // --- HÀM MỚI: HỦY HẸN (CANCEL BOOKING) ---
        private async void OnCancelBookingClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is BorrowRequestDisplay request)
            {
                bool confirm = await DisplayAlert("Hủy lịch hẹn",
                    $"Bạn có chắc chắn muốn hủy lịch hẹn của {request.ReaderName} không?\n\n(Sách sẽ được trả lại vào kho)",
                    "Hủy hẹn", "Quay lại");

                if (!confirm) return;

                try
                {
                    if (LoadingIndicator != null) { LoadingIndicator.IsRunning = true; LoadingIndicator.IsVisible = true; }

                    // Gọi API hủy hẹn mới
                    await _apiService.PostAsync<object, dynamic>($"admin/borrow-requests/{request.RequestId}/cancel-booking", new { });

                    await DisplayAlert("Thành công", "Đã hủy lịch hẹn và trả sách về kho!", "OK");
                    await LoadRequestsAsync();
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync("Lỗi", "Không thể hủy hẹn: " + ex.Message, "OK");
                }
                finally
                {
                    if (LoadingIndicator != null) { LoadingIndicator.IsRunning = false; LoadingIndicator.IsVisible = false; }
                }
            }
        }

        // Helper
        private Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel)
        {
            return MainThread.InvokeOnMainThreadAsync(() => DisplayAlert(title, message, accept, cancel));
        }

        private Task DisplayAlertAsync(string title, string message, string cancel)
        {
            return MainThread.InvokeOnMainThreadAsync(() => DisplayAlert(title, message, cancel));
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
        public string StatusText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public bool IsPending { get; set; }
        public bool IsApproved { get; set; }

        public Color StatusColor { get; set; }
        public string? RejectionReason { get; set; }
        public bool HasRejectionReason => !string.IsNullOrWhiteSpace(RejectionReason);
    }
}