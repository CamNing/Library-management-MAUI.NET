namespace book.Models
{
    public class Loan
    {
        public int Id { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<LoanItem> Items { get; set; } = new();
    }

    public class LoanItem
    {
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ManagementCode { get; set; } = string.Empty;
        public List<string> Authors { get; set; } = new();
        public int Quantity { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class LoanDisplay
    {
        public string Title { get; set; } = string.Empty;
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Authors { get; set; } = string.Empty;
        public string ManagementCode { get; set; } = string.Empty;
        public string? CoverImageUrl { get; set; }
    }
}

