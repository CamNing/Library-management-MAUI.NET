using Microsoft.Extensions.DependencyInjection;

namespace book
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
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