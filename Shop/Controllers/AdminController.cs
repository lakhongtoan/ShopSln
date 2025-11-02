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
