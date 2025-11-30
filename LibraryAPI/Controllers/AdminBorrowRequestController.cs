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
    [Route("api/admin/borrow-requests")]
    [Authorize(Roles = "Admin")]
    public class AdminBorrowRequestController : ControllerBase
    {
        private readonly LibraryDbContext _context;
        private readonly EmailService _emailService;

        public AdminBorrowRequestController(LibraryDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<ActionResult> GetBorrowRequests([FromQuery] string? status)
        {
            var query = _context.BorrowRequests
                .Include(br => br.ReaderCard)
                    .ThenInclude(rc => rc.User)
                .Include(br => br.ProcessedByUser)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(br => br.Status == status);
            }

            var requests = await query
                .OrderByDescending(br => br.CreatedAt)
                .ToListAsync();

            var result = new List<object>();
            
            foreach (var br in requests)
            {
                var bookIds = JsonSerializer.Deserialize<List<int>>(br.BookIds) ?? new List<int>();
                var books = await _context.Books
                    .Where(b => bookIds.Contains(b.Id))
                    .Select(b => new { b.Id, b.Title, b.ManagementCode })
                    .ToListAsync();
                
                result.Add(new
                {
                    br.Id,
                    Reader = new
                    {
                        br.ReaderCard.FullName,
                        br.ReaderCard.CardCode,
                        br.ReaderCard.User.Email,
                        br.ReaderCard.User.Username
                    },
                    BookIds = bookIds,
                    Books = books.Select(b => new { b.Id, b.Title, b.ManagementCode }),
                    BookTitles = string.Join(", ", books.Select(b => b.Title)),
                    br.LoanDays,
                    br.CustomDueDate,
                    br.CalculatedDueDate,
                    br.Status,
                    br.RejectionReason,
                    br.CreatedAt,
                    br.ProcessedAt,
                    ProcessedBy = br.ProcessedByUser != null ? br.ProcessedByUser.Username : null,
                    br.LoanId
                });
            }

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetBorrowRequest(int id)
        {
            var request = await _context.BorrowRequests
                .Include(br => br.ReaderCard)
                    .ThenInclude(rc => rc.User)
                .Include(br => br.ProcessedByUser)
                .FirstOrDefaultAsync(br => br.Id == id);

            if (request == null)
            {
                return NotFound(new { message = "Borrow request not found" });
            }

            var bookIds = JsonSerializer.Deserialize<List<int>>(request.BookIds) ?? new List<int>();
            var books = await _context.Books
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .Where(b => bookIds.Contains(b.Id))
                .ToListAsync();

            return Ok(new
            {
                request.Id,
                Reader = new
                {
                    request.ReaderCard.FullName,
                    request.ReaderCard.CardCode,
                    request.ReaderCard.User.Email,
                    request.ReaderCard.User.Username
                },
                Books = books.Select(b => new
                {
                    b.Id,
                    b.Title,
                    b.ManagementCode,
                    b.AvailableQuantity,
                    Authors = b.BookAuthors.Select(ba => ba.Author.Name).ToList()
                }),
                request.LoanDays,
                request.CustomDueDate,
                request.CalculatedDueDate,
                request.Status,
                request.RejectionReason,
                request.CreatedAt,
                request.ProcessedAt,
                ProcessedBy = request.ProcessedByUser != null ? request.ProcessedByUser.Username : null,
                request.LoanId
            });
        }

        [HttpPost("{id}/approve")]
        public async Task<ActionResult> ApproveBorrowRequest(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int adminUserId))
            {
                return Unauthorized();
            }

            var request = await _context.BorrowRequests
                .Include(br => br.ReaderCard)
                    .ThenInclude(rc => rc.User)
                .FirstOrDefaultAsync(br => br.Id == id);

            if (request == null)
            {
                return NotFound(new { message = "Borrow request not found" });
            }

            if (request.Status != "Pending")
            {
                return BadRequest(new { message = $"Request is already {request.Status}" });
            }

            var bookIds = JsonSerializer.Deserialize<List<int>>(request.BookIds) ?? new List<int>();
            var books = await _context.Books
                .Where(b => bookIds.Contains(b.Id))
                .ToListAsync();

            // Verify availability
            var unavailableBooks = books.Where(b => b.AvailableQuantity <= 0).ToList();
            if (unavailableBooks.Any())
            {
                return BadRequest(new
                {
                    message = "Some books are no longer available",
                    unavailableBooks = unavailableBooks.Select(b => new { b.Id, b.Title, b.AvailableQuantity })
                });
            }

            // Create loan
            var loan = new Loan
            {
                ReaderCardId = request.ReaderCardId,
                BorrowDate = DateTime.UtcNow,
                DueDate = request.CalculatedDueDate,
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

            // Update request status
            request.Status = "Approved";
            request.ProcessedAt = DateTime.UtcNow;
            request.ProcessedByUserId = adminUserId;
            request.LoanId = loan.Id;

            await _context.SaveChangesAsync();

            // Send approval email (non-blocking - don't fail the approval if email fails)
            try
            {
                var bookTitles = books.Select(b => b.Title).ToList();
                var emailSent = await _emailService.SendBorrowApprovalEmailAsync(
                    request.ReaderCard,
                    bookTitles,
                    loan.BorrowDate,
                    loan.DueDate);
                
                if (emailSent)
                {
                    Console.WriteLine($"✅ Approval email sent successfully to {request.ReaderCard.User.Email} for request {id}");
                }
                else
                {
                    Console.WriteLine($"⚠️ Failed to send approval email to {request.ReaderCard.User.Email} for request {id}");
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the approval
                // Email can be sent later if needed
                Console.WriteLine($"❌ Error sending approval email to {request.ReaderCard.User?.Email} for request {id}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            return Ok(new
            {
                message = "Borrow request approved successfully. Email notification has been sent to the reader.",
                loanId = loan.Id,
                requestId = request.Id
            });
        }

        [HttpPost("{id}/reject")]
        public async Task<ActionResult> RejectBorrowRequest(int id, [FromBody] RejectBorrowRequestDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int adminUserId))
            {
                return Unauthorized();
            }

            var request = await _context.BorrowRequests
                .Include(br => br.ReaderCard)
                    .ThenInclude(rc => rc.User)
                .FirstOrDefaultAsync(br => br.Id == id);

            if (request == null)
            {
                return NotFound(new { message = "Borrow request not found" });
            }

            if (request.Status != "Pending")
            {
                return BadRequest(new { message = $"Request is already {request.Status}" });
            }

            var bookIds = JsonSerializer.Deserialize<List<int>>(request.BookIds) ?? new List<int>();
            var books = await _context.Books
                .Where(b => bookIds.Contains(b.Id))
                .ToListAsync();

            // Update request status
            request.Status = "Rejected";
            request.RejectionReason = dto.Reason;
            request.ProcessedAt = DateTime.UtcNow;
            request.ProcessedByUserId = adminUserId;

            await _context.SaveChangesAsync();

            // Send rejection email
            var bookTitles = books.Select(b => b.Title).ToList();
            await _emailService.SendBorrowRejectionEmailAsync(
                request.ReaderCard,
                bookTitles,
                dto.Reason);

            return Ok(new
            {
                message = "Borrow request rejected",
                requestId = request.Id
            });
        }
    }
}

