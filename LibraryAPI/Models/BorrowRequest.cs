using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryAPI.Models
{
    public class BorrowRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ReaderCardId { get; set; }

        [Required]
        public string BookIds { get; set; } = string.Empty; // JSON array of book IDs

        [Required]
        public int LoanDays { get; set; } = 14;

        public DateTime? CustomDueDate { get; set; }

        public DateTime CalculatedDueDate { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public string? RejectionReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessedAt { get; set; }

        public int? ProcessedByUserId { get; set; }

        public int? LoanId { get; set; } // If approved, link to the created loan

        // Navigation properties
        [ForeignKey("ReaderCardId")]
        public virtual ReaderCard ReaderCard { get; set; } = null!;

        [ForeignKey("ProcessedByUserId")]
        public virtual User? ProcessedByUser { get; set; }

        [ForeignKey("LoanId")]
        public virtual Loan? Loan { get; set; }
    }
}

