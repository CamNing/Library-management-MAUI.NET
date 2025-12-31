using System.Text.Json.Serialization;

namespace book.Models
{
    public class RestockSuggestion
    {
        public int BookId { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public int CurrentStock { get; set; }
        public int BorrowCountLastMonth { get; set; }
        public string Suggestion { get; set; }
        public double DemandRatio { get; set; }

        // Property phụ để hiển thị màu sắc trên UI
        public string ColorCode => DemandRatio > 2.0 ? "#EF4444" : "#F59E0B"; // Đỏ nếu rất gấp, Vàng nếu cảnh báo
    }

    public class UserRisk
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public int LateReturnCount { get; set; }
        public double RiskScore { get; set; }
        public string RiskLevel { get; set; }

        // Property phụ cho UI
        public string LevelColor => RiskLevel == "Cao" ? "#EF4444" : "#F59E0B";
    }

    public class DashboardAnalytics
    {
        public List<RestockSuggestion> RestockSuggestions { get; set; } = new();
        public List<UserRisk> HighRiskUsers { get; set; } = new();
        public int TotalLoansThisMonth { get; set; }
        public double GrowthRate { get; set; }
    }
    public class TopBookStat
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public int BorrowCount { get; set; }

        // Thuộc tính hỗ trợ vẽ biểu đồ
        public double BarWidth { get; set; } // Chiều dài thanh
        public string BarColor { get; set; } = "#6C63FF";
    }

    public class TopUserStat
    {
        public string Username { get; set; }
        public string FullName { get; set; }
        public int BorrowCount { get; set; }

        // Thuộc tính hỗ trợ UI
        public string RankColor { get; set; } // Màu huy chương (Vàng, Bạc, Đồng)
    }

    public class MonthlyStat
    {
        public string MonthLabel { get; set; }
        public int LoanCount { get; set; }

        // Thuộc tính hỗ trợ biểu đồ
        public double BarHeight { get; set; }
        public string BarColor { get; set; }
    }

    public class DetailedReport
    {
        public List<TopBookStat> TopBooks { get; set; } = new();
        public List<TopUserStat> TopUsers { get; set; } = new();
        public List<MonthlyStat> MonthlyStats { get; set; } = new();
    }
}