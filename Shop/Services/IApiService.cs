using Shop.Models;

namespace Shop.Services
{
    public interface IApiService
    {
        // Products
        Task<IEnumerable<Product>> GetProductsAsync(int? categoryId = null, string? search = null, bool? isFeatured = null, int page = 1, int pageSize = 12);
        Task<Product?> GetProductByIdAsync(long id, bool includeReviews = false);
        Task<IEnumerable<Product>> GetFeaturedProductsAsync(int limit = 8);
        Task<IEnumerable<Product>> GetRelatedProductsAsync(long productId, int limit = 4);

        // Categories
        Task<IEnumerable<Category>> GetCategoriesAsync(bool? isActive = null);
        Task<Category?> GetCategoryByIdAsync(int id);

        // Brands
        Task<IEnumerable<Brand>> GetBrandsAsync(bool? isActive = null);
        Task<Brand?> GetBrandByIdAsync(int id, bool includeProducts = false);
        Task<Brand?> CreateBrandAsync(Brand brand);
        Task<bool> UpdateBrandAsync(int id, Brand brand);
        Task<bool> DeleteBrandAsync(int id);
        Task<bool> ToggleBrandActiveAsync(int id);

        // Cart Items
        Task<IEnumerable<CartItem>> GetCartItemsAsync(string sessionId);
        Task<CartItem?> AddToCartAsync(string sessionId, long productId, int quantity = 1, string? userId = null);
        Task<bool> UpdateCartItemAsync(int cartItemId, int quantity);
        Task<bool> RemoveFromCartAsync(int cartItemId);
        Task<bool> ClearCartAsync(string sessionId);

        // Orders
        Task<Order?> CreateOrderAsync(Order order);
        Task<Order?> GetOrderByIdAsync(int orderId);
        Task<IEnumerable<Order>> GetOrdersByUserIdAsync(string userId, int page = 1, int pageSize = 20);

        // Reviews
        Task<ProductReview?> CreateReviewAsync(ProductReview review);
        Task<IEnumerable<ProductReview>> GetReviewsByProductIdAsync(long productId);

        // Contacts
        Task<Contact?> CreateContactAsync(Contact contact);
    }
}

