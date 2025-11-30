using book.Models;
using book.Services;
using Microsoft.Maui.ApplicationModel;
using System.Text.Json;

namespace book.Pages.Reader
{
    public partial class BookDetailPage : ContentPage
    {
        private readonly ApiService _apiService;
        private Book? _book;
        private int? _bookId;

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
                    var dueDate = DateTime.Now.AddDays(days);
                    DueDateLabel.Text = $"Hạn trả sẽ là: {dueDate:yyyy-MM-dd} (sau {days} ngày)";
                }
                else if (string.IsNullOrWhiteSpace(LoanDaysEntry.Text))
                {
                    DueDateLabel.Text = "Hạn trả sẽ là: 14 ngày kể từ bây giờ (mặc định)";
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
                    RequestBorrowButton.BackgroundColor = Color.FromArgb("#FF4CAF50");
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
                        BorrowStatusLabel.Text = "⚠️ Sách này hiện không có sẵn để mượn.";
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
                                BorrowStatusLabel.Text = "💡 Thẻ độc giả sẽ được tạo tự động khi bạn gửi yêu cầu mượn sách.";
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
                            BorrowStatusLabel.Text = "💡 Bạn có thể mượn sách. Hệ thống sẽ tự động tạo thẻ độc giả nếu cần.";
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
                AuthorsLabel.Text = $"By: {authorsText}";
            }
            
            if (ManagementCodeLabel != null) 
                ManagementCodeLabel.Text = $"Code: {_book.ManagementCode}";
            
            if (CategoryLabel != null) 
                CategoryLabel.Text = $"Category: {_book.Category ?? "N/A"}";
            
            if (PublishedYearLabel != null) 
                PublishedYearLabel.Text = $"Published: {_book.PublishedYear?.ToString() ?? "N/A"}";
            
            if (AvailableQuantityLabel != null)
            {
                AvailableQuantityLabel.Text = $"Available: {_book.AvailableQuantity} / {_book.TotalQuantity}";
                AvailableQuantityLabel.TextColor = _book.AvailableQuantity > 0 ? Colors.Green : Colors.Red;
            }
            
