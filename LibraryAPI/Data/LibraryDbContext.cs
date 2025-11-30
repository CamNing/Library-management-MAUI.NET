using LibraryAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryAPI.Data
{
    public class LibraryDbContext : DbContext
    {
        public LibraryDbContext(DbContextOptions<LibraryDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<ReaderCard> ReaderCards { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<BookAuthor> BookAuthors { get; set; }
        public DbSet<Loan> Loans { get; set; }
        public DbSet<LoanItem> LoanItems { get; set; }
        public DbSet<EmailVerificationCode> EmailVerificationCodes { get; set; }
        public DbSet<BorrowRequest> BorrowRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure unique constraints
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<ReaderCard>()
                .HasIndex(r => r.CardCode)
                .IsUnique();

            modelBuilder.Entity<ReaderCard>()
                .HasIndex(r => r.UserId)
                .IsUnique();

            modelBuilder.Entity<Book>()
                .HasIndex(b => b.ManagementCode)
                .IsUnique();

            // Configure relationships
            modelBuilder.Entity<ReaderCard>()
                .HasOne(r => r.User)
                .WithOne(u => u.ReaderCard)
                .HasForeignKey<ReaderCard>(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BookAuthor>()
                .HasOne(ba => ba.Book)
                .WithMany(b => b.BookAuthors)
                .HasForeignKey(ba => ba.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BookAuthor>()
                .HasOne(ba => ba.Author)
                .WithMany(a => a.BookAuthors)
                .HasForeignKey(ba => ba.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ensure unique Book-Author combination
            modelBuilder.Entity<BookAuthor>()
                .HasIndex(ba => new { ba.BookId, ba.AuthorId })
                .IsUnique();

            // Configure BorrowRequest relationships
            modelBuilder.Entity<BorrowRequest>()
                .HasOne(br => br.ReaderCard)
                .WithMany()
                .HasForeignKey(br => br.ReaderCardId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BorrowRequest>()
                .HasOne(br => br.ProcessedByUser)
                .WithMany()
                .HasForeignKey(br => br.ProcessedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<BorrowRequest>()
                .HasOne(br => br.Loan)
                .WithMany()
                .HasForeignKey(br => br.LoanId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}

