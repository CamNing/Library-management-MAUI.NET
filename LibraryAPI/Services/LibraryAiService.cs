using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LibraryAPI.Services
{
    public class LibraryAiService
    {
        private readonly LibraryDbContext _context;
        private readonly string _apiKey = "AIzaSyDEdM2gGg7Wf55SbHURBr5iRrq935bKc6c";
        private readonly HttpClient _httpClient;

        public LibraryAiService(LibraryDbContext context, IConfiguration configuration)
        {
            _context = context;

            _httpClient = new HttpClient();
        }

        public async Task<ChatResponse> ProcessQueryAsync(string message, int userId)
        {
            // --- CHIẾN THUẬT MỚI: Gửi toàn bộ menu sách cho AI chọn ---

            // 1. Lấy danh sách sách từ Database (Lấy khoảng 50-100 cuốn mới nhất/phổ biến nhất)
            // Vì đây là demo/project nhỏ, lấy 100 cuốn là AI đọc thoải mái.
            var allBooks = await _context.Books
                .AsNoTracking()
                .OrderByDescending(b => b.ViewCount) // Ưu tiên sách hay xem
                .Take(100)
                .Select(b => new
                {
                    b.Id,
                    b.Title,
                    b.AvailableQuantity,
                    b.TotalQuantity,
                    Authors = b.BookAuthors.Select(a => a.Author.Name)
                })
                .ToListAsync();

            // 2. Lấy thông tin người dùng (để trả lời câu hỏi "của tôi")
            var userLoanInfo = "";
            var user = await _context.Users.Include(u => u.ReaderCard).FirstOrDefaultAsync(u => u.Id == userId);
            if (user?.ReaderCard != null)
            {
                var loans = await _context.Loans
                    .Include(l => l.LoanItems).ThenInclude(li => li.Book)
                    .Where(l => l.ReaderCardId == user.ReaderCard.Id && l.Status != "Returned")
                    .SelectMany(l => l.LoanItems.Where(li => li.Status != "Returned").Select(li => li.Book.Title))
                    .ToListAsync();

                if (loans.Any())
                    userLoanInfo = $"Sách người dùng đang mượn: {string.Join(", ", loans)}";
            }

            // 3. Xây dựng Prompt (Kịch bản) gửi cho Gemini
            var contextData = JsonSerializer.Serialize(allBooks);

            var prompt = $@"
                Bạn là thủ thư thông minh. Dưới đây là dữ liệu thực tế trong kho sách:
                --- DANH SÁCH SÁCH TRONG KHO ---
                {contextData}
                -------------------------------
                {userLoanInfo}
                
                Yêu cầu xử lý:
                1. Dựa vào danh sách trên để trả lời câu hỏi. Tuyệt đối không bịa ra sách không có trong danh sách.
                2. Nếu người dùng tìm sách (ví dụ 'tìm sách A', 'sách A còn không', 'A thì sao'), hãy nhìn vào danh sách trên. Nếu thấy sách có tên gần giống hoặc liên quan, hãy giới thiệu nó.
                3. QUAN TRỌNG: Khi giới thiệu một cuốn sách cụ thể có trong danh sách, BẮT BUỘC phải chèn mã [BOOK:ID] vào cuối câu để hiện nút bấm.
                   Ví dụ: 'Thư viện có cuốn Harry Potter nhé [BOOK:1]'.
                4. Nếu sách có AvailableQuantity > 0 thì báo còn, = 0 thì báo tạm hết.
                
                Câu hỏi của khách: ""{message}""
            ";

            // 4. Gọi AI
            var aiResponseText = await CallGeminiApi(prompt);

            // 5. Xử lý kết quả để tạo nút bấm
            var response = new ChatResponse { Response = aiResponseText };

            // Tìm mã [BOOK:ID] để gán object sách vào response
            var match = System.Text.RegularExpressions.Regex.Match(aiResponseText, @"\[BOOK:(\d+)\]");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int bookId))
            {
                response.Response = aiResponseText.Replace(match.Value, "").Trim(); // Xóa mã ID khỏi lời thoại cho đẹp

                var book = await _context.Books.FindAsync(bookId);
                if (book != null)
                {
                    response.SuggestedBooks = new List<BookDto>
                    {
                        new BookDto { Id = book.Id, Title = book.Title, CoverImageUrl = book.CoverImageUrl }
                    };
                }
            }

            return response;
        }

        private async Task<string> CallGeminiApi(string prompt)
        {
            if (string.IsNullOrEmpty(_apiKey) || _apiKey.Contains("PASTE_YOUR"))
            {
                return "Lỗi: API Key chưa được cấu hình.";
            }

            // 1. Thử dùng model chuẩn quốc tế hiện nay (Gemini 1.5 Flash trên v1beta)
            var modelName = "gemini-2.5-pro";
            var apiVersion = "v1beta";
            var url = $"https://generativelanguage.googleapis.com/{apiVersion}/models/{modelName}:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } }
            };

            try
            {
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var result = await _httpClient.PostAsync(url, content);

                if (result.IsSuccessStatusCode)
                {
                    // THÀNH CÔNG: Xử lý kết quả bình thường
                    var jsonResponse = await result.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonResponse);
                    if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                    {
                        return candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString()
                               ?? "AI trả lời rỗng.";
                    }
                    return "AI không có phản hồi.";
                }
                else
                {
                    // THẤT BẠI: Kích hoạt chế độ Chẩn đoán (Diagnostic Mode)
                    // Nếu lỗi là 404 (Not Found), nghĩa là sai tên Model. Ta sẽ đi hỏi danh sách Model đúng.
                    if (result.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        var listUrl = $"https://generativelanguage.googleapis.com/v1beta/models?key={_apiKey}";
                        var listResult = await _httpClient.GetAsync(listUrl);

                        if (listResult.IsSuccessStatusCode)
                        {
                            var listJson = await listResult.Content.ReadAsStringAsync();
                            // Phân tích JSON để lấy tên các model hỗ trợ generateContent
                            using var listDoc = JsonDocument.Parse(listJson);
                            var models = new List<string>();

                            if (listDoc.RootElement.TryGetProperty("models", out var modelArray))
                            {
                                foreach (var m in modelArray.EnumerateArray())
                                {
                                    var name = m.GetProperty("name").GetString(); // dạng "models/gemini-pro"
                                    var methods = m.GetProperty("supportedGenerationMethods");
                                    // Chỉ lấy model có hỗ trợ generateContent
                                    if (methods.ToString().Contains("generateContent"))
                                    {
                                        models.Add(name.Replace("models/", ""));
                                    }
                                }
                            }

                            return $"⚠️ Lỗi Model! Key của bạn KHÔNG dùng được '{modelName}'.\n" +
                                   $"✅ Danh sách Model khả dụng với Key của bạn là:\n- {string.Join("\n- ", models)}\n\n" +
                                   "--> Hãy sửa lại biến 'modelName' trong code bằng 1 trong các tên trên.";
                        }
                    }

                    // Các lỗi khác
                    var errorJson = await result.Content.ReadAsStringAsync();
                    return $"Lỗi API ({result.StatusCode}): {errorJson}";
                }
            }
            catch (Exception ex)
            {
                return $"Lỗi hệ thống: {ex.Message}";
            }
        }
    }
}