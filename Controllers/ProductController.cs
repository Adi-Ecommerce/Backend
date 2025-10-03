using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Model.Entities;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly AuthDbContext _context;

        public ProductController(AuthDbContext context)
        {
            _context = context;
        }

        // GET: api/Product
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetAllProducts()
        {
            return await _context.Products.Include(p => p.Category).ToListAsync();
        }

        // GET: api/Product/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            return product;
        }

        // GET: api/Product/by-category/1
        [HttpGet("by-category/{categoryId}")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProductsByCategory(int categoryId)
        {
            return await _context.Products
                .Where(p => p.CategoryId == categoryId)
                .Include(p => p.Category)
                .ToListAsync();
        }

        // POST: api/Product
        [HttpPost]
        public async Task<ActionResult<Product>> AddNewProduct(Product product)
        {
            // Validate and assign category
            var result = await ValidateAndAssignCategory(product);
            if (result is BadRequestObjectResult badRequest) return badRequest;

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }

        // POST Bulk Products - api/Product/bulk
        [HttpPost("bulk")]
        public async Task<ActionResult<IEnumerable<Product>>> AddMultipleProducts(List<Product> products)
        {
            var validatedProducts = new List<Product>();

            foreach (var product in products)
            {
                var result = await ValidateAndAssignCategory(product);
                if (result is BadRequestObjectResult badRequest)
                    return badRequest;

                validatedProducts.Add(product);
            }

            await _context.Products.AddRangeAsync(validatedProducts);
            await _context.SaveChangesAsync();

            return Ok(validatedProducts);
        }

        // PUT: api/Product/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, Product product)
        {
            if (id != product.Id)
                return BadRequest("Product ID mismatch.");

            // Validate and assign category
            var result = await ValidateAndAssignCategory(product);
            if (result is BadRequestObjectResult badRequest) return badRequest;

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        // DELETE: api/Product/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        /// <summary>
        /// Validates category info from the product object and assigns CategoryId.
        /// </summary>
        private async Task<IActionResult?> ValidateAndAssignCategory(Product product)
        {
            if (product.CategoryId > 0)
            {
                var category = await _context.Categories.FindAsync(product.CategoryId);
                if (category == null)
                    return BadRequest("Category not found. Please provide a valid CategoryId.");
            }
            else if (product.Category != null && !string.IsNullOrWhiteSpace(product.Category.Name))
            {
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == product.Category.Name.ToLower());

                if (category == null)
                    return BadRequest($"Category '{product.Category.Name}' not found. Please provide a valid Category.");

                product.CategoryId = category.Id;
                product.Category = null; // Prevent EF from trying to insert a new category
            }
            else
            {
                return BadRequest("Product must include a valid CategoryId or Category Name.");
            }

            return null;
        }
    }
}
