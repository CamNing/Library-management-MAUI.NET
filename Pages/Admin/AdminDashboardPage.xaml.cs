using book.Models;
using book.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace book.Pages.Admin
{
    public partial class AdminDashboardPage : ContentPage, INotifyPropertyChanged
    {
        private readonly AuthService _authService;
        private readonly ApiService _apiService; // Thêm ApiService

        // Binding Properties
        private DashboardAnalytics _analyticsData;
        public DashboardAnalytics AnalyticsData
        {
            get => _analyticsData;
            set { _analyticsData = value; OnPropertyChanged(); OnPropertyChanged(nameof(RiskCount)); OnPropertyChanged(nameof(GrowthColor)); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotLoading)); }
        }
        public bool IsNotLoading => !IsLoading;

        public int RiskCount => AnalyticsData?.HighRiskUsers?.Count ?? 0;

        public string GrowthColor => (AnalyticsData?.GrowthRate ?? 0) >= 0 ? "#10B981" : "#EF4444"; // Xanh nếu tăng, Đỏ nếu giảm

        public ObservableCollection<MenuItem> MenuItems { get; } = new();

        public AdminDashboardPage(AuthService authService, ApiService apiService)
        {
            InitializeComponent();
            _authService = authService;
            _apiService = apiService;
            BindingContext = this;

            LoadMenuItems();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadAnalyticsData();
        }

        private async Task LoadAnalyticsData()
        {
            try
            {
                IsLoading = true;
                // Gọi API phân tích thông minh
                var data = await _apiService.GetAdminDashboardStatsAsync();

                if (data != null)
                {
                    AnalyticsData = data;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading stats: {ex.Message}");
                // Có thể hiển thị Toast hoặc Alert nhẹ nếu muốn
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void LoadMenuItems()
        {
            MenuItems.Clear();
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
                Title = "Mượn/Trả Sách",
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
                Title = "Sách Quá hạn",
                Icon = "⏰",
                Command = new Command(async () => await Shell.Current.GoToAsync("admin/overdue"))
            });
            // Thêm nút mới để xem chi tiết rủi ro nếu muốn (chưa implement page này)
            MenuItems.Add(new MenuItem
            {
                Title = "Báo cáo Chi tiết",
                Icon = "📊",
                Command = new Command(async () => await Shell.Current.GoToAsync("admin/report"))
            });

        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool answer = await DisplayAlert("Đăng xuất", "Bạn có chắc chắn muốn đăng xuất?", "Có", "Không");
            if (answer)
            {
                await _authService.LogoutAsync();
                await Shell.Current.GoToAsync("///login");
            }
        }

        // MVVM Helper
        public new event PropertyChangedEventHandler PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Class MenuItem giữ nguyên
    public class MenuItem
    {
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public ICommand Command { get; set; } = null!;
    }
}