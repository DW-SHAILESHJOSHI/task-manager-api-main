using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagerApi.Models;
using TaskManagerApi.Data;

namespace TaskManagerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TasksController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tasks = await _context.Tasks.Include(t => t.Category).ToListAsync();
            return Ok(tasks);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var task = await _context.Tasks
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (task == null) return NotFound();
            return Ok(task);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TaskItem newTask)
        {
            if (!await CategoryExists(newTask.CategoryId))
                return BadRequest($"Category {newTask.CategoryId} does not exist.");

            _context.Tasks.Add(newTask);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = newTask.Id }, newTask);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] TaskItem updatedTask)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null) return NotFound();

            if (!await CategoryExists(updatedTask.CategoryId))
                return BadRequest($"Category {updatedTask.CategoryId} does not exist.");

            bool wasJustCompleted = !task.IsCompleted && updatedTask.IsCompleted;

            task.Title = updatedTask.Title;
            task.Priority = updatedTask.Priority;
            task.IsCompleted = updatedTask.IsCompleted;
            task.CategoryId = updatedTask.CategoryId;

            await _context.SaveChangesAsync();

            if (wasJustCompleted)
            {
                NotificationBase notification = new EmailReminder();
                notification.SendReminder(task.Title);
            }

            return Ok(task);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null) return NotFound();

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // A task without a category is allowed; only reject ids that don't exist.
        private async Task<bool> CategoryExists(int? categoryId)
        {
            return categoryId == null
                || await _context.Categories.AnyAsync(c => c.Id == categoryId);
        }
    }
}