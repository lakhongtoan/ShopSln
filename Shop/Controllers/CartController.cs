//using Microsoft.AspNetCore.Cors.Infrastructure;
//using Microsoft.AspNetCore.Mvc;
//using Shop.Models;
//using Shop.Services;

//namespace Shop.Controllers
//{
//    public class CartController : Controller
//    {
//        private readonly AppDbContext _db;
//        private readonly CartService _cartService;
//        private readonly IHttpContextAccessor _httpContextAccessor;

//        public CartController(CartService cartService, IHttpContextAccessor httpContextAccessor)
//        {
//            _cartService = cartService;
//            _httpContextAccessor = httpContextAccessor;
//        }

//        // 🧠 Lấy SessionId hiện tại
//        private string GetSessionId()
//        {
//            var session = _httpContextAccessor.HttpContext!.Session;
//            if (string.IsNullOrEmpty(session.GetString("SessionId")))
//            {
//                session.SetString("SessionId", Guid.NewGuid().ToString());
//            }
//            return session.GetString("SessionId")!;
//        }

//        // 🟢 Hiển thị giỏ hàng
//        public async Task<IActionResult> Index()
//        {
//            string sessionId = GetSessionId();
//            var cartItems = await _cartService.GetCartItemsAsync(sessionId);
//            return View(cartItems);
//        }

//        // 🟢 Thêm sản phẩm (từ trang Shop)
//        [HttpPost]
//        public async Task<IActionResult> Add(long productId, int quantity = 1)
//        {
//            string sessionId = GetSessionId();
//            await _cartService.AddToCartAsync(productId, quantity, sessionId);
//            return RedirectToAction("Index");
//        }

//        // 🟢 AJAX cập nhật số lượng
//        [HttpPost]
//        public async Task<IActionResult> UpdateCartItem(int cartItemId, int quantity)
//        {
//            var success = await _cartService.UpdateCartItemAsync(cartItemId, quantity);
//            return Json(new { success });
//        }

//        // 🟢 AJAX xóa item
//        [HttpPost]
//        public async Task<IActionResult> RemoveFromCart(int cartItemId)
//        {
//            var success = await _cartService.RemoveFromCartAsync(cartItemId);
//            return Json(new { success });
//        }

//        // 🟢 Thanh toán (chuyển qua Order sau này)
//        public IActionResult Checkout()
//        {
//            return View();
//        }

//        [HttpPost]
//        public async Task<IActionResult> Checkout(string name, string email, string phone, string address)
//        {
//            string sessionId = GetSessionId();
//            var cartItems = await _cartService.GetCartItemsAsync(sessionId);

//            if (!cartItems.Any())
//                return RedirectToAction("Index");

//            var order = new Order
//            {
//                CustomerName = name,
//                CustomerEmail = email,
//                CustomerPhone = phone,
//                CustomerAddress = address,
//                TotalAmount = cartItems.Sum(x => x.TotalPrice)
//            };

//            foreach (var item in cartItems)
//            {
//                order.OrderItems.Add(new OrderItem
//                {
//                    ProductId = item.ProductId,
//                    ProductName = item.ProductName,
//                    ProductImage = item.ProductImage,
//                    UnitPrice = item.UnitPrice,
//                    Quantity = item.Quantity,
//                    TotalPrice = item.TotalPrice
//                });
//            }

//            _db.Orders.Add(order);
//            await _db.SaveChangesAsync();

//            await _cartService.ClearCartAsync(sessionId);

//            return RedirectToAction("OrderSuccess", new { id = order.OrderId });
//        }

//    }
//}
