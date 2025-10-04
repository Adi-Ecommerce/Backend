using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Model.Entities;
using Backend.Model.DTOs;

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
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
        {
            var categories = await _context.Categories
                                           .Include(c => c.Products)
                                           .ToListAsync();

            var categoryDtos = categories.Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Products = c.Products?.Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Description = p.Description,
                    Image = p.Image,
                    StockQuantity = p.StockQuantity,
                    CategoryId = p.CategoryId
                }).ToList()
            }).ToList();

            return Ok(categoryDtos);
        }

        // GET: api/Category/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDto>> GetCategory(int id)
        {
            var category = await _context.Categories
                                         .Include(c => c.Products)
                                         .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound();

            var categoryDto = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Products = category.Products?.Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Description = p.Description,
                    Image = p.Image,
                    StockQuantity = p.StockQuantity,
                    CategoryId = p.CategoryId
                }).ToList()
            };

            return Ok(categoryDto);
        }

        // POST: api/Category
        [HttpPost]
        public async Task<IActionResult> PostCategories([FromBody] object body)
        {
            if (body == null)
                return BadRequest("No category data provided.");

            try
            {
                // Try to deserialize into a single CategoryDto
                var singleCategory = System.Text.Json.JsonSerializer.Deserialize<CategoryDto>(
                    body.ToString(),
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (singleCategory != null && !string.IsNullOrWhiteSpace(singleCategory.Name))
                {
                    var category = new Category
                    {
                        Name = singleCategory.Name,
                        Description = singleCategory.Description
                    };

                    _context.Categories.Add(category);
                    await _context.SaveChangesAsync();

                    // Return the created category as a DTO
                    var createdCategoryDto = new CategoryDto
                    {
                        Id = category.Id,
                        Name = category.Name,
                        Description = category.Description
                    };

                    return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, createdCategoryDto);
                }
            }
            catch { }

            try
            {
                // Try to deserialize into a list of CategoryDto
                var categoryList = System.Text.Json.JsonSerializer.Deserialize<List<CategoryDto>>(
                    body.ToString(),
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (categoryList != null && categoryList.Any())
                {
                    var categories = categoryList.Select(c => new Category
                    {
                        Name = c.Name,
                        Description = c.Description
                    }).ToList();

                    _context.Categories.AddRange(categories);
                    await _context.SaveChangesAsync();

                    var createdDtos = categories.Select(c => new CategoryDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description
                    }).ToList();

                    return Ok(createdDtos);
                }
            }
            catch { }

            return BadRequest("Invalid category format. Must be a single object or an array of objects.");
        }

        // PUT: api/Category/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryDto dto)
        {
            if (dto == null || id != dto.Id)
                return BadRequest("Invalid request data.");

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            category.Name = dto.Name;
            category.Description = dto.Description;

            _context.Entry(category).State = EntityState.Modified;
            await _context.SaveChangesAsync();

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
    }
}