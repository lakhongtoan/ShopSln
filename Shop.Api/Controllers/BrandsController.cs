using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Models;

namespace Shop.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BrandsController : ControllerBase
    {
        private readonly ShopDbContext _context;

        public BrandsController(ShopDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy tất cả thương hiệu
        /// </summary>
        /// <param name="isActive">Lọc theo trạng thái kích hoạt (null = tất cả)</param>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Brand>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Brand>>> GetBrands([FromQuery] bool? isActive = null)
        {
            var query = _context.Brands.AsQueryable();

            if (isActive.HasValue)
            {
                query = query.Where(b => b.IsActive == isActive.Value);
            }

            var brands = await query
                .OrderBy(b => b.Name)
                .ToListAsync();

            return Ok(brands);
        }

        /// <summary>
        /// Lấy thương hiệu theo ID
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(Brand), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Brand>> GetBrand(int id, [FromQuery] bool includeProducts = false)
        {
            IQueryable<Brand> query = _context.Brands;

            if (includeProducts)
            {
                query = query.Include(b => b.Products);
            }

            var brand = await query.FirstOrDefaultAsync(b => b.Id == id);

            if (brand == null)
            {
                return NotFound(new { message = "Không tìm thấy thương hiệu" });
            }

            return Ok(brand);
        }

        /// <summary>
        /// Tạo thương hiệu mới
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Brand), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Brand>> PostBrand([FromBody] Brand brand)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validation
            if (string.IsNullOrWhiteSpace(brand.Name))
            {
                return BadRequest(new { message = "Tên thương hiệu không được để trống" });
            }

            if (string.IsNullOrWhiteSpace(brand.Slug))
            {
                return BadRequest(new { message = "Slug không được để trống" });
            }

            // Kiểm tra slug trùng lặp
            var existingSlug = await _context.Brands.AnyAsync(b => b.Slug == brand.Slug);
            if (existingSlug)
            {
                return BadRequest(new { message = "Slug đã tồn tại" });
            }

            _context.Brands.Add(brand);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBrand), new { id = brand.Id }, brand);
        }

        /// <summary>
        /// Cập nhật thương hiệu
        /// </summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PutBrand(int id, [FromBody] Brand brand)
        {
            if (id != brand.Id)
            {
                return BadRequest(new { message = "ID không khớp" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingBrand = await _context.Brands.FindAsync(id);
            if (existingBrand == null)
            {
                return NotFound(new { message = "Không tìm thấy thương hiệu" });
            }

            // Kiểm tra slug trùng lặp (trừ chính nó)
            var existingSlug = await _context.Brands.AnyAsync(b => b.Slug == brand.Slug && b.Id != id);
            if (existingSlug)
            {
                return BadRequest(new { message = "Slug đã tồn tại" });
            }

            // Cập nhật từng field
            existingBrand.Name = brand.Name;
            existingBrand.Slug = brand.Slug;
            existingBrand.Description = brand.Description;
            existingBrand.Image = brand.Image;
            existingBrand.IsActive = brand.IsActive;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BrandExists(id))
                {
                    return NotFound(new { message = "Không tìm thấy thương hiệu" });
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        /// <summary>
        /// Xóa thương hiệu
        /// </summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteBrand(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
            {
                return NotFound(new { message = "Không tìm thấy thương hiệu" });
            }

            // Kiểm tra xem còn sản phẩm nào dùng thương hiệu này không
            bool hasProducts = await _context.Products.AnyAsync(p => p.BrandId == id);
            if (hasProducts)
            {
                return BadRequest(new { message = $"Thương hiệu '{brand.Name}' vẫn còn sản phẩm đang sử dụng, không thể xóa!" });
            }

            _context.Brands.Remove(brand);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Toggle trạng thái kích hoạt của thương hiệu
        /// </summary>
        [HttpPatch("{id:int}/toggle-active")]
        [ProducesResponseType(typeof(Brand), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Brand>> ToggleActive(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
            {
                return NotFound(new { message = "Không tìm thấy thương hiệu" });
            }

            brand.IsActive = !brand.IsActive;
            await _context.SaveChangesAsync();

            return Ok(brand);
        }

        private bool BrandExists(int id)
        {
            return _context.Brands.Any(e => e.Id == id);
        }
    }
}
