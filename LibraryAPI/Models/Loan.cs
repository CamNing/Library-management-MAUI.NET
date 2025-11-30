using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryAPI.Models
{
    public class Loan
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ReaderCardId { get; set; }

        public DateTime BorrowDate { get; set; } = DateTime.UtcNow;

        public DateTime DueDate { get; set; }

        public DateTime? ReturnDate { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Borrowed"; // Borrowed, Returned, Overdue

        // Navigation properties
        [ForeignKey("ReaderCardId")]
        public virtual ReaderCard ReaderCard { get; set; } = null!;
        public virtual ICollection<LoanItem> LoanItems { get; set; } = new List<LoanItem>();
    }
}

