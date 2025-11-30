using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Models;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace LibraryAPI.Controllers
{
    [ApiController]
    [Route("api/reader")]
    [Authorize(Roles = "Reader")]
    public class ReaderController : ControllerBase
    {
        private readonly LibraryDbContext _context;
        private readonly EmailService _emailService;
        private readonly ILogger<ReaderController> _logger;

        public ReaderController(LibraryDbContext context, EmailService emailService, ILogger<ReaderController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet("profile")]
        public async Task<ActionResult> GetProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users
                .Include(u => u.ReaderCard)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // If user is Reader but doesn't have ReaderCard, create one automatically
            if (user.Role == "Reader" && user.ReaderCard == null)
            {
                try
                {
                    var cardCode = GenerateCardCode();
                    var readerCard = new ReaderCard
                    {
                        UserId = user.Id,
                        CardCode = cardCode,
                        FullName = user.Username, // Default to username if no full name
                        Phone = null,
                        Address = null,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.ReaderCards.Add(readerCard);
                    await _context.SaveChangesAsync();

                    // Reload user with ReaderCard
                    user = await _context.Users
                        .Include(u => u.ReaderCard)
                        .FirstOrDefaultAsync(u => u.Id == userId);
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the request
                    // Return error message to client
                    return StatusCode(500, new { message = $"Failed to create reader card: {ex.Message}" });
                }
            }

            if (user?.ReaderCard == null)
            {
                return NotFound(new { message = "Reader card not found" });
            }

            return Ok(new
            {
                Username = user.Username,
                Email = user.Email,
                ReaderCard = new
                {
                    user.ReaderCard.CardCode,
                    user.ReaderCard.FullName,
                    user.ReaderCard.Phone,
                    user.ReaderCard.Address
                }
            });
        }

        private string GenerateCardCode()
        {
            // Generate a unique card code
            var random = new Random();
            var code = $"RC{random.Next(100000, 999999)}";
            
            // Ensure uniqueness
            while (_context.ReaderCards.Any(rc => rc.CardCode == code))
            {
                code = $"RC{random.Next(100000, 999999)}";
            }

            return code;
        }

        [HttpGet("my-loans")]
        public async Task<ActionResult> GetMyLoans()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users
                .Include(u => u.ReaderCard)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // If user is Reader but doesn't have ReaderCard, create one automatically
            if (user.Role == "Reader" && user.ReaderCard == null)
            {
                try
                {
                    var cardCode = GenerateCardCode();
                    var readerCard = new ReaderCard
                    {
                        UserId = user.Id,
                        CardCode = cardCode,
                        FullName = user.Username,
                        Phone = null,
                        Address = null,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.ReaderCards.Add(readerCard);
                    await _context.SaveChangesAsync();

                    // Reload user with ReaderCard
                    user = await _context.Users
                        .Include(u => u.ReaderCard)
                        .FirstOrDefaultAsync(u => u.Id == userId);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = $"Failed to create reader card: {ex.Message}" });
                }
            }

            if (user?.ReaderCard == null)
            {
                return NotFound(new { message = "Reader card not found" });
            }

            var loans = await _context.Loans
                .Include(l => l.LoanItems)
                    .ThenInclude(li => li.Book)
                        .ThenInclude(b => b.BookAuthors)
                            .ThenInclude(ba => ba.Author)
                .Where(l => l.ReaderCardId == user.ReaderCard.Id)
                .OrderByDescending(l => l.BorrowDate)
                .ToListAsync();

            var loanDtos = loans.Select(l => new
            {
                l.Id,
                l.BorrowDate,
                l.DueDate,
                l.ReturnDate,
                l.Status,
                Items = l.LoanItems.Select(li => new
                {
                    li.BookId,
                    li.Book.Title,
                    li.Book.ManagementCode,
                    li.Book.CoverImageUrl,
                    Authors = li.Book.BookAuthors.Select(ba => ba.Author.Name).ToList(),
                    li.Quantity,
                    li.Status
                }).ToList()
            }).ToList();

            return Ok(loanDtos);
        }

        [HttpPost("borrow/request")]
        public async Task<ActionResult> RequestBorrow([FromBody] ReaderBorrowRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users
                .Include(u => u.ReaderCard)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // If user is Reader but doesn't have ReaderCard, create one automatically
            if (user.Role == "Reader" && user.ReaderCard == null)
            {
                try
                {
                    var cardCode = GenerateCardCode();
                    var readerCard = new ReaderCard
                    {
                        UserId = user.Id,
                        CardCode = cardCode,
                        FullName = user.Username, // Default to username if no full name
                        Phone = null,
                        Address = null,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.ReaderCards.Add(readerCard);
                    await _context.SaveChangesAsync();

                    // Reload user with ReaderCard
                    user = await _context.Users
                        .Include(u => u.ReaderCard)
                        .FirstOrDefaultAsync(u => u.Id == userId);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = $"Failed to create reader card: {ex.Message}" });
                }
            }

            if (user?.ReaderCard == null)
            {
                return BadRequest(new { message = "User is not a Reader or reader card could not be created" });
            }

            // Validate loan days
            if (request.LoanDays <= 0 || request.LoanDays > 365)
            {
                return BadRequest(new { message = "Số ngày mượn phải từ 1 đến 365 ngày" });
            }

            // Validate books exist
            var books = await _context.Books
                .Where(b => request.BookIds.Contains(b.Id))
                .ToListAsync();

            if (books.Count != request.BookIds.Count)
            {
                return BadRequest(new { message = "Một hoặc nhiều cuốn sách không tồn tại" });
            }

            // Check if books are available
            var unavailableBooks = books.Where(b => b.AvailableQuantity <= 0).ToList();
            if (unavailableBooks.Any())
            {
                var unavailableTitles = string.Join(", ", unavailableBooks.Select(b => b.Title));
                return BadRequest(new 
                { 
                    message = $"Các cuốn sách sau không còn sẵn: {unavailableTitles}",
                    unavailableBooks = unavailableBooks.Select(b => new { b.Id, b.Title, b.AvailableQuantity })
                });
            }

            // Check if user already has pending borrow request for the SAME books
            // Only block if requesting the exact same books that are still pending
            var existingPendingRequests = await _context.BorrowRequests
                .Where(br => br.ReaderCardId == user.ReaderCard.Id 
                    && br.Status == "Pending"
                    && br.LoanId == null)
                .ToListAsync();

            foreach (var existingRequest in existingPendingRequests)
            {
                var existingBookIds = JsonSerializer.Deserialize<List<int>>(existingRequest.BookIds) ?? new List<int>();
                // Only block if requesting the exact same set of books
                if (existingBookIds.Count == request.BookIds.Count && 
                    existingBookIds.All(id => request.BookIds.Contains(id)) &&
                    request.BookIds.All(id => existingBookIds.Contains(id)))
                {
                    var duplicateBookTitles = books.Where(b => request.BookIds.Contains(b.Id)).Select(b => b.Title);
                    return BadRequest(new 
                    { 
                        message = $"Bạn đã có yêu cầu mượn đang chờ xử lý cho các cuốn sách: {string.Join(", ", duplicateBookTitles)}" 
                    });
                }
            }

            // Check if user is currently borrowing (not returned) the SAME books
            // Logic: Only block if the book is currently borrowed (Status != "Returned")
            // If book has been returned (Status = "Returned"), allow re-borrowing
            // Allow borrowing different books even if user has other active loans
            // IMPORTANT: Filter directly in database query to ensure we only check non-returned items
            // Note: We check LoanItem.Status, not Loan.Status, because each book item has its own status
            
            // First, get ALL loan items for these books (for debugging)
            var allLoanItemsForBooks = await _context.LoanItems
                .Include(li => li.Loan)
                .Include(li => li.Book)
                .Where(li => li.Loan.ReaderCardId == user.ReaderCard.Id 
                    && request.BookIds.Contains(li.BookId))
                .ToListAsync();
            
            // Log all loan items for debugging
            _logger.LogInformation($"User {user.Id} requesting books {string.Join(", ", request.BookIds)}. Found {allLoanItemsForBooks.Count} total loan items for these books.");
            foreach (var item in allLoanItemsForBooks)
            {
                _logger.LogInformation($"  - LoanItem {item.Id}: BookId={item.BookId}, BookTitle={item.Book?.Title}, Status='{item.Status}', Loan.Status='{item.Loan?.Status}', Loan.ReturnDate={item.Loan?.ReturnDate}");
            }
            
            // Filter to only active (not returned) loan items
            var activeLoanItems = allLoanItemsForBooks
                .Where(li => li.Status != "Returned")
                .ToList();
            
            _logger.LogInformation($"User {user.Id}: Found {activeLoanItems.Count} active (not returned) loan items.");

            if (activeLoanItems.Any())
            {
                var currentlyBorrowedBookIds = activeLoanItems.Select(li => li.BookId).Distinct().ToList();
                var duplicateBookTitles = books.Where(b => currentlyBorrowedBookIds.Contains(b.Id)).Select(b => b.Title);
                
                // Log for debugging
                _logger.LogWarning($"User {user.Id} tried to borrow books that are still active: {string.Join(", ", duplicateBookTitles)}");
                
                return BadRequest(new 
                { 
                    message = $"Bạn đang mượn các cuốn sách sau: {string.Join(", ", duplicateBookTitles)}. Vui lòng trả sách trước khi mượn lại." 
                });
            }

            // If we reach here, either:
            // 1. User has never borrowed these books before, OR
            // 2. User has returned these books (Status = "Returned") - allow re-borrowing
            _logger.LogInformation($"User {user.Id} can borrow books: {string.Join(", ", request.BookIds)} - No active loans found for these books");

            // Calculate due date
            var dueDate = request.CustomDueDate ?? DateTime.UtcNow.AddDays(request.LoanDays);
            
            // Validate due date is in the future
            if (dueDate <= DateTime.UtcNow)
            {
                return BadRequest(new { message = "Ngày hết hạn phải trong tương lai" });
            }

            // Create borrow request (pending approval)
            var borrowRequest = new Models.BorrowRequest
            {
                ReaderCardId = user.ReaderCard.Id,
                BookIds = JsonSerializer.Serialize(request.BookIds),
                LoanDays = request.LoanDays,
                CustomDueDate = request.CustomDueDate,
                CalculatedDueDate = dueDate,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.BorrowRequests.Add(borrowRequest);
            await _context.SaveChangesAsync();

            // Get book titles for response
            var bookTitles = books.Select(b => b.Title).ToList();

            return Ok(new
            {
                message = "Yêu cầu mượn sách đã được gửi thành công. Đang chờ admin phê duyệt.",
                requestId = borrowRequest.Id,
                status = "Pending",
                books = bookTitles,
                dueDate = dueDate.ToString("yyyy-MM-dd"),
                loanDays = request.LoanDays
            });
        }
    }
}

