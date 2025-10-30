using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Models;
using System.Security.Claims;

namespace Shop.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var stats = new
            {
                TotalProducts = _context.Products.Count(),
                TotalOrders = _context.Orders.Count(),
                TotalCategories = _context.Categories.Count(),
                TotalContacts = _context.Contacts.Count(),
                PendingOrders = _context.Orders.Count(o => o.Status == "Pending"),
                RecentOrders = _context.Orders
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToList()
            };

            return View(stats);
        }

        #region Products Management
        public IActionResult Products()
        {
            var products = _context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();
            return View(products);
        }

        public IActionResult CreateProduct()
        {
            ViewBag.Categories = _context.Categories.Where(c => c.IsActive).ToList();
            return View();
        }

        [HttpPost]
        public IActionResult CreateProduct(Product product)
        {
            if (ModelState.IsValid)
            {
                product.CreatedAt = DateTime.Now;
                _context.Products.Add(product);
                _context.SaveChanges();
                return RedirectToAction("Products");
            }
            ViewBag.Categories = _context.Categories.Where(c => c.IsActive).ToList();
            return View(product);
        }

        public IActionResult EditProduct(long id)
        {
            var product = _context.Products.Find(id);
            if (product == null) return NotFound();
            
            ViewBag.Categories = _context.Categories.Where(c => c.IsActive).ToList();
            return View(product);
        }

        [HttpPost]
        public IActionResult EditProduct(Product product)
        {
            if (ModelState.IsValid)
            {
                product.UpdatedAt = DateTime.Now;
                _context.Products.Update(product);
                _context.SaveChanges();
                return RedirectToAction("Products");
            }
            ViewBag.Categories = _context.Categories.Where(c => c.IsActive).ToList();
            return View(product);
        }

        [HttpPost]
        public IActionResult DeleteProduct(long id)
        {
            var product = _context.Products.Find(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                _context.SaveChanges();
            }
            return RedirectToAction("Products");
        }
        #endregion

        #region Categories Management
        public IActionResult Categories()
        {
            var categories = _context.Categories
                .OrderBy(c => c.Name)
                .ToList();
            return View(categories);
        }

        public IActionResult CreateCategory()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CreateCategory(Category category)
        {
            if (ModelState.IsValid)
            {
                category.CreatedAt = DateTime.Now;
                _context.Categories.Add(category);
                _context.SaveChanges();
                return RedirectToAction("Categories");
            }
            return View(category);
        }

        public IActionResult EditCategory(int id)
        {
            var category = _context.Categories.Find(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        public IActionResult EditCategory(Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Categories.Update(category);
                _context.SaveChanges();
                return RedirectToAction("Categories");
            }
            return View(category);
        }

        [HttpPost]
        public IActionResult DeleteCategory(int id)
        {
            var category = _context.Categories.Find(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                _context.SaveChanges();
            }
            return RedirectToAction("Categories");
        }
        #endregion

        #region Orders Management
        public IActionResult Orders()
        {
            var orders = _context.Orders
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .ToList();
            return View(orders);
        }

        public IActionResult OrderDetails(int id)
        {
            var order = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefault(o => o.OrderId == id);
            
            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost]
        public IActionResult UpdateOrderStatus(int id, string status)
        {
            var order = _context.Orders.Find(id);
            if (order != null)
            {
                order.Status = status;
                if (status == "Shipped")
                    order.ShippedDate = DateTime.Now;
                else if (status == "Delivered")
                    order.DeliveredDate = DateTime.Now;
                
                _context.SaveChanges();
            }
            return RedirectToAction("Orders");
        }
        #endregion

        #region Contacts Management
        public IActionResult Contacts()
        {
            var contacts = _context.Contacts
                .OrderByDescending(c => c.CreatedAt)
                .ToList();
            return View(contacts);
        }

        [HttpPost]
        public IActionResult MarkAsRead(int id)
        {
            var contact = _context.Contacts.Find(id);
            if (contact != null)
            {
                contact.IsRead = true;
                contact.ReadAt = DateTime.Now;
                _context.SaveChanges();
            }
            return RedirectToAction("Contacts");
        }
        #endregion
    }
}
