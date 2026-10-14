using TaskManagerApi.Models;

namespace TaskManagerApi.Dtos
{
    /// <summary>Body for creating or updating a task.</summary>
    public class TaskRequest
    {
        /// <summary>The task title.</summary>
        /// <example>Learn EF Core</example>
        public required string Title { get; set; }

        /// <summary>Whether the task is done. Setting it to true sends a notification.</summary>
        /// <example>false</example>
        public bool IsCompleted { get; set; }

        /// <summary>Low, Medium or High. Defaults to Low.</summary>
        /// <example>High</example>
        public Priority Priority { get; set; } = Priority.Low;

        /// <summary>The category the task belongs to, or null for none.</summary>
        /// <example>1</example>
        public int? CategoryId { get; set; }
    }
}
