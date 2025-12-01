namespace LibraryAPI.DTOs
{
    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    public class ChatResponse
    {
        public string Response { get; set; } = string.Empty;
        // Có thể thêm danh sách sách gợi ý nếu cần
        public List<BookDto>? SuggestedBooks { get; set; }
    }
}
