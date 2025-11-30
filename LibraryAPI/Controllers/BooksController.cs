using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly LibraryDbContext _context;

        public BooksController(LibraryDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetBooks(
            [FromQuery] string? search,
            [FromQuery] string? category,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.Books
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower().Trim();
                
                // Fulltext search: tìm kiếm trong Title, ManagementCode, Description, và Authors
                // Hỗ trợ tìm kiếm nhiều từ khóa (OR logic - bất kỳ từ khóa nào match)
                var searchTerms = searchLower
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                if (searchTerms.Count > 1)
                {
                    // Nếu có nhiều từ khóa, tìm sách có bất kỳ từ khóa nào match
                    query = query.Where(b =>
                        searchTerms.Any(term =>
                            b.Title.ToLower().Contains(term) ||
                            b.ManagementCode.ToLower().Contains(term) ||
                            (b.Description != null && b.Description.ToLower().Contains(term)) ||
                            b.BookAuthors.Any(ba => ba.Author.Name.ToLower().Contains(term))
                        )
                    );
                }
                else
                {
                    // Nếu chỉ có một từ khóa, tìm trong tất cả các trường
                    query = query.Where(b =>
                        b.Title.ToLower().Contains(searchLower) ||
                        b.ManagementCode.ToLower().Contains(searchLower) ||
                        (b.Description != null && b.Description.ToLower().Contains(searchLower)) ||
                        b.BookAuthors.Any(ba => ba.Author.Name.ToLower().Contains(searchLower))
                    );
                }
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(b => b.Category == category);
            }

            var totalCount = await query.CountAsync();
            var books = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var bookDtos = books.Select(b => new BookDto
            {
                Id = b.Id,
                Title = b.Title,
                ManagementCode = b.ManagementCode,
                Description = b.Description,
                Category = b.Category,
                PublishedYear = b.PublishedYear,
                CoverImageUrl = b.CoverImageUrl,
                TotalQuantity = b.TotalQuantity,
                AvailableQuantity = b.AvailableQuantity,
                Authors = b.BookAuthors.Select(ba => ba.Author.Name).ToList(),
                CreatedAt = b.CreatedAt,
                ViewCount = b.ViewCount
            }).ToList();

            return Ok(new
            {
                data = bookDtos,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BookDto>> GetBook(int id)
        {
            var book = await _context.Books
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null)
            {
                return NotFound();
            }

            // Increment view count
            book.ViewCount++;
            await _context.SaveChangesAsync();

            var dto = new BookDto
            {
                Id = book.Id,
                Title = book.Title,
                ManagementCode = book.ManagementCode,
                Description = book.Description,
                Category = book.Category,
                PublishedYear = book.PublishedYear,
                CoverImageUrl = book.CoverImageUrl,
                TotalQuantity = book.TotalQuantity,
                AvailableQuantity = book.AvailableQuantity,
                Authors = book.BookAuthors.Select(ba => ba.Author.Name).ToList(),
                CreatedAt = book.CreatedAt,
                ViewCount = book.ViewCount
            };

            return Ok(dto);
        }

        [HttpGet("popular")]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetPopularBooks([FromQuery] int limit = 10)
        {
            var books = await _context.LoanItems
                .GroupBy(li => li.BookId)
                .OrderByDescending(g => g.Count())
                .Take(limit)
                .Select(g => g.Key)
                .ToListAsync();

            var bookList = await _context.Books
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .Where(b => books.Contains(b.Id))
                .ToListAsync();

            var bookDtos = bookList.Select(b => new BookDto
            {
                Id = b.Id,
                Title = b.Title,
                ManagementCode = b.ManagementCode,
                Description = b.Description,
                Category = b.Category,
                PublishedYear = b.PublishedYear,
                CoverImageUrl = b.CoverImageUrl,
                TotalQuantity = b.TotalQuantity,
                AvailableQuantity = b.AvailableQuantity,
                Authors = b.BookAuthors.Select(ba => ba.Author.Name).ToList(),
                CreatedAt = b.CreatedAt,
                ViewCount = b.ViewCount
            }).ToList();

            return Ok(bookDtos);
        }

        [HttpGet("new")]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetNewBooks([FromQuery] int limit = 10)
        {
            var books = await _context.Books
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .OrderByDescending(b => b.CreatedAt)
                .Take(limit)
                .ToListAsync();

            var bookDtos = books.Select(b => new BookDto
            {
                Id = b.Id,
                Title = b.Title,
                ManagementCode = b.ManagementCode,
                Description = b.Description,
                Category = b.Category,
                PublishedYear = b.PublishedYear,
                CoverImageUrl = b.CoverImageUrl,
                TotalQuantity = b.TotalQuantity,
                AvailableQuantity = b.AvailableQuantity,
                Authors = b.BookAuthors.Select(ba => ba.Author.Name).ToList(),
                CreatedAt = b.CreatedAt,
                ViewCount = b.ViewCount
            }).ToList();

            return Ok(bookDtos);
        }

        [HttpGet("most-accessed")]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetMostAccessedBooks([FromQuery] int limit = 10)
        {
            var books = await _context.Books
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .OrderByDescending(b => b.ViewCount)
                .Take(limit)
                .ToListAsync();

            var bookDtos = books.Select(b => new BookDto
            {
                Id = b.Id,
                Title = b.Title,
                ManagementCode = b.ManagementCode,
                Description = b.Description,
                Category = b.Category,
                PublishedYear = b.PublishedYear,
                CoverImageUrl = b.CoverImageUrl,
                TotalQuantity = b.TotalQuantity,
                AvailableQuantity = b.AvailableQuantity,
                Authors = b.BookAuthors.Select(ba => ba.Author.Name).ToList(),
                CreatedAt = b.CreatedAt,
                ViewCount = b.ViewCount
            }).ToList();

            return Ok(bookDtos);
        }
    }
}

