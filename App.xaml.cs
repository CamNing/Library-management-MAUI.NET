using Microsoft.Extensions.DependencyInjection;
using System.Windows.Input; // Thêm namespace này
using book.Services;        // Thêm namespace này

namespace book
{
    public partial class App : Application
    {
        // Khai báo lệnh mở chat toàn cục để file App.xaml có thể gọi
        public static ICommand GlobalChatCommand { get; private set; }

        // Inject IServiceProvider vào constructor
        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            // Khởi tạo lệnh mở chat
            GlobalChatCommand = new Command(async () =>
            {
                // Lấy ChatPage từ ServiceProvider (để nó tự động inject các service con)
                var chatPage = serviceProvider.GetRequiredService<Pages.Reader.ChatPage>();

                // Điều hướng đến trang Chat
                await Shell.Current.Navigation.PushAsync(chatPage);
            });
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell())
            {
                Title = "Library Management System"
            };
            return window;
        }
    }
}