using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Models;

namespace Shop.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductsController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public IActionResult Index()
        {
            var products = _context.Products.ToList();
            return View("~/Views/Admin/Products/Index.cshtml", products);
        }

        public IActionResult Create()
        {
            ViewBag.Categories = _context.Categories.ToList();
            return View("~/Views/Admin/Products/Create.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Product model, IFormFile? ImageFile)
        {
            // Nếu có lỗi validation thì quay lại form, không lưu
            if (!ModelState.IsValid)
            {
                // Cần truyền lại danh mục để dropdown hiển thị đúng
                ViewBag.Categories = _context.Categories.ToList();
                return View("~/Views/Admin/Products/Create.cshtml", model);
            }

            // Xử lý upload ảnh nếu có
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var fileName = Path.GetFileName(ImageFile.FileName);
                var filePath = Path.Combine("wwwroot/images/products", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    ImageFile.CopyTo(stream);
                }
                model.Image = "/images/products/" + fileName;
            }

            model.CreatedAt = DateTime.Now;
            model.CreatedBy = User.Identity?.Name ?? "System";


            _context.Products.Add(model);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
            return RedirectToAction("Create");
        }

        public IActionResult Edit(long id)
        {
            var product = _context.Products.Find(id);
            if (product == null) return NotFound();
            ViewBag.Categories = _context.Categories.ToList();
            return View("~/Views/Admin/Products/Edit.cshtml", product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Product model, IFormFile? ImageFile)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = _context.Categories.ToList();
                return View("~/Views/Admin/Products/Edit.cshtml",model);
            }

            var existing = _context.Products.Find(model.ProductID);
            if (existing == null) return NotFound();

            existing.Name = model.Name;
            existing.Price = model.Price;
            existing.Description = model.Description;
            existing.CategoryId = model.CategoryId;
            existing.IsActive = model.IsActive;
            existing.IsFeatured = model.IsFeatured;

            if (ImageFile != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                string uploads = Path.Combine(_env.WebRootPath, "images/products");
                if (!Directory.Exists(uploads))
                    Directory.CreateDirectory(uploads);

                string filePath = Path.Combine(uploads, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    ImageFile.CopyTo(stream);
                }

                existing.Image = "/images/products/" + fileName;
            }

            existing.UpdatedAt = DateTime.Now;
            existing.UpdatedBy = User.Identity?.Name ?? "System";

            _context.SaveChanges();
            TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
            return RedirectToAction("Edit");
        }

        public IActionResult Delete(int id)
        {
            var product = _context.Products.Find(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}
