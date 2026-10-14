using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagerApi.Models;
using TaskManagerApi.Data;
using TaskManagerApi.Dtos;

namespace TaskManagerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>Gets all categories with their tasks.</summary>
        [HttpGet]
        [ProducesResponseType<List<CategoryResponse>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _context.Categories
                .OrderBy(c => c.Id)
                .Select(CategoryResponse.Projection)
                .ToListAsync();
            return Ok(categories);
        }

        /// <summary>Gets one category together with its tasks.</summary>
        /// <param name="id">The category id.</param>
        [HttpGet("{id}")]
        [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var category = await FindResponse(id);
            if (category == null) return NotFound();
            return Ok(category);
        }

        /// <summary>Creates a new category.</summary>
        [HttpPost]
        [ProducesResponseType<CategoryResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CategoryRequest request)
        {
            var newCategory = new Category { Name = request.Name };
            _context.Categories.Add(newCategory);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = newCategory.Id }, await FindResponse(newCategory.Id));
        }

        /// <summary>Renames an existing category.</summary>
        /// <param name="id">The category id.</param>
        /// <param name="request">The new category name.</param>
        [HttpPut("{id}")]
        [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] CategoryRequest request)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            category.Name = request.Name;

            await _context.SaveChangesAsync();
            return Ok(await FindResponse(id));
        }

        /// <summary>Deletes a category. Its tasks are kept, with no category.</summary>
        /// <param name="id">The category id.</param>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            // Load the category's tasks so EF Core sets their CategoryId to null
            // instead of the database rejecting the delete on the foreign key.
            var category = await _context.Categories
                .Include(c => c.Tasks)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (category == null) return NotFound();

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private Task<CategoryResponse?> FindResponse(int id)
        {
            return _context.Categories
                .Where(c => c.Id == id)
                .Select(CategoryResponse.Projection)
                .FirstOrDefaultAsync();
        }
    }
}
