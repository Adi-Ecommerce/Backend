using Backend.Data;
using Backend.Model.Entities;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly AuthDbContext _context;

        public CartController(AuthDbContext context)
        {
            _context = context;
        }

        // Helper method to create standardized response
        private object CreateResponse(bool success, string message, object data = null)
        {
            return new
            {
                success,
                message,
                data
            };
        }

        // Helper method to get cart data for response
        private List<object> GetCartData(Cart cart)
        {
            return cart.CartItems.Select(ci => new
            {
                id = ci.Id,
                productId = ci.ProductId,
                product = ci.Product?.Name,
                quantity = ci.Quantity,
                price = ci.Product?.Price,
                totalPrice = ci.TotalPrice
            }).Cast<object>().ToList();
        }

        // 🛒 Add item to cart
        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                if (request == null || request.ProductId <= 0 || request.Quantity <= 0)
                    return BadRequest(CreateResponse(false, "Invalid product ID or quantity"));

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                    return Unauthorized(CreateResponse(false, "Unauthorized"));

                var product = await _context.Products.FindAsync(request.ProductId);
                if (product == null)
                    return NotFound(CreateResponse(false, "Product not found"));

                if (product.StockQuantity < request.Quantity)
                    return BadRequest(CreateResponse(false, $"Insufficient stock. Only {product.StockQuantity} available"));

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    cart = new Cart { UserId = userId };
                    _context.Carts.Add(cart);
                }

                var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == request.ProductId);
                if (existingItem != null)
                {
                    if (product.StockQuantity < existingItem.Quantity + request.Quantity)
                        return BadRequest(CreateResponse(false, "Cannot add more items. Insufficient stock"));
                    existingItem.Quantity += request.Quantity;
                }
                else
                {
                    cart.CartItems.Add(new CartItem { ProductId = request.ProductId, Quantity = request.Quantity });
                }

                await _context.SaveChangesAsync();

                // Reload cart with products
                cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                var cartData = GetCartData(cart);
                return Ok(CreateResponse(true, "Item added to cart successfully!", cartData));
            }
            catch (Exception ex)
            {
                return StatusCode(500, CreateResponse(false, $"Internal Server Error: {ex.Message}"));
            }
        }

        // 🧾 Get all items in cart
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                    return Unauthorized(CreateResponse(false, "Unauthorized"));

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null || !cart.CartItems.Any())
                    return Ok(CreateResponse(true, "Your cart is empty", new List<object>()));

                var cartData = GetCartData(cart);
                return Ok(CreateResponse(true, "Cart retrieved successfully", cartData));
            }
            catch (Exception ex)
            {
                return StatusCode(500, CreateResponse(false, $"Internal Server Error: {ex.Message}"));
            }
        }

        // 💳 Checkout summary
        [HttpGet("checkout")]
        public async Task<IActionResult> Checkout()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                    return Unauthorized(CreateResponse(false, "Unauthorized"));

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null || !cart.CartItems.Any())
                    return Ok(CreateResponse(true, "Cart is empty", new { items = new List<object>(), total = 0 }));

                var total = cart.CartItems.Sum(ci => ci.TotalPrice);
                var cartItems = GetCartData(cart);

                var summaryData = new
                {
                    items = cartItems,
                    total = total
                };

                return Ok(CreateResponse(true, "Checkout summary retrieved", summaryData));
            }
            catch (Exception ex)
            {
                return StatusCode(500, CreateResponse(false, $"Internal Server Error: {ex.Message}"));
            }
        }

        // Remove item
        [HttpDelete("remove/{productId}")]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            try
            {
                if (productId <= 0)
                    return BadRequest(CreateResponse(false, "Invalid product ID"));

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                    return Unauthorized(CreateResponse(false, "Unauthorized"));

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                    return NotFound(CreateResponse(false, "Cart not found"));

                var item = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
                if (item == null)
                    return NotFound(CreateResponse(false, "Item not found in your cart"));

                cart.CartItems.Remove(item);
                await _context.SaveChangesAsync();

                var cartData = cart.CartItems.Any() ? GetCartData(cart) : new List<object>();
                return Ok(CreateResponse(true, "Item removed successfully", cartData));
            }
            catch (Exception ex)
            {
                return StatusCode(500, CreateResponse(false, $"Internal Server Error: {ex.Message}"));
            }
        }

        // Update quantity
        [HttpPut("update/{cartItemId}")]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, [FromBody] UpdateQuantityRequest request)
        {
            try
            {
                if (cartItemId <= 0 || request == null || request.Quantity <= 0)
                    return BadRequest(CreateResponse(false, "Invalid cart item ID or quantity"));

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                    return Unauthorized(CreateResponse(false, "Unauthorized"));

                var cartItem = await _context.CartItems
                    .Include(ci => ci.Cart)
                    .Include(ci => ci.Product)
                    .FirstOrDefaultAsync(ci => ci.Id == cartItemId);

                if (cartItem == null || cartItem.Cart.UserId != userId)
                    return NotFound(CreateResponse(false, "Cart item not found"));

                if (cartItem.Product.StockQuantity < request.Quantity)
                    return BadRequest(CreateResponse(false, $"Insufficient stock. Only {cartItem.Product.StockQuantity} available"));

                cartItem.Quantity = request.Quantity;
                await _context.SaveChangesAsync();

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                var cartData = GetCartData(cart);
                return Ok(CreateResponse(true, "Quantity updated successfully", cartData));
            }
            catch (Exception ex)
            {
                return StatusCode(500, CreateResponse(false, $"Internal Server Error: {ex.Message}"));
            }
        }

        // Confirm checkout
        [HttpPost("checkout/confirm")]
        public async Task<IActionResult> ConfirmCheckout()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                    return Unauthorized(CreateResponse(false, "Unauthorized"));

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null || !cart.CartItems.Any())
                    return Ok(CreateResponse(true, "Cart is already empty", null));

                var total = cart.CartItems.Sum(ci => ci.TotalPrice);
                var itemsCount = cart.CartItems.Count;

                _context.CartItems.RemoveRange(cart.CartItems);
                await _context.SaveChangesAsync();

                var checkoutData = new
                {
                    totalPaid = total,
                    itemsCount = itemsCount
                };

                return Ok(CreateResponse(true, "Checkout successful! Your cart has been cleared.", checkoutData));
            }
            catch (Exception ex)
            {
                return StatusCode(500, CreateResponse(false, $"Internal Server Error: {ex.Message}"));
            }
        }
    }

    // Request DTOs
    public class AddToCartRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class UpdateQuantityRequest
    {
        public int Quantity { get; set; }
    }
}