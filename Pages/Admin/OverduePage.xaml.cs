using book.Services;
using System.Text.Json;
using Microsoft.Maui.ApplicationModel;
using System.Collections.ObjectModel;

namespace book.Pages.Admin
{
    public partial class OverduePage : ContentPage
    {
        private readonly ApiService _apiService;
        public ObservableCollection<OverdueItemDisplay> OverdueItems { get; } = new();

        public OverduePage(ApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;
            OverdueCollection.ItemsSource = OverdueItems;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Tự động load danh sách tất cả sách đang mượn khi vào trang
            LoadOverdueListAsync();
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

        private async void OnCheckOverdueClicked(object sender, EventArgs e)
        {
            try
            {
                LoadingIndicator.IsRunning = true;
                LoadingIndicator.IsVisible = true;
                ResultLabel.IsVisible = false;

                var response = await _apiService.PostAsync<object, dynamic>("admin/overdue/check-and-notify", new { });
                
                var jsonElement = JsonSerializer.SerializeToElement(response);
                if (jsonElement.ValueKind != JsonValueKind.Null && jsonElement.ValueKind != JsonValueKind.Undefined)
                {
                    var notificationsSent = 0;
                    var loansUpdated = 0;

                    if (jsonElement.TryGetProperty("notificationsSent", out JsonElement notifElement))
                    {
                        notificationsSent = notifElement.GetInt32();
                    }
                    if (jsonElement.TryGetProperty("loansUpdated", out JsonElement loansElement))
                    {
                        loansUpdated = loansElement.GetInt32();
                    }

                    ResultLabel.Text = $"Notifications sent: {notificationsSent}\nLoans updated: {loansUpdated}";
                    ResultLabel.IsVisible = true;
                    await DisplayAlertAsync("Thành công", $"Đã kiểm tra sách quá hạn. Đã gửi {notificationsSent} thông báo, đã cập nhật {loansUpdated} lượt mượn.", "OK");
                    
                    // Tự động refresh danh sách sau khi check
                    await LoadOverdueListAsync();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Lỗi", $"Không thể kiểm tra sách quá hạn: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private async void OnViewOverdueListClicked(object sender, EventArgs e)
        {
            await LoadOverdueListAsync();
        }

        private async Task LoadOverdueListAsync()
        {
            try
            {
                LoadingIndicator.IsRunning = true;
                LoadingIndicator.IsVisible = true;
                ResultLabel.IsVisible = false;
                OverdueCollection.IsVisible = false;
                EmptyLabel.IsVisible = false;

                var response = await _apiService.GetAsync<JsonElement>("admin/overdue/list");
                OverdueItems.Clear();

                // Debug: Kiểm tra response
                if (response.ValueKind == JsonValueKind.Null || response.ValueKind == JsonValueKind.Undefined)
                {
                    EmptyLabel.IsVisible = true;
                    CountLabel.IsVisible = false;
                    return;
                }

                if (response.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in response.EnumerateArray())
                    {
                        var readerCardId = item.TryGetProperty("readerCardId", out var rcid) ? rcid.GetInt32() : 0;
                        var readerName = item.TryGetProperty("readerName", out var rn) ? rn.GetString() ?? "" : "";
                        var readerCardCode = item.TryGetProperty("readerCardCode", out var rcc) ? rcc.GetString() ?? "" : "";
                        var readerEmail = item.TryGetProperty("readerEmail", out var re) ? re.GetString() ?? "" : "";
                        var readerPhone = item.TryGetProperty("readerPhone", out var rp) ? rp.GetString() : null;
                        var readerAddress = item.TryGetProperty("readerAddress", out var ra) ? ra.GetString() : null;
                        var bookTitle = item.TryGetProperty("bookTitle", out var bt) ? bt.GetString() ?? "" : "";
                        var bookManagementCode = item.TryGetProperty("bookManagementCode", out var bmc) ? bmc.GetString() ?? "" : "";
                        var borrowDate = item.TryGetProperty("borrowDate", out var bd) ? bd.GetDateTime() : DateTime.MinValue;
                        var dueDate = item.TryGetProperty("dueDate", out var dd) ? dd.GetDateTime() : DateTime.MinValue;
                        var daysOverdue = item.TryGetProperty("daysOverdue", out var do_) ? do_.GetInt32() : 0;
                        var daysRemaining = item.TryGetProperty("daysRemaining", out var dr) ? dr.GetInt32() : 0;

                        OverdueItems.Add(new OverdueItemDisplay
                        {
                            ReaderCardId = readerCardId,
                            ReaderName = readerName,
                            ReaderCardCode = readerCardCode,
                            ReaderEmail = readerEmail,
                            ReaderPhone = readerPhone,
                            ReaderAddress = readerAddress,
                            BookTitle = bookTitle,
                            BookManagementCode = bookManagementCode,
                            BorrowDate = borrowDate,
                            DueDate = dueDate,
                            DaysOverdue = daysOverdue,
                            DaysRemaining = daysRemaining
                        });
                    }
                }

                if (OverdueItems.Count == 0)
                {
                    EmptyLabel.IsVisible = true;
                    CountLabel.IsVisible = false;
                }
                else
                {
                    OverdueCollection.IsVisible = true;
                    CountLabel.Text = $"Danh sách sách đang mượn ({OverdueItems.Count} sách)";
                    CountLabel.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Lỗi", $"Không thể tải danh sách quá hạn: {ex.Message}", "OK");
                // Hiển thị empty label nếu có lỗi
                EmptyLabel.IsVisible = true;
                CountLabel.IsVisible = false;
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private async void OnSendEmailClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is OverdueItemDisplay item)
            {
                try
                {
                    LoadingIndicator.IsRunning = true;
                    LoadingIndicator.IsVisible = true;

                    var response = await _apiService.PostAsync<object, dynamic>(
                        $"admin/overdue/send-email/{item.ReaderCardId}", 
                        new { });

                    await DisplayAlertAsync("Thành công", $"Đã gửi email quá hạn cho {item.ReaderName}", "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync("Lỗi", $"Không thể gửi email: {ex.Message}", "OK");
                }
                finally
                {
                    LoadingIndicator.IsRunning = false;
                    LoadingIndicator.IsVisible = false;
                }
            }
        }
    }

    public class OverdueItemDisplay
    {
        public int ReaderCardId { get; set; }
        public string ReaderName { get; set; } = string.Empty;
        public string ReaderCardCode { get; set; } = string.Empty;
        public string ReaderEmail { get; set; } = string.Empty;
        public string? ReaderPhone { get; set; }
        public string? ReaderAddress { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string BookManagementCode { get; set; } = string.Empty;
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public int DaysOverdue { get; set; }
        public int DaysRemaining { get; set; }
    }
}

