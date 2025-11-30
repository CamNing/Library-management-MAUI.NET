namespace book.Models
{
    public class UpdateBookRequest
    {
        public string Title { get; set; } = string.Empty;
        public string ManagementCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public int? PublishedYear { get; set; }
        public string? CoverImageUrl { get; set; }
        public int TotalQuantity { get; set; }
        public List<string> Authors { get; set; } = new();
    }
}

