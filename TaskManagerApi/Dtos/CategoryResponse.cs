using System.Linq.Expressions;
using TaskManagerApi.Models;

namespace TaskManagerApi.Dtos
{
    /// <summary>A category and the tasks in it, as returned by the API.</summary>
    public class CategoryResponse
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public List<CategoryTaskSummary> Tasks { get; set; } = new List<CategoryTaskSummary>();

        // Loads the category's tasks in the same query, so the list endpoint
        // shows each category's real tasks instead of an empty list.
        public static readonly Expression<Func<Category, CategoryResponse>> Projection = c => new CategoryResponse
        {
            Id = c.Id,
            Name = c.Name,
            Tasks = c.Tasks
                .OrderBy(t => t.Id)
                .Select(t => new CategoryTaskSummary
                {
                    Id = t.Id,
                    Title = t.Title,
                    IsCompleted = t.IsCompleted,
                    Priority = t.Priority
                })
                .ToList()
        };
    }

    /// <summary>A task inside a category response. The category is the parent, so it isn't repeated.</summary>
    public class CategoryTaskSummary
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public bool IsCompleted { get; set; }
        public string Priority { get; set; } = "Low";
    }
}
