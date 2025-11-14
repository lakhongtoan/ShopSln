using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Models;
using Shop.Models.ViewModel;
using System.Text.Json;

namespace Shop.Controllers
{
    public class ShopController : Controller
    {
        private readonly AppDbContext _context;

        private readonly UserManager<IdentityUser> _userManager;

        public ShopController(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ========== TRANG CHỦ ==========
        public IActionResult Index()
        {
            
            var featuredProducts = _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.IsFeatured)
                .Take(8)
                .ToList();

            var categories = _context.Categories
                .Where(c => c.IsActive)
                .Take(6)
                .ToList();
            var sliders = _context.Sliders.ToList();

            ViewBag.Sliders = sliders;
            ViewBag.FeaturedProducts = featuredProducts;
            ViewBag.Categories = categories;
            return View();
        }

        // ========== DANH SÁCH SẢN PHẨM ==========
        public IActionResult Shop(int? categoryId, string? search, int page = 1, int pageSize = 12)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive);

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
            }

            var totalProducts = query.Count();
            var products = query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var categories = _context.Categories
                .Where(c => c.IsActive)
                .ToList();

            ViewBag.Categories = categories;
            ViewBag.CurrentCategoryId = categoryId;
            ViewBag.SearchTerm = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalProducts / pageSize);
            ViewBag.TotalProducts = totalProducts;

            return View(products);
        }

        // ==================================== TRANG CHI TIẾT SẢN PHẨM ====================================
        // View: Views/Shop/Details.cshtml
        public IActionResult Details(long id)
        {
            var product = _context.Products
                .Include(p => p.Category)
                .FirstOrDefault(p => p.ProductID == id);

            if (product == null || !product.IsActive)
                return NotFound();

            // Lấy sản phẩm liên quan
            var relatedProducts = _context.Products
                .Include(p => p.Category)
                .Where(p => p.CategoryId == product.CategoryId && p.ProductID != id && p.IsActive)
                .Take(4)
                .ToList();

            ViewBag.RelatedProducts = relatedProducts;
            return View(product);
        }

        // Redirect từ đường dẫn cũ (nếu còn link ProductDetails)
        public IActionResult ProductDetails(long id)
        {
            return RedirectToAction("Details", new { id });
        }

        // ==================================== GIỎ HÀNG ====================================
        public IActionResult Cart()
        {
            // Lấy sessionId để xác định giỏ hàng của người dùng hiện tại
            var sessionId = GetSessionId();

            // Include Product để có dữ liệu sản phẩm đầy đủ trong view
            var cartItems = _context.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.SessionId == sessionId)
                .ToList();

            return View(cartItems);
        }

        [HttpPost]
        public IActionResult AddToCart(long productId, int quantity = 1)
        {
            // Kiểm tra sản phẩm tồn tại và đang active
            var product = _context.Products.Find(productId);
            if (product == null || !product.IsActive)
                return Json(new { success = false, message = "Product not found" });

            var sessionId = GetSessionId();

            // Kiểm tra sản phẩm đã có trong giỏ chưa
            var existingItem = _context.CartItems
                .FirstOrDefault(ci => ci.SessionId == sessionId && ci.ProductId == productId);

            if (existingItem != null)
            {
                // Nếu có rồi, cộng dồn số lượng
                existingItem.Quantity += quantity;
                existingItem.TotalPrice = existingItem.Quantity * existingItem.UnitPrice;
            }
            else
            {
                // Nếu chưa có, tạo mới
                var cartItem = new CartItem
                {
                    ProductId = productId,
                    ProductName = product.Name,
                    ProductImage = product.Image ?? "",
                    UnitPrice = product.SalePrice ?? product.Price,
                    Quantity = quantity,
                    TotalPrice = (product.SalePrice ?? product.Price) * quantity,
                    SessionId = sessionId,
                    UserId = User.Identity.IsAuthenticated ? User.Identity.Name : "guest",
                    CreatedAt = DateTime.Now
                };
                _context.CartItems.Add(cartItem);
            }

            _context.SaveChanges();

            // Lấy tổng số lượng sản phẩm trong giỏ hàng để update badge/cart icon
            var cartCount = _context.CartItems
                .Where(ci => ci.SessionId == sessionId)
                .Sum(ci => ci.Quantity);

            return Json(new { success = true, cartCount = cartCount });
        }

        [HttpPost]
        public IActionResult UpdateCartItem(int cartItemId, int quantity)
        {
            var cartItem = _context.CartItems.Find(cartItemId);
            if (cartItem == null)
                return Json(new { success = false, message = "Cart item not found" });

            if (quantity <= 0)
            {
                // Xóa sản phẩm nếu số lượng <= 0
                _context.CartItems.Remove(cartItem);
            }
            else
            {
                // Cập nhật số lượng và tính lại total
                cartItem.Quantity = quantity;
                cartItem.TotalPrice = cartItem.Quantity * cartItem.UnitPrice;
            }

            _context.SaveChanges();

            var sessionId = GetSessionId();
            var cartCount = _context.CartItems
                .Where(ci => ci.SessionId == sessionId)
                .Sum(ci => ci.Quantity);

            var cartTotal = _context.CartItems
                .Where(ci => ci.SessionId == sessionId)
                .Sum(ci => ci.TotalPrice);

            // Trả về JSON bao gồm tổng số lượng và tổng tiền
            return Json(new { success = true, cartCount = cartCount, cartTotal = cartTotal });
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int cartItemId)
        {
            var cartItem = _context.CartItems.Find(cartItemId);
            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                _context.SaveChanges();
            }

            var sessionId = GetSessionId();
            var cartCount = _context.CartItems
                .Where(ci => ci.SessionId == sessionId)
                .Sum(ci => ci.Quantity);

            return Json(new { success = true, cartCount = cartCount });
        }

        // ================================ CHECKOUT ================================
        public IActionResult Checkout()
        {
            var sessionId = GetSessionId();
            var cartItems = _context.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.SessionId == sessionId)
                .ToList();

            if (!cartItems.Any())
                return RedirectToAction("Cart");

            var vm = new CheckoutViewModel
            {
                CartItems = cartItems
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(CheckoutViewModel vm)
        {
            var sessionId = GetSessionId();
            var cartItems = _context.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.SessionId == sessionId)
                .ToList();

            if (!cartItems.Any())
                return RedirectToAction("Cart");

            var order = vm.Order;
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                order.UserId = user.Id;
            }

            order.OrderNumber = "ORD" + DateTime.Now.ToString("yyyyMMddHHmmss");
            order.OrderDate = DateTime.Now;
            order.Status = "Pending";
            order.SubTotal = cartItems.Sum(ci => ci.TotalPrice);
            order.TotalAmount = order.SubTotal + order.TaxAmount + order.ShippingAmount;

            _context.Orders.Add(order);
            _context.SaveChanges();

            foreach (var cartItem in cartItems)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.OrderId,
                    ProductId = cartItem.ProductId,
                    ProductName = cartItem.ProductName,
                    ProductImage = cartItem.ProductImage,
                    UnitPrice = cartItem.UnitPrice,
                    Quantity = cartItem.Quantity,
                    TotalPrice = cartItem.TotalPrice
                };
                _context.OrderItems.Add(orderItem);
            }

            _context.CartItems.RemoveRange(cartItems);
            _context.SaveChanges();

            return RedirectToAction("OrderConfirmation", new { orderId = order.OrderId });
        }



        public IActionResult OrderConfirmation(int orderId)
        {
            var order = _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefault(o => o.OrderId == orderId);

            if (order == null)
                return NotFound();

            return View(order);
        }

        // ========== LIÊN HỆ ==========
        [HttpPost]
        public IActionResult Contact(Contact contact)
        {
            if (ModelState.IsValid)
            {
                contact.CreatedAt = DateTime.Now;
                _context.Contacts.Add(contact);
                _context.SaveChanges();
                TempData["Message"] = "Thank you for your message. We will get back to you soon!";
                return RedirectToAction("Contact");
            }
            return View(contact);
        }

        public IActionResult Contact()
        {
            return View();
        }

        // ========== TIỆN ÍCH ==========
        private string GetSessionId()
        {
            var sessionId = HttpContext.Session.GetString("SessionId");
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("SessionId", sessionId);
            }
            return sessionId;
        }
    }
}
