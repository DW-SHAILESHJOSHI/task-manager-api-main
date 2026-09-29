namespace TaskManagerApi.Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public bool IsCompleted { get; set; }
        public string Priority { get; set; } = "Low";

        // Foreign key
        public int? CategoryId { get; set; }

        // Navigation property
        public Category? Category { get; set; }
    }
}