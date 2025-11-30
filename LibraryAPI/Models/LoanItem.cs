using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryAPI.Models
{
    public class LoanItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int LoanId { get; set; }

        [Required]
        public int BookId { get; set; }

        public int Quantity { get; set; } = 1;

        [StringLength(20)]
        public string Status { get; set; } = "Borrowed"; // Borrowed, Returned, Overdue

        // Navigation properties
        [ForeignKey("LoanId")]
        public virtual Loan Loan { get; set; } = null!;

        [ForeignKey("BookId")]
        public virtual Book Book { get; set; } = null!;
    }
}

