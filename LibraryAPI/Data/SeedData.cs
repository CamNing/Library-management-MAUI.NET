using LibraryAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryAPI.Data
{
    public static class SeedData
    {
        public static async Task SeedAsync(LibraryDbContext context)
        {
            // Check if data already exists
            if (await context.Users.AnyAsync())
            {
                return;
            }

            // Create default Admin users for testing
            var adminUser = new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Email = "admin@library.com",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(adminUser);

            // Add another admin for testing
            var adminUser2 = new User
            {
                Username = "admin2",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Email = "admin2@library.com",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(adminUser2);

            // Create sample Reader users
            var reader1 = new User
            {
                Username = "reader1",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("reader123"),
                Email = "reader1@library.com",
                Role = "Reader",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(reader1);

            var reader2 = new User
            {
                Username = "reader2",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("reader123"),
                Email = "reader2@library.com",
                Role = "Reader",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(reader2);

            await context.SaveChangesAsync();

            // Create Reader Cards
            var readerCard1 = new ReaderCard
            {
                UserId = reader1.Id,
                CardCode = "RC001001",
                FullName = "John Doe",
                Phone = "123-456-7890",
                Address = "123 Main St",
                CreatedAt = DateTime.UtcNow
            };
            context.ReaderCards.Add(readerCard1);

            var readerCard2 = new ReaderCard
            {
                UserId = reader2.Id,
                CardCode = "RC001002",
                FullName = "Jane Smith",
                Phone = "098-765-4321",
                Address = "456 Oak Ave",
                CreatedAt = DateTime.UtcNow
            };
            context.ReaderCards.Add(readerCard2);

            await context.SaveChangesAsync();

            // Create Authors
            var author1 = new Author { Name = "J.K. Rowling" };
            var author2 = new Author { Name = "George R.R. Martin" };
            var author3 = new Author { Name = "Stephen King" };
            var author4 = new Author { Name = "Agatha Christie" };
            var author5 = new Author { Name = "Isaac Asimov" };

            context.Authors.AddRange(author1, author2, author3, author4, author5);
            await context.SaveChangesAsync();

            // Create Books
            var books = new List<Book>
            {
                new Book
                {
                    Title = "Harry Potter and the Philosopher's Stone",
                    ManagementCode = "LIB-001",
                    Description = "The first book in the Harry Potter series",
                    Category = "Fantasy",
                    PublishedYear = 1997,
                    TotalQuantity = 5,
                    AvailableQuantity = 5,
                    CreatedAt = DateTime.UtcNow
                },
                new Book
                {
                    Title = "A Game of Thrones",
                    ManagementCode = "LIB-002",
                    Description = "The first book in A Song of Ice and Fire series",
                    Category = "Fantasy",
                    PublishedYear = 1996,
                    TotalQuantity = 3,
                    AvailableQuantity = 3,
                    CreatedAt = DateTime.UtcNow
                },
                new Book
                {
                    Title = "The Shining",
                    ManagementCode = "LIB-003",
                    Description = "A horror novel about a haunted hotel",
                    Category = "Horror",
                    PublishedYear = 1977,
                    TotalQuantity = 4,
                    AvailableQuantity = 4,
                    CreatedAt = DateTime.UtcNow
                },
                new Book
                {
                    Title = "Murder on the Orient Express",
                    ManagementCode = "LIB-004",
                    Description = "A classic mystery novel",
                    Category = "Mystery",
                    PublishedYear = 1934,
                    TotalQuantity = 6,
                    AvailableQuantity = 6,
                    CreatedAt = DateTime.UtcNow
                },
                new Book
                {
                    Title = "Foundation",
                    ManagementCode = "LIB-005",
                    Description = "First book in the Foundation series",
                    Category = "Science Fiction",
                    PublishedYear = 1951,
                    TotalQuantity = 4,
                    AvailableQuantity = 4,
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.Books.AddRange(books);
            await context.SaveChangesAsync();

            // Create Book-Author relationships
            var bookAuthors = new List<BookAuthor>
            {
                new BookAuthor { BookId = books[0].Id, AuthorId = author1.Id },
                new BookAuthor { BookId = books[1].Id, AuthorId = author2.Id },
                new BookAuthor { BookId = books[2].Id, AuthorId = author3.Id },
                new BookAuthor { BookId = books[3].Id, AuthorId = author4.Id },
                new BookAuthor { BookId = books[4].Id, AuthorId = author5.Id }
            };

            context.BookAuthors.AddRange(bookAuthors);
            await context.SaveChangesAsync();
        }
    }
}

