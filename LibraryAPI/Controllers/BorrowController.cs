using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Models;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LibraryAPI.Controllers
{
    [ApiController]
    [Route("api/admin/borrow")]
    [Authorize(Roles = "Admin")]
    public class BorrowController : ControllerBase
    {
        private readonly LibraryDbContext _context;
        private readonly EmailService _emailService;

        public BorrowController(LibraryDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpPost("request")]
        public async Task<ActionResult> RequestBorrow([FromBody] DTOs.BorrowRequest request)
        {
            var readerCard = await _context.ReaderCards
                .Include(rc => rc.User)
                .FirstOrDefaultAsync(rc => rc.CardCode == request.ReaderCardCode);

            if (readerCard == null)
            {
                return NotFound(new { message = "Reader card not found" });
            }

            // Validate books availability
            var books = await _context.Books
                .Where(b => request.BookIds.Contains(b.Id))
                .ToListAsync();

            if (books.Count != request.BookIds.Count)
            {
                return BadRequest(new { message = "One or more books not found" });
            }

            var unavailableBooks = books.Where(b => b.AvailableQuantity <= 0).ToList();
            if (unavailableBooks.Any())
            {
                return BadRequest(new
                {
                    message = "Some books are not available",
                    unavailableBooks = unavailableBooks.Select(b => new { b.Id, b.Title, b.AvailableQuantity })
                });
            }

            // Calculate due date
            var dueDate = request.CustomDueDate ?? DateTime.UtcNow.AddDays(request.LoanDays);

            // Generate verification code
            var random = new Random();
            var verificationCode = random.Next(100000, 999999).ToString();
            var expiresAt = DateTime.UtcNow.AddMinutes(10);

            var emailCode = new EmailVerificationCode
            {
                ReaderCardId = readerCard.Id,
                Code = verificationCode,
                Type = "Borrow",
                ExpiresAt = expiresAt,
                IsUsed = false,
                CreatedAt = DateTime.UtcNow,
                RequestData = JsonSerializer.Serialize(new
                {
                    request.BookIds,
                    request.LoanDays,
                    request.CustomDueDate,
                    DueDate = dueDate
                })
            };

            _context.EmailVerificationCodes.Add(emailCode);
            await _context.SaveChangesAsync();

            // Send email
            var bookTitles = books.Select(b => b.Title).ToList();
            await _emailService.SendBorrowVerificationEmailAsync(
                readerCard,
                bookTitles,
                DateTime.UtcNow,
                dueDate,
                verificationCode);

            return Ok(new
            {
                message = "Verification code sent to reader's email",
                verificationCodeId = emailCode.Id,
                expiresAt
            });
        }

        [HttpPost("confirm")]
        public async Task<ActionResult> ConfirmBorrow([FromBody] ConfirmBorrowRequest request)
        {
            var emailCode = await _context.EmailVerificationCodes
                .Include(evc => evc.ReaderCard)
                    .ThenInclude(rc => rc.User)
                .FirstOrDefaultAsync(evc => evc.Id == request.VerificationCodeId);

            if (emailCode == null)
            {
                return NotFound(new { message = "Verification code not found" });
            }

            if (emailCode.IsUsed)
            {
                return BadRequest(new { message = "Verification code already used" });
            }

            if (emailCode.ExpiresAt < DateTime.UtcNow)
            {
                return BadRequest(new { message = "Verification code expired" });
            }

            if (emailCode.Code != request.Code)
            {
                return BadRequest(new { message = "Invalid verification code" });
            }

            if (emailCode.Type != "Borrow")
            {
                return BadRequest(new { message = "Invalid verification code type" });
            }

            // Deserialize borrow request
            if (string.IsNullOrEmpty(emailCode.RequestData))
            {
                return BadRequest(new { message = "Invalid request data" });
            }

            var requestData = JsonSerializer.Deserialize<JsonElement>(emailCode.RequestData);
            var bookIds = requestData.GetProperty("BookIds").EnumerateArray().Select(e => e.GetInt32()).ToList();
            var dueDate = requestData.GetProperty("DueDate").GetDateTime();

            // Get books
            var books = await _context.Books
                .Where(b => bookIds.Contains(b.Id))
                .ToListAsync();

            // Verify availability again
            var unavailableBooks = books.Where(b => b.AvailableQuantity <= 0).ToList();
            if (unavailableBooks.Any())
            {
                return BadRequest(new { message = "Some books are no longer available" });
            }

            // Create loan
            var loan = new Loan
            {
                ReaderCardId = emailCode.ReaderCardId,
                BorrowDate = DateTime.UtcNow,
                DueDate = dueDate,
                Status = "Borrowed"
            };

            _context.Loans.Add(loan);
            await _context.SaveChangesAsync();

            // Create loan items and update book quantities
            foreach (var book in books)
            {
                var loanItem = new LoanItem
                {
                    LoanId = loan.Id,
                    BookId = book.Id,
                    Quantity = 1,
                    Status = "Borrowed"
                };

                _context.LoanItems.Add(loanItem);
                book.AvailableQuantity--;
            }

            emailCode.IsUsed = true;
            await _context.SaveChangesAsync();

            // Send notification email after successful confirmation
            var bookTitles = books.Select(b => b.Title).ToList();
            await _emailService.SendBorrowNotificationEmailAsync(
                emailCode.ReaderCard,
                bookTitles,
                loan.BorrowDate,
                loan.DueDate,
                loan.Id);

            return Ok(new { message = "Books borrowed successfully. Notification email sent.", loanId = loan.Id });
        }

        [HttpPost("execute")]
        public async Task<ActionResult> ExecuteBorrow([FromBody] DTOs.BorrowRequest request)
        {
            var readerCard = await _context.ReaderCards
                .Include(rc => rc.User)
                .FirstOrDefaultAsync(rc => rc.CardCode == request.ReaderCardCode);

            if (readerCard == null)
            {
                return NotFound(new { message = "Reader card not found" });
            }

            // Validate books availability
            var books = await _context.Books
                .Where(b => request.BookIds.Contains(b.Id))
                .ToListAsync();

            if (books.Count != request.BookIds.Count)
            {
                return BadRequest(new { message = "One or more books not found" });
            }

            var unavailableBooks = books.Where(b => b.AvailableQuantity <= 0).ToList();
            if (unavailableBooks.Any())
            {
                return BadRequest(new
                {
                    message = "Some books are not available",
                    unavailableBooks = unavailableBooks.Select(b => new { b.Id, b.Title, b.AvailableQuantity })
                });
            }

            // Calculate due date
            var dueDate = request.CustomDueDate ?? DateTime.UtcNow.AddDays(request.LoanDays);

            // Create loan
            var loan = new Loan
            {
                ReaderCardId = readerCard.Id,
                BorrowDate = DateTime.UtcNow,
                DueDate = dueDate,
                Status = "Borrowed"
            };

            _context.Loans.Add(loan);
            await _context.SaveChangesAsync();

            // Create loan items and update book quantities
            foreach (var book in books)
            {
                var loanItem = new LoanItem
                {
                    LoanId = loan.Id,
                    BookId = book.Id,
                    Quantity = 1,
                    Status = "Borrowed"
                };

                _context.LoanItems.Add(loanItem);
                book.AvailableQuantity--;
            }

            await _context.SaveChangesAsync();

            // Send notification email
            var bookTitles = books.Select(b => b.Title).ToList();
            await _emailService.SendBorrowNotificationEmailAsync(
                readerCard,
                bookTitles,
                DateTime.UtcNow,
                dueDate,
                loan.Id);

            return Ok(new { message = "Books borrowed successfully. Notification email sent.", loanId = loan.Id });
        }
    }
}

