using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagerApi.Models;
using TaskManagerApi.Data;

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

        /// <summary>Gets all categories.</summary>
        [HttpGet]
        [ProducesResponseType<List<Category>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _context.Categories.ToListAsync();
            return Ok(categories);
        }

        /// <summary>Gets one category together with its tasks.</summary>
        /// <param name="id">The category id.</param>
        [HttpGet("{id}")]
        [ProducesResponseType<Category>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Tasks)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (category == null) return NotFound();
            return Ok(category);
        }

        /// <summary>Creates a new category.</summary>
        [HttpPost]
        [ProducesResponseType<Category>(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] Category newCategory)
        {
            _context.Categories.Add(newCategory);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = newCategory.Id }, newCategory);
        }

        /// <summary>Renames an existing category.</summary>
        /// <param name="id">The category id.</param>
        /// <param name="updatedCategory">The new category name.</param>
        [HttpPut("{id}")]
        [ProducesResponseType<Category>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] Category updatedCategory)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            category.Name = updatedCategory.Name;

            await _context.SaveChangesAsync();
            return Ok(category);
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
    }
}