using book.Models;
using book.Services;
using Microsoft.Maui.ApplicationModel;
using System.Text.Json;
using Microsoft.Maui.Media;

namespace book.Pages.Reader
{
    public partial class BookDetailPage : ContentPage
    {
        private readonly ApiService _apiService;
        private Book? _book;
        private int? _bookId;
        private CancellationTokenSource? _cts;

        public BookDetailPage(ApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;

            if (LoanDaysEntry != null)
            {
                LoanDaysEntry.TextChanged += OnLoanDaysChanged;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Get book ID from navigation data
            _bookId = NavigationDataService.GetData<int?>("BookId");
            if (_bookId.HasValue)
            {
                LoadBookAsync(_bookId.Value);
            }
            else
            {
                await DisplayAlertAsync("Lỗi", "Không tìm thấy ID sách", "OK");
                await Shell.Current.GoToAsync("reader/home");
            }

            // Clear the data after reading it
            NavigationDataService.ClearData("BookId");
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

        private void OnLoanDaysChanged(object? sender, TextChangedEventArgs e)
        {
            UpdateDueDate();
        }

        private void UpdateDueDate()
        {
            if (LoanDaysEntry != null && DueDateLabel != null)
            {
                if (int.TryParse(LoanDaysEntry.Text, out int days) && days > 0)
                {
                    // Đây là thời gian dự kiến trả sách SAU KHI đã đến lấy
                    var dueDate = DateTime.Now.AddDays(days);
                    DueDateLabel.Text = $"Thời gian mượn: {days} ngày (tính từ ngày đến lấy sách)";
                }
                else if (string.IsNullOrWhiteSpace(LoanDaysEntry.Text))
                {
                    DueDateLabel.Text = "Thời gian mượn mặc định: 14 ngày";
                }
                else
                {
                    DueDateLabel.Text = "";
                }
            }
        }

        private async void LoadBookAsync(int bookId)
        {
            try
            {
                if (LoadingIndicator != null)
                {
                    LoadingIndicator.IsRunning = true;
                    LoadingIndicator.IsVisible = true;
                }
                if (BookFrame != null) BookFrame.IsVisible = false;
                if (BorrowFrame != null) BorrowFrame.IsVisible = false;

                _book = await _apiService.GetAsync<Book>($"Books/{bookId}");

                if (_book != null)
                {
                    DisplayBookDetails();
                    UpdateDueDate();
                    await CheckReaderCardAsync();
                }
                else
                {
                    await DisplayAlertAsync("Lỗi", "Không tìm thấy sách", "OK");
                    await Shell.Current.GoToAsync("reader/home");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Lỗi", $"Không thể tải sách: {ex.Message}", "OK");
                await Shell.Current.GoToAsync("reader/home");
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

        private async Task CheckReaderCardAsync()
        {
            // Always enable button if book is available
            // API will auto-create ReaderCard when user submits borrow request
            if (RequestBorrowButton != null && _book != null)
            {
                bool canBorrow = _book.AvailableQuantity > 0;
                RequestBorrowButton.IsEnabled = canBorrow;

                // Update button appearance
                if (canBorrow)
                {
                    RequestBorrowButton.BackgroundColor = Color.FromArgb("#6C63FF"); // Màu tím Booking
                    RequestBorrowButton.TextColor = Colors.White;
                }
                else
                {
                    RequestBorrowButton.BackgroundColor = Color.FromArgb("#FF9E9E9E");
                    RequestBorrowButton.TextColor = Color.FromArgb("#FFE0E0E0");
                }

                if (_book.AvailableQuantity <= 0)
                {
                    if (BorrowStatusLabel != null)
                    {
                        BorrowStatusLabel.Text = "⚠️ Sách này hiện đã hết hàng trong kho.";
                        BorrowStatusLabel.TextColor = Colors.Orange;
                    }
                    if (BorrowStatusFrame != null)
                    {
                        BorrowStatusFrame.IsVisible = true;
                    }
                }
                else
                {
                    // Try to check reader card, but don't block if it fails
                    try
                    {
                        var profile = await _apiService.GetAsync<JsonElement>("reader/profile");

                        if (profile.ValueKind == JsonValueKind.Object && profile.TryGetProperty("ReaderCard", out var readerCard))
                        {
                            // User has reader card, hide status
                            if (BorrowStatusLabel != null)
                            {
                                BorrowStatusLabel.Text = "";
                            }
                            if (BorrowStatusFrame != null)
                            {
                                BorrowStatusFrame.IsVisible = false;
                            }
                        }
                        else
                        {
                            // No reader card, but allow borrowing (will be created automatically)
                            if (BorrowStatusLabel != null)
                            {
                                BorrowStatusLabel.Text = "💡 Thẻ độc giả sẽ được tạo tự động khi bạn đặt lịch hẹn.";
                                BorrowStatusLabel.TextColor = Colors.Blue;
                            }
                            if (BorrowStatusFrame != null)
                            {
                                BorrowStatusFrame.IsVisible = true;
                            }
                        }
                    }
                    catch
                    {
                        // If check fails, still allow borrowing - API will handle it
                        if (BorrowStatusLabel != null)
                        {
                            BorrowStatusLabel.Text = "💡 Bạn có thể đặt hẹn. Hệ thống sẽ tự động xử lý hồ sơ.";
                            BorrowStatusLabel.TextColor = Colors.Blue;
                        }
                        if (BorrowStatusFrame != null)
                        {
                            BorrowStatusFrame.IsVisible = true;
                        }
                    }
                }
            }
        }

        private void DisplayBookDetails()
        {
            if (_book == null) return;

            if (TitleLabel != null) TitleLabel.Text = _book.Title;

            if (AuthorsLabel != null)
            {
                var authorsText = _book.Authors != null && _book.Authors.Any()
                    ? string.Join(", ", _book.Authors)
                    : "No authors";
                AuthorsLabel.Text = $"Tác giả: {authorsText}";
            }

            if (ManagementCodeLabel != null)
                ManagementCodeLabel.Text = $"Mã sách: {_book.ManagementCode}";

            if (CategoryLabel != null)
                CategoryLabel.Text = $"Thể loại: {_book.Category ?? "N/A"}";

            if (PublishedYearLabel != null)
                PublishedYearLabel.Text = $"Năm XB: {_book.PublishedYear?.ToString() ?? "N/A"}";

            if (AvailableQuantityLabel != null)
            {
                AvailableQuantityLabel.Text = $"Sẵn có: {_book.AvailableQuantity} / {_book.TotalQuantity}";
                AvailableQuantityLabel.TextColor = _book.AvailableQuantity > 0 ? Colors.Green : Colors.Red;
            }

            if (DescriptionLabel != null)
            {
                DescriptionLabel.Text = _book.Description ?? "Chưa có mô tả";
                DescriptionLabel.IsVisible = !string.IsNullOrWhiteSpace(_book.Description);

                if (ReadDescriptionButton != null)
                {
                    ReadDescriptionButton.IsVisible = !string.IsNullOrWhiteSpace(_book.Description);
                }
            }

            // Hiển thị ảnh bìa sách
            if (CoverImage != null)
            {
                if (!string.IsNullOrWhiteSpace(_book.CoverImageUrl))
                {
                    CoverImage.Source = ImageSource.FromUri(new Uri(_book.CoverImageUrl));
                    CoverImage.IsVisible = true;
                }
                else
                {
                    CoverImage.IsVisible = false;
                }
            }

            if (BookFrame != null) BookFrame.IsVisible = true;

            // Show borrow frame
            if (BorrowFrame != null)
            {
                BorrowFrame.IsVisible = true;
                // Button will be enabled/disabled after checking reader card
                if (RequestBorrowButton != null)
                {
                    RequestBorrowButton.IsEnabled = false;
                    RequestBorrowButton.BackgroundColor = Color.FromArgb("#FF9E9E9E");
                    RequestBorrowButton.TextColor = Color.FromArgb("#FFE0E0E0");
                }
            }
        }

        private async void OnReadDescriptionClicked(object sender, EventArgs e)
        {
            if (_book == null || string.IsNullOrWhiteSpace(_book.Description))
            {
                await DisplayAlertAsync("Thông báo", "Không có nội dung mô tả để đọc.", "OK");
                return;
            }

            if (_cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
                _cts = null;
                ReadDescriptionButton.Text = "🔊 Đọc";
                return;
            }

            _cts = new CancellationTokenSource();
            ReadDescriptionButton.Text = "⏹️ Dừng";

            try
            {
                var locales = await TextToSpeech.Default.GetLocalesAsync();
                var vnLocale = locales.FirstOrDefault(l => l.Language == "vi");

                var settings = new SpeechOptions()
                {
                    Volume = 1.0f,
                    Pitch = 1.0f,
                    Locale = vnLocale
                };

                await TextToSpeech.Default.SpeakAsync(_book.Description, settings, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Đã bấm hủy, không làm gì cả
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Lỗi", $"Không thể đọc: {ex.Message}", "OK");
            }
            finally
            {
                ReadDescriptionButton.Text = "🔊 Đọc";
                _cts = null;
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
        }

        // --- PHẦN LOGIC QUAN TRỌNG ĐÃ ĐƯỢC SỬA ĐỔI ---
        private async void OnRequestBorrowClicked(object sender, EventArgs e)
        {
            // Kiểm tra trạng thái nút
            if (RequestBorrowButton != null && !RequestBorrowButton.IsEnabled)
            {
                await DisplayAlertAsync("Thông báo", "Nút này hiện không khả dụng. Vui lòng kiểm tra thông báo bên dưới.", "OK");
                return;
            }

            if (_book == null || _bookId == null)
            {
                await DisplayAlertAsync("Lỗi", "Thông tin sách không khả dụng", "OK");
                return;
            }

            if (_book.AvailableQuantity <= 0)
            {
                await DisplayAlertAsync("Hết sách", "Rất tiếc, cuốn sách này hiện đã được đặt hết.", "OK");
                return;
            }

            // --- BƯỚC 1: XÁC NHẬN HẸN (CONFIRMATION) ---
            bool confirm = await DisplayAlert("Xác nhận đặt hẹn",
                $"Bạn có chắc chắn muốn đặt lịch hẹn lấy cuốn sách '{_book.Title}' không?\n\n" +
                "⚠️ LƯU Ý QUAN TRỌNG:\n" +
                "- Sách sẽ được giữ cho bạn trong vòng 3 ngày sau khi Admin duyệt.\n" +
                "- Vui lòng đến thư viện nhận sách đúng hạn, nếu không yêu cầu sẽ bị hủy.",
                "Đồng ý hẹn", "Hủy bỏ");

            if (!confirm) return;

            // Disable button immediately to prevent double-click
            if (RequestBorrowButton != null)
            {
                RequestBorrowButton.IsEnabled = false;
            }

            try
            {
                if (LoadingIndicator != null)
                {
                    LoadingIndicator.IsRunning = true;
                    LoadingIndicator.IsVisible = true;
                }

                var loanDays = int.TryParse(LoanDaysEntry?.Text, out int days) ? days : 14;

                // Validate loan days
                if (loanDays <= 0 || loanDays > 365)
                {
                    await DisplayAlertAsync("Lỗi", "Số ngày dự kiến mượn phải từ 1 đến 365 ngày", "OK");
                    if (RequestBorrowButton != null) RequestBorrowButton.IsEnabled = true; // Enable lại nếu lỗi
                    return;
                }

                var request = new ReaderBorrowRequest
                {
                    BookIds = new List<int> { _bookId.Value },
                    LoanDays = loanDays,
                    CustomDueDate = null
                };

                // Gọi API gửi yêu cầu (Vẫn dùng API cũ nhưng logic Backend đã đổi thành Booking)
                var response = await _apiService.PostAsync<ReaderBorrowRequest, dynamic>("reader/borrow/request", request);

                var jsonElement = JsonSerializer.SerializeToElement(response);
                if (jsonElement.ValueKind != JsonValueKind.Null && jsonElement.ValueKind != JsonValueKind.Undefined)
                {
                    string message = "Gửi yêu cầu hẹn thành công!";
                    if (jsonElement.TryGetProperty("message", out JsonElement messageElement))
                    {
                        message = messageElement.GetString() ?? message;
                    }

                    if (BorrowStatusLabel != null)
                    {
                        // --- CẬP NHẬT THÔNG BÁO CHO ĐÚNG NGHIỆP VỤ ---
                        BorrowStatusLabel.Text = "✓ Đã gửi yêu cầu hẹn! Vui lòng chờ Admin duyệt và kiểm tra Email để biết ngày giờ nhận sách.";
                        BorrowStatusLabel.TextColor = Colors.Green;
                    }
                    if (BorrowStatusFrame != null)
                    {
                        BorrowStatusFrame.IsVisible = true;
                    }

                    await DisplayAlertAsync("Đã gửi yêu cầu",
                        "Yêu cầu hẹn mượn sách đã được gửi.\n\nVui lòng chờ Admin duyệt. Bạn sẽ nhận được Email xác nhận lịch hẹn.",
                        "OK");

                    // Nút vẫn bị disable sau khi thành công
                    if (RequestBorrowButton != null)
                    {
                        RequestBorrowButton.IsEnabled = false;
                        RequestBorrowButton.BackgroundColor = Color.FromArgb("#FF9E9E9E");
                        RequestBorrowButton.Text = "ĐÃ GỬI YÊU CẦU";
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = "Không thể gửi yêu cầu hẹn";

                var exceptionMessage = ex.Message;

                // Xử lý các lỗi thường gặp và việt hóa lại cho đúng ngữ cảnh "Hẹn"
                if (exceptionMessage.Contains("BadRequest") || exceptionMessage.Contains("400"))
                {
                    if (exceptionMessage.Contains("không còn sẵn") || exceptionMessage.Contains("not available"))
                        errorMessage = "Sách này vừa có người đặt trước, hiện không còn sẵn.";
                    else if (exceptionMessage.Contains("đã có yêu cầu") || exceptionMessage.Contains("already"))
                        errorMessage = "Bạn đã có yêu cầu hẹn mượn sách này rồi.";
                    else if (exceptionMessage.Contains("đang mượn") || exceptionMessage.Contains("already borrowed"))
                        errorMessage = "Bạn đang giữ cuốn sách này, không thể đặt hẹn thêm.";
                }
                else if (exceptionMessage.Contains("Connection"))
                {
                    errorMessage = "Lỗi kết nối. Vui lòng kiểm tra mạng.";
                }

                await DisplayAlertAsync("Lỗi", errorMessage, "OK");

                if (BorrowStatusLabel != null)
                {
                    BorrowStatusLabel.Text = $"❌ {errorMessage}";
                    BorrowStatusLabel.TextColor = Colors.Red;
                }
                if (BorrowStatusFrame != null)
                {
                    BorrowStatusFrame.IsVisible = true;
                }

                // Enable lại nút để thử lại
                if (RequestBorrowButton != null)
                {
                    RequestBorrowButton.IsEnabled = true;
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

        // Helper method to display alerts safely on main thread
        private Task DisplayAlertAsync(string title, string message, string cancel)
        {
            return MainThread.InvokeOnMainThreadAsync(() => DisplayAlert(title, message, cancel));
        }
    }
}