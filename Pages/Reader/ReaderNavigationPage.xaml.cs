using book.Services;

namespace book.Pages.Reader
{
    public partial class ReaderNavigationPage : ContentPage
    {
        private readonly AuthService _authService;

        public ReaderNavigationPage(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
        }

        private async void OnHomeClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("reader/home");
        }

        private async void OnMyLoansClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("reader/loans");
        }

        private async void OnSearchClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("reader/search");
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            await _authService.LogoutAsync();
            await Shell.Current.GoToAsync("///login");
        }
    }
}

