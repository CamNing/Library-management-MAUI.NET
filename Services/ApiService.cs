using book.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using book.Models;

namespace book.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly SecureStorageService _secureStorage;
        private readonly string _baseUrl;
        public async Task<DashboardAnalytics?> GetAdminDashboardStatsAsync()
        {
            return await GetAsync<DashboardAnalytics>("analytics/dashboard-stats");
        }
        public ApiService(SecureStorageService secureStorage)
        {
            _secureStorage = secureStorage;

            // 1. Cấu hình địa chỉ IP và Port 5001 (HTTPS)
            string address;
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                // Android dùng 10.0.2.2
                address = "https://10.0.2.2:5001";
            }
            else
            {
                // Windows dùng localhost
                address = "https://localhost:5001";
            }

            _baseUrl = $"{address}/api";

            // 2. CẤU HÌNH BỎ QUA LỖI BẢO MẬT SSL (Quan trọng cho HTTPS Localhost)
            HttpClientHandler handler = new HttpClientHandler();

            // Dòng code này cho phép chấp nhận mọi chứng chỉ (kể cả chứng chỉ ảo của localhost)
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

            // 3. Khởi tạo HttpClient với handler vừa tạo
            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(_baseUrl.TrimEnd('/') + "/")
            };

            

        // Tăng thời gian chờ lên 30s để tránh lỗi mạng chậm
        _httpClient.Timeout = TimeSpan.FromSeconds(30);

            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private async Task SetAuthHeaderAsync()
        {
            var token = await _secureStorage.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
        }
        public async Task<ChatResponse?> ChatWithAiAsync(string message)
        {
            try
            {
                return await PostAsync<ChatRequest, ChatResponse>("ai/chat", new ChatRequest { Message = message });
            }
            catch
            {
                return new ChatResponse { Response = "Xin lỗi, tôi đang gặp sự cố kết nối." };
            }
        }

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            try
            {
                await SetAuthHeaderAsync();
                var normalizedEndpoint = endpoint.TrimStart('/');
                var response = await _httpClient.GetAsync(normalizedEndpoint);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();

                    // Try to parse error message from JSON
                    string errorMessage = errorContent;
                    try
                    {
                        var errorJson = JsonSerializer.Deserialize<JsonElement>(errorContent);
                        if (errorJson.TryGetProperty("message", out var messageElement))
                        {
                            errorMessage = messageElement.GetString() ?? errorContent;
                        }
                    }
                    catch
                    {
                        // If parsing fails, use original error content
                    }

                    throw new HttpRequestException($"Error: {response.StatusCode} - {errorMessage}");
                }

                var content = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(content))
                {
                    return default(T);
                }

                return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                // Log lỗi ra màn hình Output để bạn dễ debug
                System.Diagnostics.Debug.WriteLine($"API GET ERROR: {ex.Message}");
                throw new Exception($"API call failed: {ex.Message}", ex);
            }
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data, bool requiresAuth = true)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var json = JsonSerializer.Serialize(data, options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var normalizedEndpoint = endpoint.TrimStart('/');

                // Debug đường dẫn thực tế đang gọi là gì
                var fullUrl = $"{_baseUrl.TrimEnd('/')}/{normalizedEndpoint}";
                System.Diagnostics.Debug.WriteLine($"API Call: POST {fullUrl}");

                var request = new HttpRequestMessage(HttpMethod.Post, normalizedEndpoint)
                {
                    Content = content
                };

                if (requiresAuth)
                {
                    var token = await _secureStorage.GetTokenAsync();
                    if (!string.IsNullOrEmpty(token))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    }
                }

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"API Error: {response.StatusCode} - {errorContent}");

                    string errorMessage = errorContent;
                    try
                    {
                        var errorJson = JsonSerializer.Deserialize<JsonElement>(errorContent);
                        if (errorJson.TryGetProperty("message", out var messageElement))
                        {
                            errorMessage = messageElement.GetString() ?? errorContent;
                        }
                    }
                    catch
                    {
                    }

                    throw new HttpRequestException($"Error: {response.StatusCode} - {errorMessage}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    return default(TResponse);
                }

                return JsonSerializer.Deserialize<TResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API POST ERROR: {ex.Message}");
                throw new Exception($"API call failed: {ex.Message}", ex);
            }
        }

        public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            try
            {
                await SetAuthHeaderAsync();
                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var normalizedEndpoint = endpoint.TrimStart('/');
                var response = await _httpClient.PutAsync(normalizedEndpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Error: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    return default(TResponse);
                }

                return JsonSerializer.Deserialize<TResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"API call failed: {ex.Message}", ex);
            }
        }
        public async Task<DetailedReport?> GetDetailedReportAsync()
        {
            return await GetAsync<DetailedReport>("analytics/detailed-report");
        }
        public async Task DeleteAsync(string endpoint)
        {
            try
            {
                await SetAuthHeaderAsync();
                var normalizedEndpoint = endpoint.TrimStart('/');
                var response = await _httpClient.DeleteAsync(normalizedEndpoint);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Error: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"API call failed: {ex.Message}", ex);
            }
        }
    }
}