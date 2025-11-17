using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Models;
using Shop.Models.ViewModel;
using Shop.Services;
using System.Text.Json;

namespace Shop.Controllers
{
    [AllowAnonymous]
    public class ShopController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IApiService _apiService;

        public ShopController(AppDbContext context, UserManager<IdentityUser> userManager, IApiService apiService)
        {
            _context = context;
            _userManager = userManager;
            _apiService = apiService;
        }

        // ========== TRANG CHỦ ==========
        public async Task<IActionResult> Index()
        {
            // Thử dùng API, nếu không khả dụng thì fallback về DbContext
            var featuredProducts = await _apiService.GetFeaturedProductsAsync(8);
            if (!featuredProducts.Any())
            {
                // Fallback về DbContext
                featuredProducts = _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsActive && p.IsFeatured)
                    .Take(8)
                    .ToList();
            }

            var categories = (await _apiService.GetCategoriesAsync(true)).Take(6).ToList();
            if (!categories.Any())
            {
                // Fallback về DbContext
                categories = _context.Categories
                    .Where(c => c.IsActive)
                    .Take(6)
                    .ToList();
            }

            var sliders = _context.Sliders.ToList();

            var brands = _context.Brands
                .Where(b => b.IsActive)
                .Take(8)             
                .ToList();

            var campingTips = _context.CampingTips
                .Where(x => x.IsPublished)
                .OrderByDescending(x => x.CreatedAt)
                .Take(3)
                .ToList();
            ViewBag.CampingTips = campingTips;
            ViewBag.Sliders = sliders;
            ViewBag.FeaturedProducts = featuredProducts;
            ViewBag.Categories = categories;
            ViewBag.Brands = brands;   

