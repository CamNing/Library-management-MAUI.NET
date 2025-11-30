using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryAPI.Models
{
    public class EmailVerificationCode
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ReaderCardId { get; set; }

        [Required]
        [StringLength(10)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Type { get; set; } = string.Empty; // Borrow or Return

        public int? RelatedLoanId { get; set; } // For return operations
        
        // Store borrow request details as JSON string
        public string? RequestData { get; set; } // JSON serialized borrow/return request

        public DateTime ExpiresAt { get; set; }

        public bool IsUsed { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ReaderCardId")]
        public virtual ReaderCard ReaderCard { get; set; } = null!;
    }
}

