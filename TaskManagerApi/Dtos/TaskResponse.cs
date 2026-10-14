using System.Linq.Expressions;
using TaskManagerApi.Models;

namespace TaskManagerApi.Dtos
{
    /// <summary>A task as returned by the API.</summary>
    public class TaskResponse
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public bool IsCompleted { get; set; }
        public Priority Priority { get; set; }
        public int? CategoryId { get; set; }

        /// <summary>Name of the task's category, or null if it has none.</summary>
        public string? CategoryName { get; set; }

        // Used inside EF queries (Select) so only these columns are read and
        // no Category object, with its list of sibling tasks, is loaded.
        public static readonly Expression<Func<TaskItem, TaskResponse>> Projection = t => new TaskResponse
        {
            Id = t.Id,
            Title = t.Title,
            IsCompleted = t.IsCompleted,
            Priority = t.Priority,
            CategoryId = t.CategoryId,
            CategoryName = t.Category != null ? t.Category.Name : null
        };
    }
}
