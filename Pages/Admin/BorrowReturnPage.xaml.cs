using book.Models;
using book.Services;
using System.Collections.ObjectModel;
using Microsoft.Maui.ApplicationModel;
using System.Text.RegularExpressions;

namespace book.Pages.Admin
{
    public partial class BorrowReturnPage : ContentPage
    {
        private readonly ApiService _apiService;
        private ReaderCardDto? _currentReader;
        private int? _currentVerificationCodeId;
        private int? _currentReturnVerificationCodeId;

        public ObservableCollection<BookSelection> AvailableBooks { get; } = new();
        public ObservableCollection<LoanItemSelection> CurrentLoans { get; } = new();

        public BorrowReturnPage(ApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;
            BooksCollection.ItemsSource = AvailableBooks;
            LoansCollection.ItemsSource = CurrentLoans;
            LoadAvailableBooksAsync();
            
            // Set default minimum date for DatePicker
            if (DueDatePicker != null)
            {
                DueDatePicker.MinimumDate = DateTime.Now;
                DueDatePicker.DateSelected += OnDueDateSelected;
            }
            
            // Update calculated due date when loan days change
            if (LoanDaysEntry != null)
            {
                LoanDaysEntry.TextChanged += OnLoanDaysChanged;
            }
            
            UpdateCalculatedDueDate();
        }
        
        private void OnDueDateSelected(object? sender, DateChangedEventArgs e)
        {
            if (DueDatePicker != null && DueDatePicker.Date != null)
            {
                if (CalculatedDueDateLabel != null)
                {
                    CalculatedDueDateLabel.Text = $"Ngày hết hạn đã chọn: {DueDatePicker.Date:yyyy-MM-dd}";
                    CalculatedDueDateLabel.TextColor = Colors.Blue;
                }
            }
        }
        
        private void OnLoanDaysChanged(object? sender, TextChangedEventArgs e)
        {
            UpdateCalculatedDueDate();
        }
        
