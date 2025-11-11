using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Models;

namespace Shop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ShopDbContext _db;

    public ProductsController(ShopDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll()
    {
        var products = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .ToListAsync();
        return Ok(products);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<Product>> GetById(long id)
    {
        var product = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .FirstOrDefaultAsync(p => p.ProductID == id);
        if (product == null) return NotFound();
        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<Product>> Create(Product input)
    {
        _db.Products.Add(input);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = input.ProductID }, input);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, Product input)
    {
        if (id != input.ProductID) return BadRequest();

        _db.Entry(input).State = EntityState.Modified;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var entity = await _db.Products.FirstOrDefaultAsync(p => p.ProductID == id);
        if (entity == null) return NotFound();
        _db.Products.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

