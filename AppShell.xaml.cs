namespace book;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        
        // Register routes
        Routing.RegisterRoute("login", typeof(Pages.LoginPage));
        Routing.RegisterRoute("admin/dashboard", typeof(Pages.Admin.AdminDashboardPage));
        Routing.RegisterRoute("admin/books", typeof(Pages.Admin.BooksManagementPage));
        Routing.RegisterRoute("admin/add-book", typeof(Pages.Admin.AddEditBookPage));
        Routing.RegisterRoute("admin/edit-book", typeof(Pages.Admin.AddEditBookPage));
        Routing.RegisterRoute("admin/users", typeof(Pages.Admin.UserManagementPage));
        Routing.RegisterRoute("admin/borrow", typeof(Pages.Admin.BorrowReturnPage));
        Routing.RegisterRoute("admin/borrow-requests", typeof(Pages.Admin.BorrowRequestsPage));
        Routing.RegisterRoute("admin/overdue", typeof(Pages.Admin.OverduePage));
        Routing.RegisterRoute("reader/home", typeof(Pages.Reader.ReaderHomePage));
        Routing.RegisterRoute("reader/search", typeof(Pages.Reader.SearchPage));
        Routing.RegisterRoute("reader/loans", typeof(Pages.Reader.MyLoansPage));
        Routing.RegisterRoute("reader/book-detail", typeof(Pages.Reader.BookDetailPage));
    }
}