            if (DescriptionLabel != null)
            {
                DescriptionLabel.Text = _book.Description ?? "No description available";
                DescriptionLabel.IsVisible = !string.IsNullOrWhiteSpace(_book.Description);
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
                    RequestBorrowButton.IsEnabled = false; // Will be enabled after reader card check
                    RequestBorrowButton.BackgroundColor = Color.FromArgb("#FF9E9E9E");
                    RequestBorrowButton.TextColor = Color.FromArgb("#FFE0E0E0");
                }
            }
        }

        private async void OnRequestBorrowClicked(object sender, EventArgs e)
        {
            // Check if button is enabled
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
                await DisplayAlertAsync("Lỗi", "Sách này không có sẵn để mượn", "OK");
                return;
            }

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
                    await DisplayAlertAsync("Lỗi", "Số ngày mượn phải từ 1 đến 365 ngày", "OK");
                    return;
                }
                
                var request = new ReaderBorrowRequest
                {
                    BookIds = new List<int> { _bookId.Value },
                    LoanDays = loanDays,
                    CustomDueDate = null
                };

                var response = await _apiService.PostAsync<ReaderBorrowRequest, dynamic>("reader/borrow/request", request);
                
                var jsonElement = JsonSerializer.SerializeToElement(response);
                if (jsonElement.ValueKind != JsonValueKind.Null && jsonElement.ValueKind != JsonValueKind.Undefined)
                {
                    string message = "Gửi yêu cầu mượn sách thành công!";
                    if (jsonElement.TryGetProperty("message", out JsonElement messageElement))
                    {
                        message = messageElement.GetString() ?? message;
                    }
                    
                    if (BorrowStatusLabel != null)
                    {
                        BorrowStatusLabel.Text = "✓ Đã gửi yêu cầu! Đang chờ admin phê duyệt. Bạn sẽ nhận được email khi được phê duyệt.";
                        BorrowStatusLabel.TextColor = Colors.Green;
                    }
                    if (BorrowStatusFrame != null)
                    {
                        BorrowStatusFrame.IsVisible = true;
                    }
                    
                    await DisplayAlertAsync("Thành công", message, "OK");
                    
                    // Disable button after successful request
                    if (RequestBorrowButton != null)
                    {
                        RequestBorrowButton.IsEnabled = false;
                        RequestBorrowButton.BackgroundColor = Color.FromArgb("#FF9E9E9E");
                        RequestBorrowButton.TextColor = Color.FromArgb("#FFE0E0E0");
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = "Không thể gửi yêu cầu mượn";
                
                // Parse error message from exception
                var exceptionMessage = ex.Message;
                
                // Try to extract detailed error message from API response
                if (exceptionMessage.Contains("BadRequest") || exceptionMessage.Contains("400"))
                {
                    // Extract message from error response
                    var errorParts = exceptionMessage.Split(new[] { "message" }, StringSplitOptions.None);
                    if (errorParts.Length > 1)
                    {
                        try
                        {
                            var messagePart = errorParts[1];
                            var startIndex = messagePart.IndexOf(':') + 1;
                            var endIndex = messagePart.IndexOf('\n');
                            if (endIndex == -1) endIndex = messagePart.Length;
                            if (startIndex > 0 && endIndex > startIndex)
                            {
                                errorMessage = messagePart.Substring(startIndex, endIndex - startIndex).Trim().Trim('"', '\'', '}');
                            }
                        }
                        catch
                        {
                            // Fall back to default parsing
                        }
                    }
                    
                    // Check for specific error types
                    if (exceptionMessage.Contains("không còn sẵn") || exceptionMessage.Contains("not available"))
                    {
                        errorMessage = "Sách này không còn sẵn để mượn.";
                    }
                    else if (exceptionMessage.Contains("đã có yêu cầu") || exceptionMessage.Contains("already"))
                    {
                        errorMessage = "Bạn đã có yêu cầu mượn sách này đang chờ xử lý.";
                    }
                    else if (exceptionMessage.Contains("đang mượn") || exceptionMessage.Contains("already borrowed"))
                    {
                        errorMessage = "Bạn đang mượn cuốn sách này. Vui lòng trả sách trước khi mượn lại.";
                    }
                    else if (exceptionMessage.Contains("Số ngày mượn") || exceptionMessage.Contains("LoanDays"))
                    {
                        errorMessage = "Số ngày mượn không hợp lệ. Vui lòng nhập từ 1 đến 365 ngày.";
                    }
                    else if (exceptionMessage.Contains("Ngày hết hạn") || exceptionMessage.Contains("due date"))
                    {
                        errorMessage = "Ngày hết hạn không hợp lệ.";
                    }
                    else if (string.IsNullOrWhiteSpace(errorMessage) || errorMessage == "Không thể gửi yêu cầu mượn")
                    {
                        errorMessage = "Yêu cầu không hợp lệ. Vui lòng kiểm tra thông tin và thử lại.";
                    }
                }
                else if (exceptionMessage.Contains("NotFound") || exceptionMessage.Contains("404"))
                {
                    errorMessage = "Không tìm thấy thông tin. Vui lòng thử lại sau.";
                }
                else if (exceptionMessage.Contains("Unauthorized") || exceptionMessage.Contains("401"))
                {
                    errorMessage = "Xác thực thất bại. Vui lòng đăng nhập lại.";
                }
                else if (exceptionMessage.Contains("Connection") || exceptionMessage.Contains("network") || exceptionMessage.Contains("refused"))
                {
                    errorMessage = "Không thể kết nối đến server. Vui lòng kiểm tra kết nối mạng.";
                }
                else
                {
                    // Use the first line of error message
                    var firstLine = exceptionMessage.Split('\n')[0];
                    if (firstLine.Length > 100)
                    {
                        errorMessage = firstLine.Substring(0, 100) + "...";
                    }
                    else
                    {
                        errorMessage = firstLine;
                    }
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

