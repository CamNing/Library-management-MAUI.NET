using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using System.Text.Json.Serialization;

namespace LibraryAPI.Models
{
    public class Book
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string ManagementCode { get; set; } = string.Empty; // Unique library code

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

        public int? PublishedYear { get; set; }

        [StringLength(500)]
        public string? CoverImageUrl { get; set; }

        public int TotalQuantity { get; set; } = 1;

        public int AvailableQuantity { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int ViewCount { get; set; } = 0; // For tracking most accessed books
        [JsonIgnore]
        public string? UnsignedSearchText { get; set; }

        // Navigation properties
        public virtual ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
        public virtual ICollection<LoanItem> LoanItems { get; set; } = new List<LoanItem>();
    }
}

