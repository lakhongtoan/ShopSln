using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Models;

namespace Shop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ShopDbContext _db;

    public OrdersController(ShopDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lấy tất cả đơn hàng với filter và pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Order>>> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] string? userId = null,
        [FromQuery] string? orderNumber = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(o => o.Status == status);
        }

        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(o => o.UserId == userId);
        }

        if (!string.IsNullOrEmpty(orderNumber))
        {
            query = query.Where(o => o.OrderNumber.Contains(orderNumber));
        }

        if (fromDate.HasValue)
        {
            query = query.Where(o => o.OrderDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(o => o.OrderDate <= toDate.Value);
        }

        var total = await query.CountAsync();
        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var response = new
        {
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(total / (double)pageSize),
            data = orders
        };

        return Ok(response);
    }

    /// <summary>
    /// Lấy đơn hàng theo ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Order>> GetById(int id)
    {
        var order = await _db.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order == null)
            return NotFound(new { message = "Không tìm thấy đơn hàng" });

        return Ok(order);
    }

    /// <summary>
    /// Lấy đơn hàng của một user
    /// </summary>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Order>>> GetByUserId(
        string userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.UserId == userId);

        var total = await query.CountAsync();
        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var response = new
        {
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(total / (double)pageSize),
            data = orders
        };

        return Ok(response);
    }

    /// <summary>
    /// Tạo đơn hàng mới
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Order), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Order>> Create([FromBody] Order input)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Tạo order number nếu chưa có
        if (string.IsNullOrEmpty(input.OrderNumber))
        {
            input.OrderNumber = "ORD" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        if (input.OrderDate == default)
        {
            input.OrderDate = DateTime.Now;
        }

        if (string.IsNullOrEmpty(input.Status))
        {
            input.Status = "Pending";
        }

        _db.Orders.Add(input);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = input.OrderId }, input);
    }

    /// <summary>
    /// Cập nhật đơn hàng
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] Order input)
    {
        if (id != input.OrderId)
            return BadRequest(new { message = "ID không khớp" });

        var existingOrder = await _db.Orders.FindAsync(id);
        if (existingOrder == null)
            return NotFound(new { message = "Không tìm thấy đơn hàng" });

        existingOrder.CustomerName = input.CustomerName;
        existingOrder.CustomerEmail = input.CustomerEmail;
        existingOrder.CustomerPhone = input.CustomerPhone;
        existingOrder.ShippingAddress = input.ShippingAddress;
        existingOrder.BillingAddress = input.BillingAddress;
        existingOrder.SubTotal = input.SubTotal;
        existingOrder.TaxAmount = input.TaxAmount;
        existingOrder.ShippingAmount = input.ShippingAmount;
        existingOrder.TotalAmount = input.TotalAmount;
        existingOrder.Status = input.Status;
        existingOrder.Notes = input.Notes;
        existingOrder.ShippedDate = input.ShippedDate;
        existingOrder.DeliveredDate = input.DeliveredDate;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Cập nhật trạng thái đơn hàng
    /// </summary>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Order>> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest request)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null)
            return NotFound(new { message = "Không tìm thấy đơn hàng" });

        order.Status = request.Status;

        // Tự động cập nhật ngày giao hàng nếu status là "Delivered"
        if (request.Status == "Delivered" && !order.DeliveredDate.HasValue)
        {
            order.DeliveredDate = DateTime.Now;
        }

        // Tự động cập nhật ngày vận chuyển nếu status là "Shipped"
        if (request.Status == "Shipped" && !order.ShippedDate.HasValue)
        {
            order.ShippedDate = DateTime.Now;
        }

        await _db.SaveChangesAsync();

        return Ok(order);
    }

    /// <summary>
    /// Xóa đơn hàng
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var order = await _db.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order == null)
            return NotFound(new { message = "Không tìm thấy đơn hàng" });

        _db.Orders.Remove(order);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

public class UpdateOrderStatusRequest
{
    public string Status { get; set; } = null!;
}

