using book.Services;
using book.Pages;
using Microsoft.Extensions.Logging;

namespace book
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // Register services
            builder.Services.AddSingleton<SecureStorageService>();
            builder.Services.AddSingleton<ApiService>();
            builder.Services.AddSingleton<AuthService>();

            // Register pages
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<Pages.Admin.AdminDashboardPage>();
            builder.Services.AddTransient<Pages.Admin.BooksManagementPage>();
            builder.Services.AddTransient<Pages.Admin.AddEditBookPage>();
            builder.Services.AddTransient<Pages.Admin.UserManagementPage>();
            builder.Services.AddTransient<Pages.Admin.BorrowReturnPage>();
            builder.Services.AddTransient<Pages.Admin.BorrowRequestsPage>();
            builder.Services.AddTransient<Pages.Admin.OverduePage>();
            builder.Services.AddTransient<Pages.Reader.ReaderHomePage>();
            builder.Services.AddTransient<Pages.Reader.SearchPage>();
            builder.Services.AddTransient<Pages.Reader.MyLoansPage>();
            builder.Services.AddTransient<Pages.Reader.BookDetailPage>();

            return builder.Build();
        }
    }
}
