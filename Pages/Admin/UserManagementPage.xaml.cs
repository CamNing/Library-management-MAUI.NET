using book.Models;
using book.Services;
using System.Collections.ObjectModel;
using Microsoft.Maui.ApplicationModel;

namespace book.Pages.Admin
{
    public partial class UserManagementPage : ContentPage
    {
        private readonly ApiService _apiService;
        public ObservableCollection<User> Users { get; } = new();

        public UserManagementPage(ApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;
            BindingContext = this;
            LoadUsersAsync();
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

        private async void LoadUsersAsync()
        {
            try
            {
                var users = await _apiService.GetAsync<List<User>>("admin/users");
                if (users != null)
                {
                    Users.Clear();
                    foreach (var user in users)
                    {
                        Users.Add(user);
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Lỗi", $"Không thể tải danh sách người dùng: {ex.Message}", "OK");
            }
        }

        private async void OnAddUserClicked(object sender, EventArgs e)
        {
            var username = await DisplayPromptAsync("Thêm Người dùng", "Tên đăng nhập:");
            if (string.IsNullOrWhiteSpace(username)) return;

            var password = await DisplayPromptAsync("Thêm Người dùng", "Mật khẩu:");
            if (string.IsNullOrWhiteSpace(password)) return;

            var email = await DisplayPromptAsync("Thêm Người dùng", "Email:");
            if (string.IsNullOrWhiteSpace(email)) return;

            var role = await DisplayActionSheet("Chọn Vai trò", "Hủy", null, "Admin", "Reader");
            if (role == "Hủy" || string.IsNullOrWhiteSpace(role)) return;

            string? fullName = null;
            string? phone = null;
            string? address = null;

            if (role == "Reader")
            {
                fullName = await DisplayPromptAsync("Thêm Người dùng", "Họ và tên:");
                phone = await DisplayPromptAsync("Thêm Người dùng", "Số điện thoại:");
                address = await DisplayPromptAsync("Thêm Người dùng", "Địa chỉ:");
            }

            try
            {
                var request = new CreateUserRequest
                {
                    Username = username,
                    Password = password,
                    Email = email,
                    Role = role,
                    FullName = fullName,
                    Phone = phone,
                    Address = address
                };

                await _apiService.PostAsync<CreateUserRequest, object>("admin/users", request);
                await DisplayAlertAsync("Thành công", "Thêm người dùng thành công", "OK");
                LoadUsersAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Lỗi", $"Không thể thêm người dùng: {ex.Message}", "OK");
            }
        }

        private async void OnResetPasswordClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is User user)
            {
                var newPassword = await DisplayPromptAsync("Đặt lại Mật khẩu", $"Nhập mật khẩu mới cho {user.Username}:");
                if (string.IsNullOrWhiteSpace(newPassword)) return;

                try
                {
                    await _apiService.PutAsync<string, object>($"admin/users/{user.Id}/reset-password", newPassword);
                    await DisplayAlertAsync("Thành công", "Đặt lại mật khẩu thành công", "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync("Lỗi", $"Không thể đặt lại mật khẩu: {ex.Message}", "OK");
                }
            }
        }

        private async void OnToggleActiveClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is User user)
            {
                var action = user.IsActive ? "vô hiệu hóa" : "kích hoạt";
                var confirm = await DisplayAlert("Xác nhận", $"Bạn có chắc muốn {action} người dùng {user.Username}?", "Có", "Không");
                if (!confirm) return;

                try
                {
                    await _apiService.PutAsync<object, object>($"admin/users/{user.Id}/toggle-active", new { });
                    await DisplayAlertAsync("Thành công", $"Đã {action} người dùng thành công", "OK");
                    LoadUsersAsync();
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync("Lỗi", $"Không thể thay đổi trạng thái người dùng: {ex.Message}", "OK");
                }
            }
        }

        private async void OnDeleteUserClicked(object sender, EventArgs e)
        {
            // Delete functionality can be implemented if API supports it
            await DisplayAlertAsync("Thông tin", "Xóa người dùng bị vô hiệu hóa. Vui lòng sử dụng tính năng Kích hoạt/Vô hiệu hóa để vô hiệu hóa người dùng.", "OK");
        }
    }
}

