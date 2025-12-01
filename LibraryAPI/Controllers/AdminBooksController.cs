using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryAPI.Helpers;

namespace LibraryAPI.Controllers
{
    [ApiController]
    [Route("api/admin/books")]
    [Authorize(Roles = "Admin")]
    public class AdminBooksController : ControllerBase
    {
        private readonly LibraryDbContext _context;

        public AdminBooksController(LibraryDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult<BookDto>> CreateBook([FromBody] CreateBookRequest request)
        {
            if (await _context.Books.AnyAsync(b => b.ManagementCode == request.ManagementCode))
            {
                return BadRequest(new { message = "Management code already exists" });
            }

            var authorString = string.Join(" ", request.Authors);
            var rawSearchText = $"{request.Title} {request.ManagementCode} {request.Description} {authorString}";
            var book = new Book
            {
                Title = request.Title,
                ManagementCode = request.ManagementCode,
                Description = request.Description,
                Category = request.Category,
                PublishedYear = request.PublishedYear,
                CoverImageUrl = request.CoverImageUrl,
                TotalQuantity = request.TotalQuantity,
                AvailableQuantity = request.TotalQuantity,
                CreatedAt = DateTime.UtcNow,
                UnsignedSearchText = StringUtils.ConvertToUnSign(rawSearchText)
            };


            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            // Handle authors
            foreach (var authorName in request.Authors)
            {
                var author = await _context.Authors.FirstOrDefaultAsync(a => a.Name == authorName);
                if (author == null)
                {
                    author = new Author { Name = authorName };
                    _context.Authors.Add(author);
                    await _context.SaveChangesAsync();
                }

                var bookAuthor = new BookAuthor
                {
                    BookId = book.Id,
                    AuthorId = author.Id
                };
                _context.BookAuthors.Add(bookAuthor);
            }

            await _context.SaveChangesAsync();

            // Reload with authors
            await _context.Entry(book).Collection(b => b.BookAuthors).LoadAsync();
            foreach (var ba in book.BookAuthors)
            {
                await _context.Entry(ba).Reference(b => b.Author).LoadAsync();
            }

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

            return CreatedAtAction(nameof(GetBook), new { id = book.Id }, dto);
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

        [HttpPut("{id}")]
        public async Task<ActionResult<BookDto>> UpdateBook(int id, [FromBody] UpdateBookRequest request)
        {
            var book = await _context.Books
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .FirstOrDefaultAsync(b => b.Id == id);


            if (book == null)
            {
                return NotFound();
            }

            // Check if management code is changed and if it conflicts
            if (book.ManagementCode != request.ManagementCode &&
                await _context.Books.AnyAsync(b => b.ManagementCode == request.ManagementCode && b.Id != id))
            {
                return BadRequest(new { message = "Management code already exists" });
            }
            var authorString = string.Join(" ", request.Authors);
            var rawSearchText = $"{request.Title} {request.ManagementCode} {request.Description} {authorString}";

            // Update book properties
            book.Title = request.Title;
            book.ManagementCode = request.ManagementCode;
            book.Description = request.Description;
            book.Category = request.Category;
            book.PublishedYear = request.PublishedYear;
            book.CoverImageUrl = request.CoverImageUrl;
            book.UnsignedSearchText = StringUtils.ConvertToUnSign(rawSearchText);

            // Adjust available quantity if total quantity changed
            var quantityDiff = request.TotalQuantity - book.TotalQuantity;
            book.TotalQuantity = request.TotalQuantity;
            book.AvailableQuantity = Math.Max(0, book.AvailableQuantity + quantityDiff);

            // Update authors
            _context.BookAuthors.RemoveRange(book.BookAuthors);
            await _context.SaveChangesAsync();

            foreach (var authorName in request.Authors)
            {
                var author = await _context.Authors.FirstOrDefaultAsync(a => a.Name == authorName);
                if (author == null)
                {
                    author = new Author { Name = authorName };
                    _context.Authors.Add(author);
                    await _context.SaveChangesAsync();
                }

                var bookAuthor = new BookAuthor
                {
                    BookId = book.Id,
                    AuthorId = author.Id
                };
                _context.BookAuthors.Add(bookAuthor);
            }

            await _context.SaveChangesAsync();

            // Reload with authors
            await _context.Entry(book).Collection(b => b.BookAuthors).LoadAsync();
            foreach (var ba in book.BookAuthors)
            {
                await _context.Entry(ba).Reference(b => b.Author).LoadAsync();
            }

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

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }

            // Check if book has active loans
            var hasActiveLoans = await _context.LoanItems
                .AnyAsync(li => li.BookId == id && li.Status != "Returned");

            if (hasActiveLoans)
            {
                return BadRequest(new { message = "Cannot delete book with active loans" });
            }

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

