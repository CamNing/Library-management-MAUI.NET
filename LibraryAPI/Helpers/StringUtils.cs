// File: LibraryAPI/Helpers/StringUtils.cs
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

            Regex regex = new Regex("\\p{IsCombiningDiacriticalMarks}+");
            string temp = s.Normalize(NormalizationForm.FormD);
            string unsigned = regex.Replace(temp, string.Empty).Replace('\u0111', 'd').Replace('\u0110', 'D');

            return unsigned.ToLower().Trim();
        }
    }
}