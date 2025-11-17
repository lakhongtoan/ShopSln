using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Shop.Models;
using Shop.Services;
using System;

namespace Shop.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IApiService _apiService;

        public AccountController(AppDbContext context,
                                 SignInManager<IdentityUser> signInManager,
                                 UserManager<IdentityUser> userManager,
                                 IApiService apiService)
        {
            _context = context;
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
            var user = new IdentityUser { UserName = username, Email = email };
            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");
                return RedirectToAction("Login");
            }

            ViewBag.Error = string.Join(", ", result.Errors.Select(e => e.Description));
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

            var orders = await _apiService.GetOrdersByUserIdAsync(user.Id);

            return View(orders);
        }

        [Authorize]
        public async Task<IActionResult> OrderDetail(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var order = await _apiService.GetOrderByIdAsync(id);
            if (order == null || order.UserId != user.Id) return NotFound();

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
