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
            // ⚡ Chạy API calls SONG SONG (chúng không dùng DbContext)
            var featuredProductsTask = _apiService.GetFeaturedProductsAsync(8);
            var categoriesTask = _apiService.GetCategoriesAsync(true);

            // Đợi API calls hoàn thành
            await Task.WhenAll(featuredProductsTask, categoriesTask);

            // Xử lý kết quả API
            var featuredProducts = await featuredProductsTask;
            if (!featuredProducts.Any())
            {
                // Fallback về DbContext (tuần tự vì cùng instance)
                featuredProducts = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Where(p => p.IsActive && p.IsFeatured)
                    .Take(8)
                    .ToListAsync();
            }

            var categories = (await categoriesTask).Take(6).ToList();
            if (!categories.Any())
            {
                // Fallback về DbContext (tuần tự)
                categories = await _context.Categories
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .Take(6)
                    .ToListAsync();
            }

            // ⚡ Database queries chạy TUẦN TỰ (không thể song song trên cùng DbContext)
            // Nhưng vẫn async nên không block thread
            var sliders = await _context.Sliders
                .AsNoTracking()
                .ToListAsync();

            var brands = await _context.Brands
                .AsNoTracking()
                .Where(b => b.IsActive)
                .Take(8)
                .ToListAsync();

            var campingTips = await _context.CampingTips
                .AsNoTracking()
                .Where(x => x.IsPublished)
                .OrderByDescending(x => x.CreatedAt)
                .Take(3)
                .ToListAsync();

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
            // ⚡ Chạy products và categories song song
            var productsTask = _apiService.GetProductsAsync(categoryId, search, null, page, pageSize);
            var categoriesTask = _apiService.GetCategoriesAsync(true);
            
            var products = await productsTask;
            var productList = products.ToList();
            
            // Fallback về DbContext nếu API không khả dụng
            if (!productList.Any())
            {
                var query = _context.Products
                    .AsNoTracking()
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

                // ⚡ Chạy Count và query song song
                var countTask = query.CountAsync();
                var productsDbTask = query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                await Task.WhenAll(countTask, productsDbTask);

                var totalProducts = await countTask;
                productList = await productsDbTask;

                ViewBag.TotalPages = (int)Math.Ceiling((double)totalProducts / pageSize);
                ViewBag.TotalProducts = totalProducts;
            }
            else
            {
                ViewBag.TotalPages = (int)Math.Ceiling((double)productList.Count / pageSize);
                ViewBag.TotalProducts = productList.Count;
            }

            var categories = await categoriesTask;
            var categoryList = categories.ToList();
            if (!categoryList.Any())
            {
                categoryList = await _context.Categories
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .ToListAsync();
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
            // ⚡ Chạy các queries song song
            var productTask = _apiService.GetProductByIdAsync(id, true);
            var relatedProductsTask = _apiService.GetRelatedProductsAsync(id, 4);
            var reviewsTask = _apiService.GetReviewsByProductIdAsync(id);

            var product = await productTask;

            // Fallback về DbContext nếu API không khả dụng
            if (product == null)
            {
                product = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.ProductID == id);
            }

            if (product == null || !product.IsActive)
                return NotFound();

            // Đợi các queries song song
            await Task.WhenAll(relatedProductsTask, reviewsTask);

            // Lấy sản phẩm liên quan
            var relatedProducts = await relatedProductsTask;
            if (!relatedProducts.Any())
            {
                relatedProducts = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Where(p => p.CategoryId == product.CategoryId && p.ProductID != id && p.IsActive)
                    .Take(4)
                    .ToListAsync();
            }

            // Lấy đánh giá của sản phẩm (chỉ lấy reviews của sản phẩm này)
            var reviews = await reviewsTask;
            if (!reviews.Any())
            {
                reviews = await _context.ProductReviews
                    .AsNoTracking()
                    .Where(r => r.ProductId == id && r.IsActive)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
            }
            else
            {
                // Đảm bảo chỉ lấy reviews của sản phẩm này
                reviews = reviews.Where(r => r.ProductId == id && r.IsActive).ToList();
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
                cartItems = await _context.CartItems
                    .AsNoTracking()
                    .Include(ci => ci.Product)
                    .Where(ci => ci.SessionId == sessionId)
                    .ToListAsync();
            }
            else
            {
                // ⚡ Batch load - 1 query thay vì N+1 queries
                var productIds = cartItems.Select(ci => ci.ProductId).Distinct().ToList();
                var productsDict = new Dictionary<long, Product>();

                // Load từ database trước (nhanh hơn API)
                var products = await _context.Products
                    .AsNoTracking()
                    .Where(p => productIds.Contains(p.ProductID))
                    .ToListAsync();

                foreach (var product in products)
                {
                    productsDict[product.ProductID] = product;
                }

                // Chỉ gọi API cho products còn thiếu (song song)
                var missingIds = productIds.Where(id => !productsDict.ContainsKey(id)).ToList();
                if (missingIds.Any())
                {
                    var apiProductTasks = missingIds.Select(id => _apiService.GetProductByIdAsync(id));
                    var apiProducts = await Task.WhenAll(apiProductTasks);
                    foreach (var product in apiProducts.Where(p => p != null))
                    {
                        if (product != null)
                        {
                            productsDict[product.ProductID] = product;
                        }
                    }
                }

                // Gán vào cart items
                foreach (var item in cartItems)
                {
                    if (productsDict.TryGetValue(item.ProductId, out var product))
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
            // Thử lấy product từ API trước, nếu không khả dụng thì fallback về database
            var product = await _apiService.GetProductByIdAsync(productId);
            if (product == null)
            {
                // Fallback về DbContext nếu API không khả dụng
                product = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.ProductID == productId);
            }

            if (product == null || !product.IsActive)
                return Json(new { success = false, message = "Sản phẩm không tồn tại hoặc đã bị vô hiệu hóa" });

            var sessionId = GetSessionId();
            var userId = User.Identity?.IsAuthenticated == true ? User.Identity.Name : null;

            // Thử thêm vào giỏ qua API, nếu không khả dụng thì dùng database trực tiếp
            var cartItem = await _apiService.AddToCartAsync(sessionId, productId, quantity, userId);
            if (cartItem == null)
            {
                // Fallback: Thêm vào giỏ hàng trực tiếp qua database
                var existingCartItem = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.SessionId == sessionId && ci.ProductId == productId);

                if (existingCartItem != null)
                {
                    existingCartItem.Quantity += quantity;
                    existingCartItem.UnitPrice = product.SalePrice ?? product.Price;
                    existingCartItem.TotalPrice = existingCartItem.Quantity * existingCartItem.UnitPrice;
                }
                else
                {
                    var unitPrice = product.SalePrice ?? product.Price;
                    cartItem = new CartItem
                    {
                        ProductId = productId,
                        ProductName = product.Name,
                        ProductImage = product.Image ?? "",
                        UnitPrice = unitPrice,
                        Quantity = quantity,
                        TotalPrice = unitPrice * quantity,
                        SessionId = sessionId,
                        UserId = userId,
                        CreatedAt = DateTime.Now
                    };
                    _context.CartItems.Add(cartItem);
                }

                await _context.SaveChangesAsync();
            }

            // Lấy số lượng giỏ hàng
            var cartItems = await _apiService.GetCartItemsAsync(sessionId);
            if (!cartItems.Any())
            {
                // Fallback về database nếu API không khả dụng
                cartItems = await _context.CartItems
                    .AsNoTracking()
                    .Where(ci => ci.SessionId == sessionId)
                    .ToListAsync();
            }

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

            // ⚡ Batch load - 1 query thay vì N+1 queries
            var productIds = cartItems.Select(ci => ci.ProductId).Distinct().ToList();
            var productsDict = new Dictionary<long, Product>();

            // Load từ database trước (nhanh hơn API)
            var products = await _context.Products
                .AsNoTracking()
                .Where(p => productIds.Contains(p.ProductID))
                .ToListAsync();

            foreach (var product in products)
            {
                productsDict[product.ProductID] = product;
            }

            // Chỉ gọi API cho products còn thiếu (song song)
            var missingIds = productIds.Where(id => !productsDict.ContainsKey(id)).ToList();
            if (missingIds.Any())
            {
                var apiProductTasks = missingIds.Select(id => _apiService.GetProductByIdAsync(id));
                var apiProducts = await Task.WhenAll(apiProductTasks);
                foreach (var product in apiProducts.Where(p => p != null))
                {
                    if (product != null)
                    {
                        productsDict[product.ProductID] = product;
                    }
                }
            }

            // Gán vào cart items
            foreach (var item in cartItems)
            {
                if (productsDict.TryGetValue(item.ProductId, out var product))
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
