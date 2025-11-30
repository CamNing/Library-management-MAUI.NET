using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Models;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using System.Security.Cryptography;

namespace LibraryAPI.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly LibraryDbContext _context;
        private readonly EmailService _emailService;

        public AdminController(LibraryDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        #region User Management

        [HttpPost("users")]
        public async Task<ActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                return BadRequest(new { message = "Username already exists" });
            }

            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest(new { message = "Email already exists" });
            }

            var user = new User
            {
                Username = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Email = request.Email,
                Role = request.Role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // If role is Reader, create ReaderCard
            if (request.Role == "Reader")
            {
                var cardCode = GenerateCardCode();
                var readerCard = new ReaderCard
                {
                    UserId = user.Id,
                    CardCode = cardCode,
                    FullName = request.FullName ?? request.Username,
                    Phone = request.Phone,
                    Address = request.Address,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ReaderCards.Add(readerCard);
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "User created successfully", userId = user.Id });
        }

        [HttpGet("users")]
        public async Task<ActionResult> GetUsers()
        {
            var users = await _context.Users
                .Include(u => u.ReaderCard)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Role,
                    u.IsActive,
                    u.CreatedAt,
                    ReaderCardCode = u.ReaderCard != null ? u.ReaderCard.CardCode : null
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPut("users/{id}/reset-password")]
        public async Task<ActionResult> ResetPassword(int id, [FromBody] string newPassword)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password reset successfully" });
        }

        [HttpPut("users/{id}/toggle-active")]
        public async Task<ActionResult> ToggleUserActive(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"User {(user.IsActive ? "activated" : "deactivated")} successfully" });
        }

        [HttpPost("users/{id}/create-reader-card")]
        public async Task<ActionResult> CreateReaderCardForUser(int id, [FromBody] DTOs.CreateReaderCardRequest? request = null)
        {
            var user = await _context.Users
                .Include(u => u.ReaderCard)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            if (user.Role != "Reader")
            {
                return BadRequest(new { message = "User is not a Reader" });
            }

            if (user.ReaderCard != null)
            {
                return BadRequest(new { message = "User already has a reader card" });
            }

            var cardCode = GenerateCardCode();
            var readerCard = new ReaderCard
            {
                UserId = user.Id,
                CardCode = cardCode,
                FullName = request?.FullName ?? user.Username,
                Phone = request?.Phone,
                Address = request?.Address,
                CreatedAt = DateTime.UtcNow
            };

            _context.ReaderCards.Add(readerCard);
            await _context.SaveChangesAsync();

            return Ok(new 
            { 
                message = "Reader card created successfully", 
                cardCode = readerCard.CardCode,
                readerCardId = readerCard.Id
            });
        }

        [HttpPost("users/create-missing-reader-cards")]
        public async Task<ActionResult> CreateMissingReaderCards()
        {
            var readersWithoutCards = await _context.Users
                .Include(u => u.ReaderCard)
                .Where(u => u.Role == "Reader" && u.ReaderCard == null)
                .ToListAsync();

            var created = 0;
            foreach (var user in readersWithoutCards)
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
                created++;
            }

            await _context.SaveChangesAsync();

            return Ok(new 
            { 
                message = $"Created {created} reader card(s) for users without cards",
                created = created
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

        #endregion

        #region Reader Card Lookup

        [HttpGet("readers/{cardCode}")]
        public async Task<ActionResult<ReaderCardDto>> GetReaderByCard(string cardCode)
        {
            var readerCard = await _context.ReaderCards
                .Include(rc => rc.User)
                .Include(rc => rc.Loans)
                    .ThenInclude(l => l.LoanItems)
                        .ThenInclude(li => li.Book)
                .FirstOrDefaultAsync(rc => rc.CardCode == cardCode);

            if (readerCard == null)
            {
                return NotFound(new { message = "Reader card not found" });
            }

            var loanHistory = readerCard.Loans.Select(l => new LoanHistoryDto
            {
                LoanId = l.Id,
                BorrowDate = l.BorrowDate,
                DueDate = l.DueDate,
                ReturnDate = l.ReturnDate,
                Status = l.Status,
                Items = l.LoanItems.Select(li => new LoanItemDto
                {
                    Id = li.Id,
                    BookId = li.BookId,
                    BookTitle = li.Book.Title,
                    ManagementCode = li.Book.ManagementCode,
                    Quantity = li.Quantity,
                    Status = li.Status
                }).ToList()
            }).ToList();

            var dto = new ReaderCardDto
            {
                Id = readerCard.Id,
                CardCode = readerCard.CardCode,
                FullName = readerCard.FullName,
                Email = readerCard.User.Email,
                Phone = readerCard.Phone,
                Address = readerCard.Address,
                CreatedAt = readerCard.CreatedAt,
                LoanHistory = loanHistory
            };

            return Ok(dto);
        }

        #endregion
    }
}

