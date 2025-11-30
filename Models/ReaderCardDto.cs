namespace book.Models
{
    public class ReaderCardDto
    {
        public int Id { get; set; }
        public string CardCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<LoanHistoryDto> LoanHistory { get; set; } = new();
    }

    public class LoanHistoryDto
    {
        public int LoanId { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<LoanItemDto> Items { get; set; } = new();
    }

    public class LoanItemDto
    {
        public int BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string ManagementCode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Status { get; set; } = string.Empty;
        public int Id { get; set; } // Add Id for loan item selection
    }
}

