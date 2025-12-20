using book.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace book.Pages.Admin
{
    public partial class AdminDashboardPage : ContentPage
    {
        private readonly AuthService _authService;

        public ObservableCollection<MenuItem> MenuItems { get; } = new();

        public AdminDashboardPage(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
            BindingContext = this;

            MenuItems.Add(new MenuItem
            {
                Title = "Quản lý Sách",
                Icon = "📚",
                Command = new Command(async () => await Shell.Current.GoToAsync("admin/books"))
            });
            MenuItems.Add(new MenuItem
            {
                Title = "Quản lý Người dùng",
                Icon = "👥",
                Command = new Command(async () => await Shell.Current.GoToAsync("admin/users"))
            });
            MenuItems.Add(new MenuItem
            {
                Title = "Mượn/Trả",
                Icon = "📖",
                Command = new Command(async () => await Shell.Current.GoToAsync("admin/borrow"))
            });
            MenuItems.Add(new MenuItem
            {
                Title = "Yêu cầu Mượn",
                Icon = "📋",
                Command = new Command(async () => await Shell.Current.GoToAsync("admin/borrow-requests"))
            });
            MenuItems.Add(new MenuItem
            {
                Title = "Kiểm tra Quá hạn",
                Icon = "⏰",
                Command = new Command(async () => await Shell.Current.GoToAsync("admin/overdue"))
            });
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            await _authService.LogoutAsync();
            await Shell.Current.GoToAsync("///login");
        }
    }

    public class MenuItem
    {
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty; // Thêm mô tả
        public string Icon { get; set; } = string.Empty;
        public string CardColor { get; set; } = "#6C63FF"; // Thêm màu nền
        public ICommand Command { get; set; } = null!;
    }
}

