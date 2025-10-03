using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Model.Entities;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly AuthDbContext _context;

        public CategoryController(AuthDbContext context)
        {
            _context = context;
        }

        // GET: api/Category
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            return await _context.Categories
                                 .Include(c => c.Products)
                                 .ToListAsync();
        }

        // GET: api/Category/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            var category = await _context.Categories
                                         .Include(c => c.Products)
                                         .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound();

            return category;
        }

        // POST: api/Category
        // Supports single or bulk insertion
        [HttpPost]
        public async Task<IActionResult> PostCategories([FromBody] object body)
        {
            if (body == null)
                return BadRequest("No category data provided.");

            try
            {
                // Try single Category
                var singleCategory = System.Text.Json.JsonSerializer.Deserialize<Category>(
                    body.ToString(),
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (singleCategory != null && !string.IsNullOrWhiteSpace(singleCategory.Name))
                {
                    singleCategory.Id = 0; // EF Core auto generates ID
                    _context.Categories.Add(singleCategory);
                    await _context.SaveChangesAsync();

                    return CreatedAtAction(nameof(GetCategory), new { id = singleCategory.Id }, singleCategory);
                }
            }
            catch { }

            try
            {
                // Try list of Categories
                var categoryList = System.Text.Json.JsonSerializer.Deserialize<List<Category>>(
                    body.ToString(),
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (categoryList != null && categoryList.Any())
                {
                    categoryList.ForEach(c => c.Id = 0);
                    _context.Categories.AddRange(categoryList);
                    await _context.SaveChangesAsync();

                    return Ok(categoryList);
                }
            }
            catch { }

            return BadRequest("Invalid category format. Must be a single object or an array of objects.");
        }

        // PUT: api/Category/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, Category category)
        {
            if (id != category.Id)
                return BadRequest();

            _context.Entry(category).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        // DELETE: api/Category/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
}