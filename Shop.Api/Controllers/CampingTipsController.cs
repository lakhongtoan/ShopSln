using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Models;

namespace Shop.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CampingTipsController : ControllerBase
    {
        private readonly ShopDbContext _db;
        private readonly IWebHostEnvironment _env;

        public CampingTipsController(ShopDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // Lấy tất cả Camping Tip
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tips = await _db.CampingTips
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return Ok(tips);
        }

        // Lấy 1 bài theo ID
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var tip = await _db.CampingTips.FindAsync(id);
            if (tip == null) return NotFound();

            return Ok(tip);
        }

        // Tạo mới
        [HttpPost]
        public async Task<IActionResult> Create(CampingTip model)
        {
            model.CreatedAt = DateTime.Now;

            _db.CampingTips.Add(model);
            await _db.SaveChangesAsync();

            return Ok(model);
        }

        // Cập nhật
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, CampingTip model)
        {
            var tip = await _db.CampingTips.FindAsync(id);
            if (tip == null) return NotFound();

            tip.Title = model.Title;
            tip.Content = model.Content;
            tip.Author = model.Author;
            tip.IsPublished = model.IsPublished;
            tip.Image = model.Image;
            tip.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            return Ok(tip);
        }

        // Xóa
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var tip = await _db.CampingTips.FindAsync(id);
            if (tip == null) return NotFound();

            _db.CampingTips.Remove(tip);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Deleted" });
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File không hợp lệ");

            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var folder = Path.Combine(_env.WebRootPath, "images/camping");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var url = $"/images/camping/{fileName}";

            return Ok(new { imageUrl = url });
        }

    }
}
