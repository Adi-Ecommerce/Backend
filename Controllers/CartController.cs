using Backend.Data;
using Backend.Model.Entities;
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

        // 🛒 Add to Cart with validation
        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized(new ApiResponse<string> { Success = false, Message = "Unauthorized" });

            if (dto.Quantity <= 0)
                return BadRequest(new ApiResponse<string> { Success = false, Message = "Quantity must be greater than 0." });

            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null)
                return NotFound(new ApiResponse<string> { Success = false, Message = "Product not found." });

            // Optional: validate stock if you track it
            // if (product.StockQuantity < dto.Quantity)
            //     return BadRequest(new ApiResponse<string> { Success = false, Message = "Insufficient stock." });

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                _context.Carts.Add(cart);
            }

            var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == dto.ProductId);
            if (existingItem != null)
                existingItem.Quantity += dto.Quantity;
            else
                cart.CartItems.Add(new CartItem { ProductId = dto.ProductId, Quantity = dto.Quantity });

            await _context.SaveChangesAsync();

            var updatedCart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            var cartData = updatedCart.CartItems.Select(ci => new
            {
                ci.Id,
                ProductId = ci.ProductId,
                Product = ci.Product?.Name,
                ci.Quantity,
                Price = ci.Product?.Price,
                ci.TotalPrice
            });

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Item added successfully!",
                Data = cartData
            });
        }

        // 🧾 Get all items in cart
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized(new ApiResponse<string> { Success = false, Message = "Unauthorized" });

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
                return Ok(new ApiResponse<string> { Success = true, Message = "Your cart is empty." });

            var cartDetails = cart.CartItems.Select(ci => new
            {
                ci.Id,
                ProductId = ci.ProductId,
                Product = ci.Product?.Name,
                ci.Quantity,
                Price = ci.Product?.Price,
                ci.TotalPrice
            });

            return Ok(new ApiResponse<object> { Success = true, Message = "Cart retrieved successfully.", Data = cartDetails });
        }

        // 🧮 Update quantity
        [HttpPut("update/{cartItemId}")]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, [FromBody] UpdateQuantityDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized(new ApiResponse<string> { Success = false, Message = "Unauthorized" });

            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .ThenInclude(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId);

            if (cartItem == null || cartItem.Cart.UserId != userId)
                return NotFound(new ApiResponse<string> { Success = false, Message = "Cart item not found." });

            if (dto.Quantity <= 0)
                return BadRequest(new ApiResponse<string> { Success = false, Message = "Quantity must be greater than 0." });

            cartItem.Quantity = dto.Quantity;
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Quantity updated successfully.",
                Data = new
                {
                    cartItem.Id,
                    Product = cartItem.Product?.Name,
                    cartItem.Quantity,
                    cartItem.TotalPrice
                }
            });
        }

        // 🗑️ Remove item
        [HttpDelete("remove/{productId}")]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized(new ApiResponse<string> { Success = false, Message = "Unauthorized" });

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
                return NotFound(new ApiResponse<string> { Success = false, Message = "Cart not found." });

            var item = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
            if (item == null)
                return NotFound(new ApiResponse<string> { Success = false, Message = "Item not found in your cart." });

            cart.CartItems.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<string> { Success = true, Message = "Item removed successfully." });
        }

        // ✅ Checkout
        [HttpPost("checkout/confirm")]
        public async Task<IActionResult> ConfirmCheckout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized(new ApiResponse<string> { Success = false, Message = "Unauthorized" });

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
                return Ok(new ApiResponse<string> { Success = true, Message = "Cart is empty." });

            var total = cart.CartItems.Sum(ci => ci.TotalPrice);
            _context.CartItems.RemoveRange(cart.CartItems);
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Checkout successful! Cart cleared.",
                Data = new { totalPaid = total }
            });
        }
    }
}
