using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagerApi.Models;
using TaskManagerApi.Data;
using TaskManagerApi.Dtos;

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

        /// <summary>Gets all tasks with their category name.</summary>
        [HttpGet]
        [ProducesResponseType<List<TaskResponse>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var tasks = await _context.Tasks
                .OrderBy(t => t.Id)
                .Select(TaskResponse.Projection)
                .ToListAsync();
            return Ok(tasks);
        }

        /// <summary>Gets one task with its category name.</summary>
        /// <param name="id">The task id.</param>
        [HttpGet("{id}")]
        [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var task = await FindResponse(id);
            if (task == null) return NotFound();
            return Ok(task);
        }

        /// <summary>Creates a new task, optionally in a category.</summary>
        /// <response code="400">The title is missing or categoryId does not exist.</response>
        [HttpPost]
        [ProducesResponseType<TaskResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] TaskRequest request)
        {
            if (!await CategoryExists(request.CategoryId))
                return BadRequest($"Category {request.CategoryId} does not exist.");

            var newTask = new TaskItem
            {
                Title = request.Title,
                Priority = request.Priority,
                IsCompleted = request.IsCompleted,
                CategoryId = request.CategoryId
            };
            _context.Tasks.Add(newTask);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = newTask.Id }, await FindResponse(newTask.Id));
        }

        /// <summary>Updates a task. Marking it completed sends an email notification.</summary>
        /// <param name="id">The task id.</param>
        /// <param name="request">The new values for the task.</param>
        /// <response code="400">The title is missing or categoryId does not exist.</response>
        [HttpPut("{id}")]
        [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] TaskRequest request)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null) return NotFound();

            if (!await CategoryExists(request.CategoryId))
                return BadRequest($"Category {request.CategoryId} does not exist.");

            bool wasJustCompleted = !task.IsCompleted && request.IsCompleted;

            task.Title = request.Title;
            task.Priority = request.Priority;
            task.IsCompleted = request.IsCompleted;
            task.CategoryId = request.CategoryId;

            await _context.SaveChangesAsync();

            if (wasJustCompleted)
            {
                NotificationBase notification = new EmailReminder();
                notification.SendReminder(task.Title);
            }

            return Ok(await FindResponse(task.Id));
        }

        /// <summary>Deletes a task.</summary>
        /// <param name="id">The task id.</param>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null) return NotFound();

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private Task<TaskResponse?> FindResponse(int id)
        {
            return _context.Tasks
                .Where(t => t.Id == id)
                .Select(TaskResponse.Projection)
                .FirstOrDefaultAsync();
        }

        // A task without a category is allowed; only reject ids that don't exist.
        private async Task<bool> CategoryExists(int? categoryId)
        {
            return categoryId == null
                || await _context.Categories.AnyAsync(c => c.Id == categoryId);
        }
    }
}