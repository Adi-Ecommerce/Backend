using Backend.Data;
using Backend.Models;
using Backend.Models.DTOs;
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

        private object CreateResponse(bool success, string message, object data = null)
        {
            return new { success, message, data };
        }

        // Helper: now includes image
        private List<object> GetCartData(Cart cart)
        {
            return cart.CartItems.Select(ci => new
            {
                id = ci.Id,
                productId = ci.ProductId,
                product = ci.Product?.Name,
                image = ci.Product?.Image, // 🖼️ Include image
                quantity = ci.Quantity,
                price = ci.Product?.Price,
                totalPrice = ci.TotalPrice
            }).Cast<object>().ToList();
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto request)
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
                    return BadRequest(CreateResponse(false, $"Only {product.StockQuantity} left in stock"));

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
                        return BadRequest(CreateResponse(false, "Not enough stock"));
                    existingItem.Quantity += request.Quantity;
                }
                else
                {
                    cart.CartItems.Add(new CartItem { ProductId = request.ProductId, Quantity = request.Quantity });
                }

                await _context.SaveChangesAsync();

                cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                var cartData = GetCartData(cart);
                return Ok(CreateResponse(true, "Item added successfully", cartData));
            }
            catch (Exception ex)
            {
                return StatusCode(500, CreateResponse(false, ex.Message));
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
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

        [HttpGet("checkout")]
        public async Task<IActionResult> Checkout()
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

            return Ok(CreateResponse(true, "Checkout summary retrieved", new { items = cartItems, total }));
        }

        [HttpDelete("remove/{productId}")]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
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

            var cartData = GetCartData(cart);
            return Ok(CreateResponse(true, "Item removed successfully", cartData));
        }

        [HttpPut("update/{cartItemId}")]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, [FromBody] UpdateQuantityDto request)
        {
            if (cartItemId <= 0 || request == null || request.Quantity <= 0)
                return BadRequest(CreateResponse(false, "Invalid data"));

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
                return BadRequest(CreateResponse(false, $"Only {cartItem.Product.StockQuantity} available"));

            cartItem.Quantity = request.Quantity;
            await _context.SaveChangesAsync();

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            var cartData = GetCartData(cart);
            return Ok(CreateResponse(true, "Quantity updated", cartData));
        }

        [HttpPost("checkout/confirm")]
        public async Task<IActionResult> ConfirmCheckout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized(CreateResponse(false, "Unauthorized"));

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
                return Ok(CreateResponse(true, "Cart already empty", null));

            var total = cart.CartItems.Sum(ci => ci.TotalPrice);
            var count = cart.CartItems.Count;

            _context.CartItems.RemoveRange(cart.CartItems);
            await _context.SaveChangesAsync();

            return Ok(CreateResponse(true, "Checkout successful", new { totalPaid = total, itemsCount = count }));
        }
    }
}
