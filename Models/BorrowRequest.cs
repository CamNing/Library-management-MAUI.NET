namespace book.Models
{
    public class BorrowRequest
    {
        public string ReaderCardCode { get; set; } = string.Empty;
        public List<int> BookIds { get; set; } = new();
        public int LoanDays { get; set; } = 14;
        public DateTime? CustomDueDate { get; set; }
    }

    public class ConfirmBorrowRequest
    {
        public int VerificationCodeId { get; set; }
        public string Code { get; set; } = string.Empty;
    }

    public class ReturnRequest
    {
        public string ReaderCardCode { get; set; } = string.Empty;
        public List<int> LoanItemIds { get; set; } = new();
    }

    public class ConfirmReturnRequest
    {
        public int VerificationCodeId { get; set; }
        public string Code { get; set; } = string.Empty;
    }

    public class ReaderBorrowRequest
    {
        public List<int> BookIds { get; set; } = new();
        public int LoanDays { get; set; } = 14; // Default 14 days
        public DateTime? CustomDueDate { get; set; }
    }

    public class BorrowRequestResponse
    {
        public string Message { get; set; } = string.Empty;
        public int VerificationCodeId { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public class ReturnRequestResponse
    {
        public string Message { get; set; } = string.Empty;
        public int VerificationCodeId { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}

