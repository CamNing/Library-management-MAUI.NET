namespace book.Services
{
    public class SecureStorageService
    {
        private const string TokenKey = "auth_token";
        private const string UsernameKey = "username";
        private const string RoleKey = "role";
        private const string ReaderCardKey = "reader_card";

        public async Task SaveTokenAsync(string token)
        {
            await SecureStorage.SetAsync(TokenKey, token);
        }

        public async Task<string?> GetTokenAsync()
        {
            try
            {
                return await SecureStorage.GetAsync(TokenKey);
            }
            catch
            {
                return null;
            }
        }

        public async Task SaveUserInfoAsync(string username, string role, string? readerCard = null)
        {
            await SecureStorage.SetAsync(UsernameKey, username);
            await SecureStorage.SetAsync(RoleKey, role);
            if (!string.IsNullOrEmpty(readerCard))
            {
                await SecureStorage.SetAsync(ReaderCardKey, readerCard);
            }
        }

        public async Task<(string? Username, string? Role, string? ReaderCard)> GetUserInfoAsync()
        {
            try
            {
                var username = await SecureStorage.GetAsync(UsernameKey);
                var role = await SecureStorage.GetAsync(RoleKey);
                var readerCard = await SecureStorage.GetAsync(ReaderCardKey);
                return (username, role, readerCard);
            }
            catch
            {
                return (null, null, null);
            }
        }

        public async Task ClearAsync()
        {
            SecureStorage.Remove(TokenKey);
            SecureStorage.Remove(UsernameKey);
            SecureStorage.Remove(RoleKey);
            SecureStorage.Remove(ReaderCardKey);
        }
    }
}

