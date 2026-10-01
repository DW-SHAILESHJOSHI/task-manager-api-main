namespace TaskManagerApi.Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public bool IsCompleted { get; set; }
        public Priority Priority { get; set; } = Priority.Low;

        // Foreign key
        public int? CategoryId { get; set; }

        // Navigation property
        public Category? Category { get; set; }
    }
}