            return View();
}

        // ========== DANH SÁCH SẢN PHẨM ==========
        public async Task<IActionResult> Shop(int? categoryId, string? search, int page = 1, int pageSize = 12)
        {
            var products = await _apiService.GetProductsAsync(categoryId, search, null, page, pageSize);
            var productList = products.ToList();
            
            // Fallback về DbContext nếu API không khả dụng
            if (!productList.Any())
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
                productList = query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.TotalPages = (int)Math.Ceiling((double)totalProducts / pageSize);
                ViewBag.TotalProducts = totalProducts;
            }
            else
            {
                ViewBag.TotalPages = (int)Math.Ceiling((double)productList.Count / pageSize);
                ViewBag.TotalProducts = productList.Count;
            }

            var categories = await _apiService.GetCategoriesAsync(true);
            var categoryList = categories.ToList();
            if (!categoryList.Any())
            {
                categoryList = _context.Categories
                    .Where(c => c.IsActive)
                    .ToList();
            }

            ViewBag.Categories = categoryList;
            ViewBag.CurrentCategoryId = categoryId;
            ViewBag.SearchTerm = search;
            ViewBag.CurrentPage = page;

            return View(productList);
        }

        // ==================================== TRANG CHI TIẾT SẢN PHẨM ====================================
        // View: Views/Shop/Details.cshtml
        public async Task<IActionResult> Details(long id)
        {
            var product = await _apiService.GetProductByIdAsync(id, true);

            // Fallback về DbContext nếu API không khả dụng
            if (product == null)
            {
                product = _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefault(p => p.ProductID == id);
            }

            if (product == null || !product.IsActive)
                return NotFound();

            // Lấy sản phẩm liên quan
            var relatedProducts = await _apiService.GetRelatedProductsAsync(id, 4);
            if (!relatedProducts.Any())
            {
                relatedProducts = _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.CategoryId == product.CategoryId && p.ProductID != id && p.IsActive)
                    .Take(4)
                    .ToList();
            }

            // Lấy đánh giá của sản phẩm
            var reviews = await _apiService.GetReviewsByProductIdAsync(id);
            if (!reviews.Any())
            {
                reviews = _context.ProductReviews
                    .Where(r => r.ProductId == id && r.IsActive)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToList();
            }

            // Tính điểm đánh giá trung bình
            var activeReviews = reviews.Where(r => r.IsActive).ToList();
            var averageRating = activeReviews.Any() ? activeReviews.Average(r => r.Rating) : 0;
            var totalReviews = activeReviews.Count;

            ViewBag.RelatedProducts = relatedProducts;
            ViewBag.Reviews = activeReviews;
            ViewBag.AverageRating = Math.Round(averageRating, 1);
            ViewBag.TotalReviews = totalReviews;
            return View(product);
        }

        // Redirect từ đường dẫn cũ (nếu còn link ProductDetails)
        public IActionResult ProductDetails(long id)
        {
            return RedirectToAction("Details", new { id });
        }

        // ==================================== GIỎ HÀNG ====================================
        public async Task<IActionResult> Cart()
        {
            var sessionId = GetSessionId();
            var cartItems = await _apiService.GetCartItemsAsync(sessionId);
            
            // Fallback về DbContext nếu API không khả dụng
            if (!cartItems.Any())
            {
                cartItems = _context.CartItems
                    .Include(ci => ci.Product)
                    .Where(ci => ci.SessionId == sessionId)
                    .ToList();
            }
            else
            {
                // Load product details for each cart item
                foreach (var item in cartItems)
                {
                    var product = await _apiService.GetProductByIdAsync(item.ProductId);
                    if (product == null)
                    {
                        product = _context.Products.Find(item.ProductId);
                    }
                    if (product != null)
                    {
                        item.Product = product;
                    }
                }
            }

            return View(cartItems);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(long productId, int quantity = 1)
        {
            var product = await _apiService.GetProductByIdAsync(productId);
            if (product == null || !product.IsActive)
                return Json(new { success = false, message = "Product not found" });

            var sessionId = GetSessionId();
            var userId = User.Identity?.IsAuthenticated == true ? User.Identity.Name : null;

            var cartItem = await _apiService.AddToCartAsync(sessionId, productId, quantity, userId);
            if (cartItem == null)
                return Json(new { success = false, message = "Failed to add to cart" });

            var cartItems = await _apiService.GetCartItemsAsync(sessionId);
            var cartCount = cartItems.Sum(ci => ci.Quantity);

            return Json(new { success = true, cartCount = cartCount });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCartItem(int cartItemId, int quantity)
        {
            var success = await _apiService.UpdateCartItemAsync(cartItemId, quantity);
            if (!success)
                return Json(new { success = false, message = "Cart item not found" });

            var sessionId = GetSessionId();
            var cartItems = await _apiService.GetCartItemsAsync(sessionId);
            var cartCount = cartItems.Sum(ci => ci.Quantity);
            var cartTotal = cartItems.Sum(ci => ci.TotalPrice);

            return Json(new { success = true, cartCount = cartCount, cartTotal = cartTotal });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int cartItemId)
        {
            var success = await _apiService.RemoveFromCartAsync(cartItemId);
            if (!success)
                return Json(new { success = false, message = "Cart item not found" });

            var sessionId = GetSessionId();
            var cartItems = await _apiService.GetCartItemsAsync(sessionId);
            var cartCount = cartItems.Sum(ci => ci.Quantity);

            return Json(new { success = true, cartCount = cartCount });
        }

        // ================================ CHECKOUT ================================
        public async Task<IActionResult> Checkout()
        {
            var sessionId = GetSessionId();
            var cartItems = await _apiService.GetCartItemsAsync(sessionId);

            if (!cartItems.Any())
                return RedirectToAction("Cart");

            // Load product details
            foreach (var item in cartItems)
            {
                var product = await _apiService.GetProductByIdAsync(item.ProductId);
                if (product != null)
                {
                    item.Product = product;
                }
            }

            var vm = new CheckoutViewModel
            {
                CartItems = cartItems.ToList()
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(CheckoutViewModel vm)
        {
            var sessionId = GetSessionId();
            var cartItems = await _apiService.GetCartItemsAsync(sessionId);

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

            // Create order items
            order.OrderItems = cartItems.Select(ci => new OrderItem
            {
                ProductId = ci.ProductId,
                ProductName = ci.ProductName,
                ProductImage = ci.ProductImage,
                UnitPrice = ci.UnitPrice,
                Quantity = ci.Quantity,
                TotalPrice = ci.TotalPrice
            }).ToList();

            var createdOrder = await _apiService.CreateOrderAsync(order);
            if (createdOrder == null)
            {
                ModelState.AddModelError("", "Không thể tạo đơn hàng. Vui lòng thử lại.");
                vm.CartItems = cartItems.ToList();
                return View(vm);
            }

            // Clear cart after successful order
            await _apiService.ClearCartAsync(sessionId);

            return RedirectToAction("OrderConfirmation", new { orderId = createdOrder.OrderId });
        }



        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            var order = await _apiService.GetOrderByIdAsync(orderId);
            if (order == null)
                return NotFound();

            return View(order);
        }

        // ========== ĐÁNH GIÁ SẢN PHẨM ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> SubmitReview(long productId, int rating, string? comment)
        {
            if (rating < 1 || rating > 5)
            {
                return Json(new { success = false, message = "Đánh giá phải từ 1 đến 5 sao" });
            }

            try
            {
                var product = await _apiService.GetProductByIdAsync(productId);
                if (product == null || !product.IsActive)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại" });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Người dùng không hợp lệ" });
                }

                // Tạo đánh giá mới (API sẽ xử lý update nếu đã tồn tại)
                var review = new ProductReview
                {
                    ProductId = productId,
                    UserId = user.Id,
                    UserName = user.UserName ?? user.Email ?? "Người dùng",
                    Rating = rating,
                    Comment = comment,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                var createdReview = await _apiService.CreateReviewAsync(review);
                if (createdReview == null)
                {
                    return Json(new { success = false, message = "Không thể tạo đánh giá" });
                }

                // Lấy lại danh sách reviews và tính điểm trung bình
                var reviews = await _apiService.GetReviewsByProductIdAsync(productId);
                var activeReviews = reviews.Where(r => r.IsActive).ToList();
                var averageRating = activeReviews.Any() ? activeReviews.Average(r => r.Rating) : 0;
                var totalReviews = activeReviews.Count;

                return Json(new 
                { 
                    success = true, 
                    message = "Đánh giá thành công",
                    averageRating = Math.Round(averageRating, 1),
                    totalReviews = totalReviews
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }
  

        // ========== GIỚI THIỆU ==========
        public IActionResult About()
        {
            return View();
        }

        // ========== LIÊN HỆ ==========
        [HttpPost]
        public async Task<IActionResult> Contact(Contact contact)
        {
            if (ModelState.IsValid)
            {
                contact.CreatedAt = DateTime.Now;
                var createdContact = await _apiService.CreateContactAsync(contact);
                if (createdContact != null)
                {
                    TempData["Message"] = "Thank you for your message. We will get back to you soon!";
                    return RedirectToAction("Contact");
                }
                ModelState.AddModelError("", "Không thể gửi tin nhắn. Vui lòng thử lại.");
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
