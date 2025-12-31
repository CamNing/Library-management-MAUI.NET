namespace book.Models
{
    public class BorrowRequest
    {
        public string ReaderCardCode { get; set; } = string.Empty;
        public List<int> BookIds { get; set; } = new();
        public int LoanDays { get; set; } = 14;
        public DateTime? CustomDueDate { get; set; }
        public string Status { get; set; }
        public bool IsPending => Status == "Pending";
        public bool IsApproved => Status == "Approved";
        public string StatusColor
        {
            get
            {
                if (Status == "Pending") return "#F59E0B"; // Vàng
                if (Status == "Approved") return "#3B82F6"; // Xanh dương (Đã hẹn)
                if (Status == "PickedUp") return "#10B981"; // Xanh lá (Đã lấy)
                return "#EF4444"; // Đỏ (Hủy/Từ chối)
            }
        }

        public string StatusText
        {
            get
            {
                if (Status == "Pending") return "Chờ duyệt";
                if (Status == "Approved") return "Chờ lấy sách"; // Khách chưa đến
                if (Status == "PickedUp") return "Hoàn tất";
                return "Từ chối";
            }
        }
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

