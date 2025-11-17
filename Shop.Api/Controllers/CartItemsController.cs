using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Models;

namespace Shop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartItemsController : ControllerBase
{
    private readonly ShopDbContext _db;

    public CartItemsController(ShopDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lấy giỏ hàng theo session ID
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CartItem>>> GetBySession([FromQuery] string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            return BadRequest(new { message = "SessionId là bắt buộc" });
        }

        var cartItems = await _db.CartItems
            .Include(ci => ci.Product)
            .Where(ci => ci.SessionId == sessionId)
            .ToListAsync();

        var total = cartItems.Sum(ci => ci.TotalPrice);
        var totalQuantity = cartItems.Sum(ci => ci.Quantity);

        return Ok(new
        {
            sessionId,
            items = cartItems,
            totalQuantity,
            totalAmount = total
        });
    }

    /// <summary>
    /// Thêm sản phẩm vào giỏ hàng
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CartItem), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartItem>> AddToCart([FromBody] AddToCartRequest request)
    {
        if (string.IsNullOrEmpty(request.SessionId))
        {
            return BadRequest(new { message = "SessionId là bắt buộc" });
        }

        var product = await _db.Products.FindAsync(request.ProductId);
        if (product == null || !(product.IsActive ?? false))
        {
            return NotFound(new { message = "Sản phẩm không tồn tại hoặc đã bị vô hiệu hóa" });
        }

        // Kiểm tra sản phẩm đã có trong giỏ chưa
        var existingItem = await _db.CartItems
            .FirstOrDefaultAsync(ci => ci.SessionId == request.SessionId && ci.ProductId == request.ProductId);

        if (existingItem != null)
        {
            // Cập nhật số lượng
            existingItem.Quantity += request.Quantity;
            existingItem.TotalPrice = existingItem.Quantity * existingItem.UnitPrice;
        }
        else
        {
            // Tạo mới
            var unitPrice = product.SalePrice ?? product.Price;
            var cartItem = new CartItem
            {
                ProductId = request.ProductId,
                ProductName = product.Name,
                ProductImage = product.Image ?? "",
                UnitPrice = unitPrice,
                Quantity = request.Quantity,
                TotalPrice = unitPrice * request.Quantity,
                SessionId = request.SessionId,
                CreatedAt = DateTime.Now,
            };

            _db.CartItems.Add(cartItem);
            existingItem = cartItem;
        }

        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = existingItem.CartItemId }, existingItem);
    }

    /// <summary>
    /// Lấy cart item theo ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CartItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartItem>> GetById(int id)
    {
        var cartItem = await _db.CartItems
            .Include(ci => ci.Product)
            .FirstOrDefaultAsync(ci => ci.CartItemId == id);

        if (cartItem == null)
            return NotFound(new { message = "Không tìm thấy sản phẩm trong giỏ hàng" });

        return Ok(cartItem);
    }

    /// <summary>
    /// Cập nhật số lượng sản phẩm trong giỏ hàng
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CartItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartItem>> UpdateQuantity(int id, [FromBody] UpdateCartItemRequest request)
    {
        var cartItem = await _db.CartItems.FindAsync(id);
        if (cartItem == null)
            return NotFound(new { message = "Không tìm thấy sản phẩm trong giỏ hàng" });

        if (request.Quantity <= 0)
        {
            // Xóa sản phẩm nếu số lượng <= 0
            _db.CartItems.Remove(cartItem);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Đã xóa sản phẩm khỏi giỏ hàng" });
        }

        cartItem.Quantity = request.Quantity;
        cartItem.TotalPrice = cartItem.Quantity * cartItem.UnitPrice;

        await _db.SaveChangesAsync();

        return Ok(cartItem);
    }

    /// <summary>
    /// Xóa sản phẩm khỏi giỏ hàng
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFromCart(int id)
    {
        var cartItem = await _db.CartItems.FindAsync(id);
        if (cartItem == null)
            return NotFound(new { message = "Không tìm thấy sản phẩm trong giỏ hàng" });

        _db.CartItems.Remove(cartItem);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Xóa toàn bộ giỏ hàng theo session
    /// </summary>
    [HttpDelete("session/{sessionId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearCart(string sessionId)
    {
        var cartItems = await _db.CartItems
            .Where(ci => ci.SessionId == sessionId)
            .ToListAsync();

        _db.CartItems.RemoveRange(cartItems);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

public class AddToCartRequest
{
    public string SessionId { get; set; } = null!;
    public long ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public string? UserId { get; set; }
}

public class UpdateCartItemRequest
{
    public int Quantity { get; set; }
}