        private void UpdateCalculatedDueDate()
        {
            if (LoanDaysEntry != null && CalculatedDueDateLabel != null)
            {
                if (int.TryParse(LoanDaysEntry.Text, out int days) && days > 0)
                {
                    var dueDate = DateTime.Now.AddDays(days);
                    CalculatedDueDateLabel.Text = $"Hạn trả sẽ là: {dueDate:yyyy-MM-dd} (sau {days} ngày)";
                    CalculatedDueDateLabel.TextColor = Colors.Gray;
                }
                else if (string.IsNullOrWhiteSpace(LoanDaysEntry.Text))
                {
                    CalculatedDueDateLabel.Text = "Hạn trả sẽ là: 14 ngày kể từ bây giờ (mặc định)";
                    CalculatedDueDateLabel.TextColor = Colors.Gray;
                }
                else
                {
                    CalculatedDueDateLabel.Text = "";
                }
            }
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

        private async void LoadAvailableBooksAsync()
        {
            try
            {
                var response = await _apiService.GetAsync<BooksResponse>("Books");
                if (response != null && response.Data != null)
                {
                    AvailableBooks.Clear();
                    foreach (var book in response.Data)
                    {
                        if (book.AvailableQuantity > 0)
                        {
                            AvailableBooks.Add(new BookSelection { Book = book, IsSelected = false });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Lỗi", $"Không thể tải sách: {ex.Message}", "OK");
            }
        }

        private async void OnLookupReaderClicked(object sender, EventArgs e)
        {
            var cardCode = CardCodeEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(cardCode))
            {
                await DisplayAlertAsync("Lỗi", "Vui lòng nhập mã thẻ độc giả", "OK");
                return;
            }

            try
            {
                LoadingIndicator.IsRunning = true;
                LoadingIndicator.IsVisible = true;

                _currentReader = await _apiService.GetAsync<ReaderCardDto>($"admin/readers/{Uri.EscapeDataString(cardCode)}");
                if (_currentReader != null)
                {
                    ReaderInfoLabel.Text = $"Tên: {_currentReader.FullName}";
                    ReaderEmailLabel.Text = $"Email: {_currentReader.Email}";
                    ReaderPhoneLabel.Text = $"SĐT: {_currentReader.Phone ?? "N/A"}";
                    ReaderInfoFrame.IsVisible = true;

                    // Load current loans for return
                    CurrentLoans.Clear();
                    foreach (var loan in _currentReader.LoanHistory)
                    {
                        foreach (var item in loan.Items.Where(i => i.Status != "Returned"))
                        {
                            CurrentLoans.Add(new LoanItemSelection
                            {
                                LoanItemId = item.Id,
                                BookTitle = item.BookTitle,
                                ManagementCode = item.ManagementCode,
                                Status = item.Status,
                                IsSelected = false
                            });
                        }
                    }
                    
                    // Show success message if no active loans
                    if (!CurrentLoans.Any())
                    {
                        System.Diagnostics.Debug.WriteLine("Reader found but has no active loans");
                    }
                }
            }
            catch (Exception ex)
            {
                // Parse error message from API response
                string errorMessage = "Không tìm thấy thẻ độc giả";
                
                // Extract the actual error message from exception
                var exceptionMessage = ex.Message;
                
                if (exceptionMessage.Contains("NotFound") || exceptionMessage.Contains("not found"))
                {
                    errorMessage = $"Không tìm thấy thẻ độc giả '{cardCode}'.\n\nVui lòng kiểm tra mã thẻ và thử lại.\n\nLưu ý: Mã thẻ thường có định dạng như 'RC001001', 'RC001002', v.v.";
                }
                else if (exceptionMessage.Contains("Unauthorized") || exceptionMessage.Contains("401"))
                {
                    errorMessage = "Xác thực thất bại. Vui lòng đăng nhập lại.";
                }
                else if (exceptionMessage.Contains("Reader card not found"))
                {
                    errorMessage = $"Không tìm thấy thẻ độc giả '{cardCode}'.\n\nVui lòng xác minh mã thẻ đúng.\n\nVí dụ mã thẻ: RC001001, RC001002";
                }
                else
                {
                    // Try to extract message from exception
                    var match = Regex.Match(exceptionMessage, @"Reader card not found|not found|message[""']?\s*:\s*[""']?([^""']+)");
                    if (match.Success)
                    {
                        errorMessage = match.Groups.Count > 1 && !string.IsNullOrEmpty(match.Groups[1].Value) 
                            ? match.Groups[1].Value 
                            : $"Không tìm thấy thẻ độc giả '{cardCode}'. Vui lòng kiểm tra mã thẻ.";
                    }
                    else
                    {
                        errorMessage = $"Không thể tra cứu độc giả: {exceptionMessage.Split('\n')[0]}";
                    }
                }
                
                await DisplayAlertAsync("Lỗi", errorMessage, "OK");
                ReaderInfoFrame.IsVisible = false;
                CurrentLoans.Clear();
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private async void OnBorrowClicked(object sender, EventArgs e)
        {
            if (_currentReader == null)
            {
                await DisplayAlertAsync("Error", "Vui lòng tra cứu độc giả trước", "OK");
                return;
            }

            var selectedBooks = AvailableBooks.Where(bs => bs.IsSelected).ToList();
            if (!selectedBooks.Any())
            {
                await DisplayAlertAsync("Error", "Vui lòng chọn ít nhất một cuốn sách", "OK");
                return;
            }

            try
            {
                LoadingIndicator.IsRunning = true;
                LoadingIndicator.IsVisible = true;

                var loanDays = int.TryParse(LoanDaysEntry?.Text, out int days) ? days : 14;
                DateTime? customDueDate = null;
                
                // If DatePicker has a date selected, use it instead of loan days
                if (DueDatePicker != null && DueDatePicker.Date.HasValue)
                {
                    var selectedDate = DueDatePicker.Date.Value;
                    if (selectedDate > DateTime.Now)
                    {
                        // Use UTC date at midnight to avoid timezone issues
                        customDueDate = new DateTime(selectedDate.Year, selectedDate.Month, selectedDate.Day, 0, 0, 0, DateTimeKind.Utc);
                        loanDays = 0; // Will be ignored when CustomDueDate is set
                    }
                }
                
                var request = new BorrowRequest
                {
                    ReaderCardCode = _currentReader.CardCode,
                    BookIds = selectedBooks.Select(bs => bs.Book.Id).ToList(),
                    LoanDays = loanDays,
                    CustomDueDate = customDueDate
                };

                // Call request API to send verification code
                var response = await _apiService.PostAsync<BorrowRequest, BorrowRequestResponse>("admin/borrow/request", request);
                
                if (response != null && response.VerificationCodeId > 0)
                {
                    _currentVerificationCodeId = response.VerificationCodeId;
                    
                    // Show verification UI
                    VerificationFrame.IsVisible = true;
                    VerificationCodeEntry.Text = "";
                    VerificationStatusLabel.Text = $"Mã xác minh đã được gửi đến email: {_currentReader.Email}";
                    VerificationStatusLabel.TextColor = Colors.Blue;
                    
                    await DisplayAlertAsync("Thành công", $"Mã xác minh 6 số đã được gửi đến email của độc giả: {_currentReader.Email}\n\nVui lòng nhập mã xác minh để hoàn tất giao dịch mượn sách.", "OK");
                }
                else
                {
                    await DisplayAlertAsync("Lỗi", "Không nhận được mã xác minh từ server", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Lỗi", $"Không thể gửi mã xác minh: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private async void OnConfirmBorrowClicked(object sender, EventArgs e)
        {
            if (!_currentVerificationCodeId.HasValue)
            {
                await DisplayAlertAsync("Lỗi", "Không có mã xác minh hợp lệ", "OK");
                return;
            }

            var code = VerificationCodeEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(code) || code.Length != 6)
            {
                VerificationStatusLabel.Text = "Mã xác minh phải có 6 số";
                VerificationStatusLabel.TextColor = Colors.Red;
                return;
            }

            try
            {
                LoadingIndicator.IsRunning = true;
                LoadingIndicator.IsVisible = true;

                var confirmRequest = new ConfirmBorrowRequest
                {
                    VerificationCodeId = _currentVerificationCodeId.Value,
                    Code = code
                };

                var response = await _apiService.PostAsync<ConfirmBorrowRequest, dynamic>("admin/borrow/confirm", confirmRequest);
                
                // Success - hide verification frame and reset form
                VerificationFrame.IsVisible = false;
                _currentVerificationCodeId = null;
                VerificationCodeEntry.Text = "";
                VerificationStatusLabel.Text = "";
                
                var loanDays = int.TryParse(LoanDaysEntry?.Text, out int days) ? days : 14;
                DateTime? customDueDate = null;
                if (DueDatePicker != null && DueDatePicker.Date.HasValue)
                {
                    var selectedDate = DueDatePicker.Date.Value;
                    if (selectedDate > DateTime.Now)
                    {
                        customDueDate = new DateTime(selectedDate.Year, selectedDate.Month, selectedDate.Day, 0, 0, 0, DateTimeKind.Utc);
                    }
                }
                
                var dueDateInfo = "";
                if (customDueDate.HasValue)
                {
                    dueDateInfo = $"Hạn trả: {customDueDate.Value:yyyy-MM-dd}";
                }
                else if (loanDays > 0)
                {
                    dueDateInfo = $"Hạn trả: {DateTime.Now.AddDays(loanDays):yyyy-MM-dd} (sau {loanDays} ngày)";
                }
                
                await DisplayAlertAsync("Thành công", $"Mượn sách thành công!\n\nEmail thông báo đã được gửi đến {_currentReader?.Email}.{(!string.IsNullOrEmpty(dueDateInfo) ? "\n" + dueDateInfo : "")}", "OK");
                
                // Reset form
                foreach (var book in AvailableBooks)
                {
                    book.IsSelected = false;
                }
                if (DueDatePicker != null) DueDatePicker.Date = DateTime.Now;
                if (LoanDaysEntry != null) LoanDaysEntry.Text = "14";
                UpdateCalculatedDueDate();
                LoadAvailableBooksAsync();
                OnLookupReaderClicked(sender, e);
            }
            catch (Exception ex)
            {
                VerificationStatusLabel.Text = $"Lỗi: {ex.Message}";
                VerificationStatusLabel.TextColor = Colors.Red;
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private void OnCancelVerificationClicked(object sender, EventArgs e)
        {
            VerificationFrame.IsVisible = false;
            _currentVerificationCodeId = null;
            VerificationCodeEntry.Text = "";
            VerificationStatusLabel.Text = "";
        }

        private async void OnReturnClicked(object sender, EventArgs e)
        {
            if (_currentReader == null)
            {
                await DisplayAlertAsync("Lỗi", "Vui lòng tra cứu độc giả trước", "OK");
                return;
            }

            var selectedLoans = CurrentLoans.Where(ls => ls.IsSelected).ToList();
            if (!selectedLoans.Any())
            {
                await DisplayAlertAsync("Lỗi", "Vui lòng chọn ít nhất một mục để trả", "OK");
                return;
            }

            try
            {
                LoadingIndicator.IsRunning = true;
                LoadingIndicator.IsVisible = true;

                var request = new ReturnRequest
                {
                    ReaderCardCode = _currentReader.CardCode,
                    LoanItemIds = selectedLoans.Select(ls => ls.LoanItemId).ToList()
                };

                // Call request API to send verification code
                var response = await _apiService.PostAsync<ReturnRequest, ReturnRequestResponse>("admin/return/request", request);
                
                if (response != null && response.VerificationCodeId > 0)
                {
                    _currentReturnVerificationCodeId = response.VerificationCodeId;
                    
                    // Show verification UI
                    ReturnVerificationFrame.IsVisible = true;
                    ReturnVerificationCodeEntry.Text = "";
                    ReturnVerificationStatusLabel.Text = $"Mã xác minh đã được gửi đến email: {_currentReader.Email}";
                    ReturnVerificationStatusLabel.TextColor = Colors.Blue;
                    
                    await DisplayAlertAsync("Thành công", $"Mã xác minh 6 số đã được gửi đến email của độc giả: {_currentReader.Email}\n\nVui lòng nhập mã xác minh để hoàn tất giao dịch trả sách.", "OK");
                }
                else
                {
                    await DisplayAlertAsync("Lỗi", "Không nhận được mã xác minh từ server", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Lỗi", $"Không thể gửi mã xác minh: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private async void OnConfirmReturnClicked(object sender, EventArgs e)
        {
            if (!_currentReturnVerificationCodeId.HasValue)
            {
                await DisplayAlertAsync("Lỗi", "Không có mã xác minh hợp lệ", "OK");
                return;
            }

            var code = ReturnVerificationCodeEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(code) || code.Length != 6)
            {
                ReturnVerificationStatusLabel.Text = "Mã xác minh phải có 6 số";
                ReturnVerificationStatusLabel.TextColor = Colors.Red;
                return;
            }

            try
            {
                LoadingIndicator.IsRunning = true;
                LoadingIndicator.IsVisible = true;

                var confirmRequest = new ConfirmReturnRequest
                {
                    VerificationCodeId = _currentReturnVerificationCodeId.Value,
                    Code = code
                };

                var response = await _apiService.PostAsync<ConfirmReturnRequest, dynamic>("admin/return/confirm", confirmRequest);
                
                // Success - hide verification frame and reset form
                ReturnVerificationFrame.IsVisible = false;
                _currentReturnVerificationCodeId = null;
                ReturnVerificationCodeEntry.Text = "";
                ReturnVerificationStatusLabel.Text = "";
                
                await DisplayAlertAsync("Thành công", $"Trả sách thành công!\n\nEmail thông báo đã được gửi đến {_currentReader?.Email}.", "OK");
                
                // Reset form
                foreach (var loan in CurrentLoans)
                {
                    loan.IsSelected = false;
                }
                LoadAvailableBooksAsync();
                OnLookupReaderClicked(sender, e);
            }
            catch (Exception ex)
            {
                ReturnVerificationStatusLabel.Text = $"Lỗi: {ex.Message}";
                ReturnVerificationStatusLabel.TextColor = Colors.Red;
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private void OnCancelReturnVerificationClicked(object sender, EventArgs e)
        {
            ReturnVerificationFrame.IsVisible = false;
            _currentReturnVerificationCodeId = null;
            ReturnVerificationCodeEntry.Text = "";
            ReturnVerificationStatusLabel.Text = "";
        }

    }

    public class LoanItemSelection
    {
        public int LoanItemId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string ManagementCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }
}

