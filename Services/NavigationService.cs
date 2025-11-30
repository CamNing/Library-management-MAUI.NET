using book.Services;

namespace book.Services
{
    public class NavigationService
    {
        private readonly AuthService _authService;

        public NavigationService(AuthService authService)
        {
            _authService = authService;
        }

        public static async Task NavigateBackAsync()
        {
            try
            {
                if (Shell.Current.Navigation.NavigationStack.Count > 1)
                {
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    // If we're at root, navigate to appropriate dashboard
                    await NavigateToDashboardAsync();
                }
            }
            catch
            {
                // Fallback to dashboard if relative navigation fails
                await NavigateToDashboardAsync();
            }
        }

        public static async Task NavigateToDashboardAsync()
        {
            var token = await SecureStorage.GetAsync("auth_token");
            if (string.IsNullOrEmpty(token))
            {
                await Shell.Current.GoToAsync("///login");
                return;
            }

            // Get user role from token or storage
            var role = await SecureStorage.GetAsync("user_role");
            if (role == "Admin")
            {
                await Shell.Current.GoToAsync("admin/dashboard");
            }
            else if (role == "Reader")
            {
                await Shell.Current.GoToAsync("reader/home");
            }
            else
            {
                await Shell.Current.GoToAsync("///login");
            }
        }

        public static async Task<bool> CanGoBackAsync()
        {
            return Shell.Current.Navigation.NavigationStack.Count > 1;
        }
    }
}

