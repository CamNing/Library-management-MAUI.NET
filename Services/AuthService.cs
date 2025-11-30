using book.Models;
using System.Text.Json;

namespace book.Services
{
    public class AuthService
    {
        private readonly ApiService _apiService;
        private readonly SecureStorageService _secureStorage;

        public AuthService(ApiService apiService, SecureStorageService secureStorage)
        {
            _apiService = apiService;
            _secureStorage = secureStorage;
        }

        public async Task<LoginResponse?> LoginAsync(string username, string password)
        {
            var request = new LoginRequest
            {
                Username = username,
                Password = password
            };

            var response = await _apiService.PostAsync<LoginRequest, LoginResponse>(
                "auth/login", request, requiresAuth: false);

            if (response != null && !string.IsNullOrEmpty(response.Token))
            {
                await _secureStorage.SaveTokenAsync(response.Token);
                await _secureStorage.SaveUserInfoAsync(
                    response.Username,
                    response.Role,
                    response.ReaderCardCode);
            }

            return response;
        }

        public async Task LogoutAsync()
        {
            await _secureStorage.ClearAsync();
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            var token = await _secureStorage.GetTokenAsync();
            return !string.IsNullOrEmpty(token);
        }

        public async Task<string?> GetUserRoleAsync()
        {
            var (_, role, _) = await _secureStorage.GetUserInfoAsync();
            return role;
        }

        public async Task<LoginResponse?> RegisterAsync(string username, string password, string email, string? fullName = null, string? phone = null, string? address = null)
        {
            var request = new RegisterRequest
            {
                Username = username,
                Password = password,
                Email = email,
                FullName = fullName,
                Phone = phone,
                Address = address
            };

            var response = await _apiService.PostAsync<RegisterRequest, LoginResponse>(
                "auth/register", request, requiresAuth: false);

            if (response != null && !string.IsNullOrEmpty(response.Token))
            {
                await _secureStorage.SaveTokenAsync(response.Token);
                await _secureStorage.SaveUserInfoAsync(
                    response.Username,
                    response.Role,
                    response.ReaderCardCode);
            }

            return response;
        }
    }
}

