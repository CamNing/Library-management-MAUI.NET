using book.Models;
using book.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace book.Pages.Admin
{
    public partial class ReportPage : ContentPage, INotifyPropertyChanged
    {
        private readonly ApiService _apiService;

        private DetailedReport _reportData;
        public DetailedReport ReportData
        {
            get => _reportData;
            set { _reportData = value; OnPropertyChanged(); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotLoading)); }
        }
        public bool IsNotLoading => !IsLoading;

        public ReportPage(ApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadReportData();
        }

        private async Task LoadReportData()
        {
            try
            {
                IsLoading = true;
                var data = await _apiService.GetDetailedReportAsync();

                if (data != null)
                {
                    ProcessChartData(data);
                    ReportData = data;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", "Không thể tải báo cáo: " + ex.Message, "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ProcessChartData(DetailedReport data)
        {
            // --- XỬ LÝ BIỂU ĐỒ CỘT (LOAN TRENDS) ---
            int maxLoan = data.MonthlyStats.Any() ? data.MonthlyStats.Max(x => x.LoanCount) : 1;
            if (maxLoan == 0) maxLoan = 1;

            foreach (var item in data.MonthlyStats)
            {
                // Max height là 120 (trong khung Grid 180) để chừa chỗ cho Label
                double height = (double)item.LoanCount / maxLoan * 120;
                item.BarHeight = height < 5 ? 5 : height; // Tối thiểu 5px

                item.BarColor = item.LoanCount == maxLoan ? "#4F46E5" : "#A5B4FC";
            }

            // --- XỬ LÝ BIỂU ĐỒ THANH NGANG (TOP BOOKS) ---
            int maxBookBorrow = data.TopBooks.Any() ? data.TopBooks.Max(x => x.BorrowCount) : 1;
            if (maxBookBorrow == 0) maxBookBorrow = 1;

            foreach (var item in data.TopBooks)
            {
                // Max width là 220 (trừ lề màn hình)
                double width = (double)item.BorrowCount / maxBookBorrow * 220;
                item.BarWidth = width < 10 ? 10 : width; // Tối thiểu 10px

                item.BarColor = item.BorrowCount == maxBookBorrow ? "#10B981" : "#34D399";
            }

            // --- XỬ LÝ MÀU SẮC USER ---
            for (int i = 0; i < data.TopUsers.Count; i++)
            {
                var user = data.TopUsers[i];
                if (i == 0) user.RankColor = "#F59E0B"; // Vàng
                else if (i == 1) user.RankColor = "#9CA3AF"; // Bạc
                else if (i == 2) user.RankColor = "#B45309"; // Đồng
                else user.RankColor = "#6C63FF"; // Tím
            }
        }

        public new event PropertyChangedEventHandler PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("admin/dashboard");
        }
    }
    }