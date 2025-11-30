namespace book.Services
{
    public static class NavigationDataService
    {
        private static int? _editBookId;
        private static Dictionary<string, object> _data = new Dictionary<string, object>();

        public static void SetEditBookId(int bookId)
        {
            _editBookId = bookId;
        }

        public static int? GetEditBookId()
        {
            var id = _editBookId;
            _editBookId = null; // Clear after reading
            return id;
        }

        public static void ClearEditBookId()
        {
            _editBookId = null;
        }

        // Generic data storage methods
        public static void SetData(string key, object value)
        {
            _data[key] = value;
        }

        public static T? GetData<T>(string key)
        {
            if (_data.TryGetValue(key, out object? value))
            {
                return (T?)value;
            }
            return default(T);
        }

        public static void ClearData(string key)
        {
            _data.Remove(key);
        }
    }
}

