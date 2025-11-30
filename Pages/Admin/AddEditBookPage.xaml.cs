using book.Models;
using book.Services;
using System.Text;
using Microsoft.Maui.ApplicationModel;
using System.Linq;

namespace book.Pages.Admin
{
    public partial class AddEditBookPage : ContentPage
    {
        private readonly ApiService _apiService;
        private int? _bookId;

        public AddEditBookPage(ApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;
            _bookId = null; // Initialize as new book
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            // Only load if we haven't loaded yet
            if (_bookId.HasValue)
            {
                return;
            }
            
            // Check if we're editing - get ID from NavigationDataService first (most reliable)
            var bookId = NavigationDataService.GetEditBookId();
            
            if (bookId.HasValue)
            {
                _bookId = bookId.Value;
                System.Diagnostics.Debug.WriteLine($"Loading book with ID from NavigationDataService: {bookId.Value}");
                LoadBookAsync(bookId.Value);
                return;
            }
            
            // Fallback: Try to parse from URL
            try
            {
                var location = Shell.Current.CurrentState.Location;
                var originalString = location.OriginalString;
                
                System.Diagnostics.Debug.WriteLine($"OnAppearing - Location: {originalString}");
                
                if (originalString.Contains("edit-book"))
                {
                    // Method 1: Parse from OriginalString using regex
                    var idMatch = System.Text.RegularExpressions.Regex.Match(originalString, @"edit-book[?&](?:bookId|id)=(\d+)");
                    if (idMatch.Success && int.TryParse(idMatch.Groups[1].Value, out int parsedId))
                    {
                        _bookId = parsedId;
                        System.Diagnostics.Debug.WriteLine($"Found ID via regex: {parsedId}");
                        LoadBookAsync(parsedId);
                        return;
                    }
                    
                    // Method 2: Try splitting
                    var parts = originalString.Split(new[] { "edit-book", "?", "&" }, StringSplitOptions.None);
                    foreach (var part in parts)
                    {
                        if (part.StartsWith("bookId="))
                        {
                            var idStr = part.Substring(7);
                            if (int.TryParse(idStr, out int id))
                            {
                                _bookId = id;
                                System.Diagnostics.Debug.WriteLine($"Found ID via splitting (bookId): {id}");
                                LoadBookAsync(id);
                                return;
                            }
                        }
                        else if (part.StartsWith("id="))
                        {
                            var idStr = part.Substring(3);
                            if (int.TryParse(idStr, out int id))
                            {
                                _bookId = id;
                                System.Diagnostics.Debug.WriteLine($"Found ID via splitting (id): {id}");
                                LoadBookAsync(id);
                                return;
                            }
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Could not parse book ID from: {originalString}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing book ID: {ex.Message}");
            }
            
            // If no ID found, reset to new book mode
            if (!_bookId.HasValue)
            {
                ResetToNewBookMode();
            }
        }

        private void ResetToNewBookMode()
        {
            TitleEntry.Text = "";
            ManagementCodeEntry.Text = "";
            DescriptionEditor.Text = "";
            CategoryEntry.Text = "";
            PublishedYearEntry.Text = "";
            CoverImageUrlEntry.Text = "";
            TotalQuantityEntry.Text = "1";
            AuthorsEntry.Text = "";
            if (PageTitleLabel != null)
            {
                PageTitleLabel.Text = "Thêm Sách";
            }
            Title = "Thêm Sách";
        }

        private async void LoadBookAsync(int id)
        {
            try
            {
                LoadingIndicator.IsRunning = true;
                LoadingIndicator.IsVisible = true;
                SaveButton.IsEnabled = false;

                // Clear all fields first
                TitleEntry.Text = "";
                ManagementCodeEntry.Text = "";
                DescriptionEditor.Text = "";
                CategoryEntry.Text = "";
                PublishedYearEntry.Text = "";
                CoverImageUrlEntry.Text = "";
                TotalQuantityEntry.Text = "1";
                AuthorsEntry.Text = "";

                var book = await _apiService.GetAsync<Book>($"admin/books/{id}");
                if (book != null)
                {
                    // Populate all fields with book data
                    TitleEntry.Text = book.Title ?? "";
                    ManagementCodeEntry.Text = book.ManagementCode ?? "";
                    DescriptionEditor.Text = book.Description ?? "";
                    CategoryEntry.Text = book.Category ?? "";
                    PublishedYearEntry.Text = book.PublishedYear?.ToString() ?? "";
                    CoverImageUrlEntry.Text = book.CoverImageUrl ?? "";
                    TotalQuantityEntry.Text = book.TotalQuantity.ToString();
                    
                    // Format authors as comma-separated string
                    if (book.Authors != null && book.Authors.Any())
                    {
                        AuthorsEntry.Text = string.Join(", ", book.Authors);
                    }
                    else
                    {
                        AuthorsEntry.Text = "";
                    }
                    
                    // Update title
                    Title = "Sửa Sách";
                    if (PageTitleLabel != null)
                    {
                        PageTitleLabel.Text = "Sửa Sách";
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Book loaded successfully: {book.Title}");
                }
                else
                {
                    await DisplayAlertAsync("Lỗi", "Không tìm thấy sách", "OK");
                    await Shell.Current.GoToAsync("admin/books");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading book: {ex.Message}");
                await DisplayAlertAsync("Lỗi", $"Không thể tải sách: {ex.Message}", "OK");
                await Shell.Current.GoToAsync("admin/books");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
                SaveButton.IsEnabled = true;
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleEntry.Text) ||
                string.IsNullOrWhiteSpace(ManagementCodeEntry.Text) ||
                !int.TryParse(TotalQuantityEntry.Text, out int quantity) || quantity <= 0)
            {
                await DisplayAlertAsync("Lỗi", "Vui lòng điền đầy đủ các trường bắt buộc (Tiêu đề, Mã quản lý, Tổng số lượng)", "OK");
                return;
            }

            try
            {
                LoadingIndicator.IsRunning = true;
                LoadingIndicator.IsVisible = true;
                SaveButton.IsEnabled = false;

                var authors = AuthorsEntry.Text
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(a => a.Trim())
                    .Where(a => !string.IsNullOrWhiteSpace(a))
                    .ToList();

                if (_bookId.HasValue)
                {
                    // Update
                    var request = new UpdateBookRequest
                    {
                        Title = TitleEntry.Text,
                        ManagementCode = ManagementCodeEntry.Text,
                        Description = DescriptionEditor.Text,
                        Category = CategoryEntry.Text,
                        PublishedYear = int.TryParse(PublishedYearEntry.Text, out int year) ? year : null,
                        CoverImageUrl = CoverImageUrlEntry.Text,
                        TotalQuantity = quantity,
                        Authors = authors
                    };

                    await _apiService.PutAsync<UpdateBookRequest, Book>($"admin/books/{_bookId}", request);
                    await DisplayAlertAsync("Thành công", "Cập nhật sách thành công", "OK");
                }
                else
                {
                    // Create
                    var request = new CreateBookRequest
                    {
                        Title = TitleEntry.Text,
                        ManagementCode = ManagementCodeEntry.Text,
                        Description = DescriptionEditor.Text,
                        Category = CategoryEntry.Text,
                        PublishedYear = int.TryParse(PublishedYearEntry.Text, out int year) ? year : null,
                        CoverImageUrl = CoverImageUrlEntry.Text,
                        TotalQuantity = quantity,
                        Authors = authors
                    };

                    await _apiService.PostAsync<CreateBookRequest, Book>("admin/books", request);
                    await DisplayAlertAsync("Thành công", "Thêm sách thành công", "OK");
                }

                await Shell.Current.GoToAsync("admin/books");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Lỗi", $"Không thể lưu sách: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
                SaveButton.IsEnabled = true;
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("admin/books");
        }

        protected override bool OnBackButtonPressed()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.GoToAsync("admin/books");
            });
            return true;
        }
    }
}

