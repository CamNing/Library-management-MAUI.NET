namespace LibraryAPI.DTOs
{
    public class BorrowRequest
    {
        public string ReaderCardCode { get; set; } = string.Empty;
        public List<int> BookIds { get; set; } = new();
        public int LoanDays { get; set; } = 14; // Default 14 days
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
        public List<int> LoanItemIds { get; set; } = new(); // IDs of LoanItems to return
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
}

