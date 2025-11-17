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

        // GET: api/brands
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Brand>>> GetBrands()
        {
            // Nếu muốn chỉ lấy brand đang active và có cột IsActive:
            // return await _context.Brands.Where(b => b.IsActive).ToListAsync();

            return await _context.Brands.ToListAsync();
        }

        // GET: api/brands/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Brand>> GetBrand(int id)
        {
            var brand = await _context.Brands.FindAsync(id);

            if (brand == null)
            {
                return NotFound();
            }

            return brand;
        }

        // POST: api/brands
        [HttpPost]
        public async Task<ActionResult<Brand>> PostBrand(Brand brand)
        {
            _context.Brands.Add(brand);
            await _context.SaveChangesAsync();

            // trả về 201 Created + location của resource mới
            return CreatedAtAction(nameof(GetBrand), new { id = brand.Id }, brand);
        }

        // PUT: api/brands/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> PutBrand(int id, Brand brand)
        {
            if (id != brand.Id)
            {
                return BadRequest("Id trên URL không khớp với Id trong body.");
            }

            _context.Entry(brand).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BrandExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            // Không trả nội dung, chỉ báo OK
            return NoContent();
        }

        // DELETE: api/brands/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteBrand(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
            {
                return NotFound();
            }

            _context.Brands.Remove(brand);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool BrandExists(int id)
        {
            return _context.Brands.Any(e => e.Id == id);
        }
    }
}
