using System.Text;
using System.Text.RegularExpressions;

namespace LibraryAPI.Helpers
{
    public static class StringUtils
    {
        public static string ConvertToUnSign(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;

            // 1. Chuẩn hóa chuỗi unicode
            s = s.Normalize(NormalizationForm.FormD);

            // 2. Dùng Regex để loại bỏ dấu
            Regex regex = new Regex("\\p{IsCombiningDiacriticalMarks}+");
            string temp = regex.Replace(s, string.Empty)
                               .Replace('\u0111', 'd').Replace('\u0110', 'D'); // Xử lý đ/Đ

            // 3. Xử lý các ký tự đặc biệt nếu cần và chuyển về chữ thường
            return temp.Normalize(NormalizationForm.FormC).ToLower().Trim();
        }
    }
}