namespace TaskManagerApi.Models
{
    public class Category
    {
        public int Id { get; set; }
        public required string Name { get; set; }

        // Navigation property - one category can have many tasks
        public List<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}