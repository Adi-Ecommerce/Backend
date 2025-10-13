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

        // 🛒 Add item to cart
        [HttpPost("add")]
        public async Task<IActionResult> AddToCart(int productId, int quantity)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                _context.Carts.Add(cart);
            }

            var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
            if (existingItem != null)
                existingItem.Quantity += quantity;
            else
                cart.CartItems.Add(new CartItem { ProductId = productId, Quantity = quantity });

            await _context.SaveChangesAsync();

            return Ok(new { message = "Item added to cart successfully!" });
        }

        // 🧾 Get all items in cart
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
                return Ok(new { message = "Your cart is empty." });

            var cartDetails = cart.CartItems.Select(ci => new
            {
                Product = ci.Product?.Name,
                ci.Quantity,
                Price = ci.Product?.Price,
                ci.TotalPrice
            });

            return Ok(cartDetails);
        }

        // 💳 View checkout summary
        [HttpGet("checkout")]
        public async Task<IActionResult> Checkout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
                return Ok(new { message = "Cart is empty." });

            var total = cart.CartItems.Sum(ci => ci.TotalPrice);

            return Ok(new
            {
                Items = cart.CartItems.Select(ci => new
                {
                    Product = ci.Product?.Name,
                    ci.Quantity,
                    Price = ci.Product?.Price,
                    ci.TotalPrice
                }),
                Total = total
            });
        }

        // 🗑️ Remove item from cart
        [HttpDelete("remove/{productId}")]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
                return NotFound(new { message = "Cart not found." });

            var item = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
            if (item == null)
                return NotFound(new { message = "Item not found in your cart." });

            cart.CartItems.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Item removed successfully." });
        }

        // ✅ Confirm checkout (clear cart)
        [HttpPost("checkout/confirm")]
        public async Task<IActionResult> ConfirmCheckout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
                return Ok(new { message = "Cart is empty." });

            var total = cart.CartItems.Sum(ci => ci.TotalPrice);

            // (Optional) You could save an Order record here

            _context.CartItems.RemoveRange(cart.CartItems); // Clear the cart
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Checkout successful! Your cart has been cleared.",
                totalPaid = total
            });
        }
    }
}
