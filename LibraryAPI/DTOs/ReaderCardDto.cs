namespace LibraryAPI.DTOs
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
        public int Id { get; set; } // LoanItem ID
        public int BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string ManagementCode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class OverdueDetailDto
    {
        public int LoanId { get; set; }
        public int ReaderCardId { get; set; }
        public string ReaderName { get; set; } = string.Empty;
        public string ReaderCardCode { get; set; } = string.Empty;
        public string ReaderEmail { get; set; } = string.Empty;
        public string? ReaderPhone { get; set; }
        public string? ReaderAddress { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string BookManagementCode { get; set; } = string.Empty;
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public int DaysOverdue { get; set; } // Số ngày quá hạn (âm nếu chưa quá hạn)
        public int DaysRemaining { get; set; } // Số ngày còn lại (dương nếu chưa quá hạn)
        public int Quantity { get; set; }
    }
}

