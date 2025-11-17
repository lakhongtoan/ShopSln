using System.Net.Http;
using System.Text;
using System.Text.Json;
using Shop.Models;
using Microsoft.Extensions.Configuration;

namespace Shop.Services
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _baseUrl;

        public ApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:5023";
            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        // Products
        public async Task<IEnumerable<Product>> GetProductsAsync(int? categoryId = null, string? search = null, bool? isFeatured = null, int page = 1, int pageSize = 12)
        {
            try
            {
                var queryParams = new List<string>();
                if (categoryId.HasValue) queryParams.Add($"categoryId={categoryId.Value}");
                if (!string.IsNullOrEmpty(search)) queryParams.Add($"search={Uri.EscapeDataString(search)}");
                if (isFeatured.HasValue) queryParams.Add($"isFeatured={isFeatured.Value}");
                queryParams.Add($"page={page}");
                queryParams.Add($"pageSize={pageSize}");

                var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
                var response = await _httpClient.GetAsync($"/api/products{queryString}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ApiResponse<Product>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return result?.Data ?? Enumerable.Empty<Product>();
                }
            }
            catch (HttpRequestException)
            {
                // API không khả dụng, trả về empty để fallback về DbContext
            }
            catch (TaskCanceledException)
            {
                // Timeout - fallback nhanh về database
            }
            return Enumerable.Empty<Product>();
        }

        public async Task<Product?> GetProductByIdAsync(long id, bool includeReviews = false)
        {
            try
            {
                var url = $"/api/products/{id}";
                if (includeReviews) url += "?includeReviews=true";

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    if (includeReviews)
                    {
                        var result = JsonSerializer.Deserialize<ProductWithReviews>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        return result?.Product;
                    }
                    return JsonSerializer.Deserialize<Product>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }
            catch (HttpRequestException)
            {
                // API không khả dụng
            }
            catch (TaskCanceledException)
            {
                // Timeout - fallback nhanh về database
            }
            return null;
        }

        public async Task<IEnumerable<Product>> GetFeaturedProductsAsync(int limit = 8)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/products/featured?limit={limit}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<IEnumerable<Product>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? Enumerable.Empty<Product>();
                }
            }
            catch (HttpRequestException)
            {
                // API không khả dụng, trả về empty để fallback về DbContext
            }
            catch (TaskCanceledException)
            {
                // Timeout - fallback nhanh về database
            }
            return Enumerable.Empty<Product>();
        }

        public async Task<IEnumerable<Product>> GetRelatedProductsAsync(long productId, int limit = 4)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/products/{productId}/related?limit={limit}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<IEnumerable<Product>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? Enumerable.Empty<Product>();
                }
            }
            catch (HttpRequestException)
            {
                // API không khả dụng
            }
            catch (TaskCanceledException)
            {
                // Timeout - fallback nhanh về database
            }
            return Enumerable.Empty<Product>();
        }

        // Categories
        public async Task<IEnumerable<Category>> GetCategoriesAsync(bool? isActive = null)
        {
            try
            {
                var url = "/api/categories";
                if (isActive.HasValue) url += $"?isActive={isActive.Value}";

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<IEnumerable<Category>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? Enumerable.Empty<Category>();
                }
            }
            catch (HttpRequestException)
            {
                // API không khả dụng, trả về empty để fallback về DbContext
            }
            catch (TaskCanceledException)
            {
                // Timeout - fallback nhanh về database
            }
            return Enumerable.Empty<Category>();
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"/api/categories/{id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Category>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            return null;
        }

        // Cart Items
        public async Task<IEnumerable<CartItem>> GetCartItemsAsync(string sessionId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/cartitems?sessionId={Uri.EscapeDataString(sessionId)}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<CartResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return result?.Items ?? Enumerable.Empty<CartItem>();
                }
            }
            catch (HttpRequestException)
            {
                // API không khả dụng
            }
            catch (TaskCanceledException)
            {
                // Timeout - fallback nhanh về database
            }
            return Enumerable.Empty<CartItem>();
        }

        public async Task<CartItem?> AddToCartAsync(string sessionId, long productId, int quantity = 1, string? userId = null)
        {
            try
            {
                var request = new
                {
                    SessionId = sessionId,
                    ProductId = productId,
                    Quantity = quantity,
                    UserId = userId
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/cartitems", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<CartItem>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }
            catch (HttpRequestException)
            {
                // API không khả dụng, trả về null để fallback về database
            }
            catch (TaskCanceledException)
            {
                // Timeout khi gọi API
            }
            return null;
        }

        public async Task<bool> UpdateCartItemAsync(int cartItemId, int quantity)
        {
            var request = new { Quantity = quantity };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"/api/cartitems/{cartItemId}", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RemoveFromCartAsync(int cartItemId)
        {
            var response = await _httpClient.DeleteAsync($"/api/cartitems/{cartItemId}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ClearCartAsync(string sessionId)
        {
            var response = await _httpClient.DeleteAsync($"/api/cartitems/session/{Uri.EscapeDataString(sessionId)}");
            return response.IsSuccessStatusCode;
        }

        // Orders
        public async Task<Order?> CreateOrderAsync(Order order)
        {
            var json = JsonSerializer.Serialize(order);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/orders", content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Order>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            return null;
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            var response = await _httpClient.GetAsync($"/api/orders/{orderId}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Order>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            return null;
        }

        public async Task<IEnumerable<Order>> GetOrdersByUserIdAsync(string userId, int page = 1, int pageSize = 20)
        {
            var response = await _httpClient.GetAsync($"/api/orders/user/{Uri.EscapeDataString(userId)}?page={page}&pageSize={pageSize}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<Order>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return result?.Data ?? Enumerable.Empty<Order>();
            }
            return Enumerable.Empty<Order>();
        }

        // Reviews
        public async Task<ProductReview?> CreateReviewAsync(ProductReview review)
        {
            var json = JsonSerializer.Serialize(review);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/reviews", content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ProductReview>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            return null;
        }

        public async Task<IEnumerable<ProductReview>> GetReviewsByProductIdAsync(long productId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/reviews/product/{productId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<IEnumerable<ProductReview>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? Enumerable.Empty<ProductReview>();
                }
            }
            catch (HttpRequestException)
            {
                // API không khả dụng
            }
            return Enumerable.Empty<ProductReview>();
        }

        // Contacts
        public async Task<Contact?> CreateContactAsync(Contact contact)
        {
            var json = JsonSerializer.Serialize(contact);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/contacts", content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Contact>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            return null;
        }
    }

    // Helper classes for deserialization
    public class ApiResponse<T>
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
    }

    public class CartResponse
    {
        public string SessionId { get; set; } = string.Empty;
        public IEnumerable<CartItem> Items { get; set; } = Enumerable.Empty<CartItem>();
        public int TotalQuantity { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class ProductWithReviews
    {
        public Product? Product { get; set; }
        public IEnumerable<ProductReview> Reviews { get; set; } = Enumerable.Empty<ProductReview>();
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
    }
}

