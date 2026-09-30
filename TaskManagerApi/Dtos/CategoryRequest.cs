namespace TaskManagerApi.Dtos
{
    /// <summary>Body for creating or updating a category.</summary>
    public class CategoryRequest
    {
        /// <summary>The category name.</summary>
        /// <example>Work</example>
        public required string Name { get; set; }
    }
}
