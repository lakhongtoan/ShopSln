using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Models;
using Shop.Services;
using System;

namespace Shop.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AppIdentityDbContext _identityContext;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IApiService _apiService;

        public AccountController(AppDbContext context,
                                 AppIdentityDbContext identityContext,
                                 SignInManager<IdentityUser> signInManager,
                                 UserManager<IdentityUser> userManager,
                                 IApiService apiService)
        {
            _context = context;
            _identityContext = identityContext;
            _signInManager = signInManager;
            _userManager = userManager;
            _apiService = apiService;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login() => View();

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            //Chức năng login
            var result = await _signInManager.PasswordSignInAsync(username, password, false, false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByNameAsync(username);
                if (user != null) 
                {
                    if (await _userManager.IsInRoleAsync(user, "Admin"))
                        return Redirect("/admin");
                    return RedirectToAction("Index", "Shop");
                }
            }

            ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng!";
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register() => View();

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Register(string username, string email, string password)
        {
            // Kiểm tra email đã tồn tại chưa (query trực tiếp để tránh lỗi khi có nhiều email trùng)
            var existingUserByEmail = await _identityContext.Users
                .Where(u => u.Email == email)
                .FirstOrDefaultAsync();
            if (existingUserByEmail != null)
            {
                ViewBag.Error = "Email này đã được sử dụng. Vui lòng sử dụng email khác!";
                return View();
            }

            // Kiểm tra username đã tồn tại chưa
            var existingUserByUsername = await _userManager.FindByNameAsync(username);
            if (existingUserByUsername != null)
            {
                ViewBag.Error = "Tên đăng nhập này đã được sử dụng. Vui lòng chọn tên khác!";
                return View();
            }

            var user = new IdentityUser { UserName = username, Email = email };
            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");
                ViewBag.Success = "Đăng ký thành công! Vui lòng đăng nhập.";
                return View();
            }

            // Xử lý các lỗi validation khác từ Identity
            var errorMessages = result.Errors.Select(e => e.Description).ToList();
            var errorText = string.Join(", ", errorMessages);
            
            // Chuyển đổi một số thông báo lỗi phổ biến sang tiếng Việt
            if (errorText.Contains("Password") && errorText.Contains("too short"))
                ViewBag.Error = "Mật khẩu phải có ít nhất 6 ký tự!";
            else if (errorText.Contains("Password") && errorText.Contains("non alphanumeric"))
                ViewBag.Error = "Mật khẩu phải chứa ít nhất một ký tự đặc biệt!";
            else
                ViewBag.Error = errorText;

            return View();
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Redirect("/");
        }

        [Authorize]
        public async Task<IActionResult> MyOrders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // Luôn load từ DbContext để đảm bảo OrderItems được load đầy đủ
            var orders = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Load tất cả OrderItems cho các orders này
            var orderIds = orders.Select(o => o.OrderId).ToList();
            if (orderIds.Any())
            {
                var allOrderItems = await _context.OrderItems
                    .AsNoTracking()
                    .Where(oi => orderIds.Contains(oi.OrderId))
                    .ToListAsync();

                // Gán OrderItems cho từng order
                foreach (var order in orders)
                {
                    order.OrderItems = allOrderItems
                        .Where(oi => oi.OrderId == order.OrderId)
                        .ToList();
                }
            }

            return View(orders);
        }

        [Authorize]
        public async Task<IActionResult> OrderDetail(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // Luôn load từ DbContext để đảm bảo OrderItems được load đầy đủ
            var order = await _context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null || order.UserId != user.Id) return NotFound();

            // Load OrderItems riêng để đảm bảo có dữ liệu
            var orderItems = await _context.OrderItems
                .AsNoTracking()
                .Where(oi => oi.OrderId == id)
                .ToListAsync();

            // Gán OrderItems cho order
            order.OrderItems = orderItems;

            return View(order);
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            return View(user);
        }

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                return View();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                ViewBag.Error = "Không tìm thấy người dùng.";
                return View();
            }

            var result = await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                ViewBag.Success = "Đổi mật khẩu thành công!";
            }
            else
            {
                ViewBag.Error = string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return View();
        }
    }
}
