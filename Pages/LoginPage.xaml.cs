using book.Services;

namespace book.Pages
{
    public partial class LoginPage : ContentPage
    {
        private readonly AuthService _authService;
        private bool _isRegisterMode = false;

        public LoginPage(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
        }

        private void OnFieldChanged(object sender, TextChangedEventArgs e)
        {
            UpdateActionButtonState();
        }

        private void UpdateActionButtonState()
        {
            if (_isRegisterMode)
            {
                ActionButton.IsEnabled = !string.IsNullOrWhiteSpace(UsernameEntry.Text) &&
                                       !string.IsNullOrWhiteSpace(PasswordEntry.Text) &&
                                       !string.IsNullOrWhiteSpace(EmailEntry.Text);
            }
            else
            {
                ActionButton.IsEnabled = !string.IsNullOrWhiteSpace(UsernameEntry.Text) &&
                                       !string.IsNullOrWhiteSpace(PasswordEntry.Text);
            }
        }

        protected override bool OnBackButtonPressed()
        {
            // Prevent going back from login page
            return true;
        }

        private void OnToggleClicked(object sender, EventArgs e)
        {
            _isRegisterMode = !_isRegisterMode;

            if (_isRegisterMode)
            {
                TitleLabel.Text = "Tạo tài khoản mới";
                ActionButton.Text = "Đăng ký";
                ToggleButton.Text = "Đã có tài khoản? Đăng nhập";
                EmailEntry.IsVisible = true;
                FullNameEntry.IsVisible = true;
                PhoneEntry.IsVisible = true;
                AddressEntry.IsVisible = true;
            }
            else
            {
                TitleLabel.Text = "Đăng nhập vào tài khoản";
                ActionButton.Text = "Đăng nhập";
                ToggleButton.Text = "Chưa có tài khoản? Đăng ký";
                EmailEntry.IsVisible = false;
                FullNameEntry.IsVisible = false;
                PhoneEntry.IsVisible = false;
                AddressEntry.IsVisible = false;
            }

            // Clear fields
            UsernameEntry.Text = string.Empty;
            PasswordEntry.Text = string.Empty;
            EmailEntry.Text = string.Empty;
            FullNameEntry.Text = string.Empty;
            PhoneEntry.Text = string.Empty;
            AddressEntry.Text = string.Empty;
            ErrorMessage.IsVisible = false;
            UpdateActionButtonState();
        }

        private async void OnActionClicked(object sender, EventArgs e)
        {
            ErrorMessage.IsVisible = false;
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            ActionButton.IsEnabled = false;

            try
            {
                if (_isRegisterMode)
                {
                    // Register
                    var response = await _authService.RegisterAsync(
                        UsernameEntry.Text,
                        PasswordEntry.Text,
                        EmailEntry.Text,
                        string.IsNullOrWhiteSpace(FullNameEntry.Text) ? null : FullNameEntry.Text,
                        string.IsNullOrWhiteSpace(PhoneEntry.Text) ? null : PhoneEntry.Text,
                        string.IsNullOrWhiteSpace(AddressEntry.Text) ? null : AddressEntry.Text);

                    if (response != null)
                    {
                        // Navigate based on role (should be Reader)
                        if (response.Role == "Admin")
                        {
                            await Shell.Current.GoToAsync("admin/dashboard");
                        }
                        else
                        {
                            await Shell.Current.GoToAsync("reader/home");
                        }
                    }
                    else
                    {
                        ErrorMessage.Text = "Đăng ký thất bại. Vui lòng kiểm tra thông tin của bạn.";
                        ErrorMessage.IsVisible = true;
                    }
                }
                else
                {
                    // Login
                    var response = await _authService.LoginAsync(
                        UsernameEntry.Text,
                        PasswordEntry.Text);

                    if (response != null)
                    {
                        // Navigate based on role
                        if (response.Role == "Admin")
                        {
                            await Shell.Current.GoToAsync("admin/dashboard");
                        }
                        else
                        {
                            await Shell.Current.GoToAsync("reader/home");
                        }
                    }
                    else
                    {
                        ErrorMessage.Text = "Tên đăng nhập hoặc mật khẩu không đúng";
                        ErrorMessage.IsVisible = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage.Text = $"{(_isRegisterMode ? "Đăng ký" : "Đăng nhập")} thất bại: {ex.Message}";
                ErrorMessage.IsVisible = true;
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
                UpdateActionButtonState();
            }
        }
    }
}

