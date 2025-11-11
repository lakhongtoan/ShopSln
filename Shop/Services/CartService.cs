using Microsoft.EntityFrameworkCore;
using Shop.Models;

namespace Shop.Services
{
    public class CartService
    {
        private readonly AppDbContext _db;

        public CartService(AppDbContext db)
        {
            _db = db;
        }

        // 🟢 Lấy toàn bộ cart theo session hoặc user
        public async Task<List<CartItem>> GetCartItemsAsync(string sessionId, string? userId = null)
        {
            return await _db.CartItems
                .Include(c => c.Product)
                .Where(c => c.SessionId == sessionId || (userId != null && c.UserId == userId))
                .ToListAsync();
        }

        // 🟢 Thêm sản phẩm vào giỏ
        public async Task AddToCartAsync(long productId, int quantity, string sessionId, string? userId = null)
        {
            var product = await _db.Products.FindAsync(productId);
            if (product == null) return;

            var cartItem = await _db.CartItems.FirstOrDefaultAsync(c =>
                (c.SessionId == sessionId || (userId != null && c.UserId == userId)) &&
                c.ProductId == productId);

            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
                cartItem.UnitPrice = product.Price;
            }
            else
            {
                cartItem = new CartItem
                {
                    ProductId = productId,
                    ProductName = product.Name,
                    ProductImage = product.Image,
                    UnitPrice = product.Price,
                    Quantity = quantity,
                    SessionId = sessionId,
                    UserId = userId
                };
                _db.CartItems.Add(cartItem);
            }

            await _db.SaveChangesAsync();
        }

        // 🟢 Cập nhật số lượng
        public async Task<bool> UpdateCartItemAsync(int cartItemId, int quantity)
        {
            var item = await _db.CartItems.FindAsync(cartItemId);
            if (item == null) return false;

            item.Quantity = quantity;
            await _db.SaveChangesAsync();
            return true;
        }

        // 🟢 Xóa sản phẩm khỏi giỏ
        public async Task<bool> RemoveFromCartAsync(int cartItemId)
        {
            var item = await _db.CartItems.FindAsync(cartItemId);
            if (item == null) return false;

            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();
            return true;
        }

        // 🟢 Xóa toàn bộ giỏ hàng (sau khi thanh toán)
        public async Task ClearCartAsync(string sessionId, string? userId = null)
        {
            var items = _db.CartItems.Where(c => c.SessionId == sessionId || (userId != null && c.UserId == userId));
            _db.CartItems.RemoveRange(items);
            await _db.SaveChangesAsync();
        }
    }
}
