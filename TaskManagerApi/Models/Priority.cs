using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskManagerApi.Models
{
    /// <summary>How important a task is.</summary>
    [JsonConverter(typeof(PriorityJsonConverter))]
    public enum Priority
    {
        Low,
        Medium,
        High
    }

    // Reads and writes Priority as "Low"/"Medium"/"High". Integers are rejected,
    // otherwise any number (e.g. 7) would be accepted as an undefined Priority.
    public class PriorityJsonConverter : JsonStringEnumConverter<Priority>
    {
        public PriorityJsonConverter() : base(namingPolicy: null, allowIntegerValues: false)
        {
        }
    }
}
