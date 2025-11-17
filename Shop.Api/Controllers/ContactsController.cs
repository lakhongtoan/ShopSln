using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Models;

namespace Shop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactsController : ControllerBase
{
    private readonly ShopDbContext _db;

    public ContactsController(ShopDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lấy tất cả liên hệ với pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Contact>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Contacts.AsQueryable();

        var total = await query.CountAsync();
        var contacts = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var response = new
        {
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(total / (double)pageSize),
            data = contacts
        };

        return Ok(response);
    }

    /// <summary>
    /// Lấy liên hệ theo ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Contact), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Contact>> GetById(int id)
    {
        var contact = await _db.Contacts.FindAsync(id);

        if (contact == null)
            return NotFound(new { message = "Không tìm thấy liên hệ" });

        return Ok(contact);
    }

    /// <summary>
    /// Tạo liên hệ mới
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Contact), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Contact>> Create([FromBody] Contact input)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        input.CreatedAt = DateTime.Now;

        _db.Contacts.Add(input);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = input.ContactId }, input);
    }

    /// <summary>
    /// Xóa liên hệ
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var contact = await _db.Contacts.FindAsync(id);
        if (contact == null)
            return NotFound(new { message = "Không tìm thấy liên hệ" });

        _db.Contacts.Remove(contact);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

