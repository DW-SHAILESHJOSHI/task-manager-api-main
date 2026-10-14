using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskManagerApi.Tests
{
    public class TasksTests : IClassFixture<ApiFactory>
    {
        private readonly HttpClient _client;

        public TasksTests(ApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        private async Task<int> CreateCategory(string name)
        {
            var response = await _client.PostJson("/api/categories", $$"""{"name":"{{name}}"}""");
            return (await response.ReadJson()).GetProperty("id").GetInt32();
        }

        private async Task<JsonElement> CreateTask(string json)
        {
            var response = await _client.PostJson("/api/tasks", json);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return await response.ReadJson();
        }

        [Fact]
        public async Task Create_returns_201_with_category_name()
        {
            var categoryId = await CreateCategory("Work");

            var response = await _client.PostJson("/api/tasks", $$"""{"title":"Learn EF Core","priority":"High","categoryId":{{categoryId}}}""");

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = await response.ReadJson();
            Assert.Equal("Learn EF Core", body.GetProperty("title").GetString());
            Assert.Equal("High", body.GetProperty("priority").GetString());
            Assert.Equal("Work", body.GetProperty("categoryName").GetString());
            Assert.NotNull(response.Headers.Location);
        }

        [Fact]
        public async Task Create_defaults_priority_to_low()
        {
            var task = await CreateTask("""{"title":"No priority"}""");

            Assert.Equal("Low", task.GetProperty("priority").GetString());
        }

        [Fact]
        public async Task Create_without_title_returns_400()
        {
            var response = await _client.PostJson("/api/tasks", """{"priority":"High"}""");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Create_with_unknown_category_returns_400()
        {
            var response = await _client.PostJson("/api/tasks", """{"title":"x","categoryId":999999}""");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Get_by_id_and_missing_id()
        {
            var id = (await CreateTask("""{"title":"Find me"}""")).GetProperty("id").GetInt32();

            Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/tasks/{id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/tasks/999999")).StatusCode);
        }

        [Fact]
        public async Task Update_changes_fields_and_category()
        {
            var categoryId = await CreateCategory("Target");
            var id = (await CreateTask("""{"title":"Before"}""")).GetProperty("id").GetInt32();

            var response = await _client.PutJson($"/api/tasks/{id}",
                $$"""{"title":"After","priority":"Medium","isCompleted":true,"categoryId":{{categoryId}}}""");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.ReadJson();
            Assert.Equal("After", body.GetProperty("title").GetString());
            Assert.Equal("Medium", body.GetProperty("priority").GetString());
            Assert.True(body.GetProperty("isCompleted").GetBoolean());
            Assert.Equal("Target", body.GetProperty("categoryName").GetString());
        }

        [Fact]
        public async Task Update_missing_returns_404_and_unknown_category_returns_400()
        {
            var id = (await CreateTask("""{"title":"x"}""")).GetProperty("id").GetInt32();

            Assert.Equal(HttpStatusCode.NotFound, (await _client.PutJson("/api/tasks/999999", """{"title":"x"}""")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await _client.PutJson($"/api/tasks/{id}", """{"title":"x","categoryId":999999}""")).StatusCode);
        }

        [Fact]
        public async Task Delete_removes_task()
        {
            var id = (await CreateTask("""{"title":"Delete me"}""")).GetProperty("id").GetInt32();

            Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/tasks/{id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/tasks/{id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync($"/api/tasks/{id}")).StatusCode);
        }

        // Review point 1: an explicit id used to reach SQL Server's identity column (500).
        [Fact]
        public async Task Create_ignores_id_sent_by_client()
        {
            var task = await CreateTask("""{"id":424242,"title":"With id"}""");

            Assert.NotEqual(424242, task.GetProperty("id").GetInt32());
        }

        // Review point 2: a nested category used to be inserted, bypassing the category check.
        [Fact]
        public async Task Create_ignores_nested_category()
        {
            var task = await CreateTask("""{"title":"Sneaky task","category":{"name":"Sneaky"}}""");

            Assert.Equal(JsonValueKind.Null, task.GetProperty("categoryId").ValueKind);
            var categories = await _client.GetFromJsonAsync<JsonElement>("/api/categories");
            Assert.DoesNotContain(categories.EnumerateArray(), c => c.GetProperty("name").GetString() == "Sneaky");
        }

        // Review point 4: each task used to embed its category with every sibling task.
        [Fact]
        public async Task Get_all_returns_flat_tasks_without_nested_objects()
        {
            var categoryId = await CreateCategory("Busy");
            for (var i = 0; i < 5; i++)
                await CreateTask($$"""{"title":"Busy {{i}}","categoryId":{{categoryId}}}""");

            var tasks = await _client.GetFromJsonAsync<JsonElement>("/api/tasks");

            foreach (var task in tasks.EnumerateArray())
            {
                Assert.False(task.TryGetProperty("category", out _), "task should not embed a category object");
                Assert.False(task.TryGetProperty("tasks", out _), "task should not embed sibling tasks");
            }
            Assert.Equal(5, tasks.EnumerateArray().Count(t => t.GetProperty("categoryName").GetString() == "Busy"));
        }

        // Review point 5: any string used to be accepted as a priority.
        [Theory]
        [InlineData("\"banana\"")]
        [InlineData("7")]
        [InlineData("null")]
        public async Task Create_with_invalid_priority_returns_400(string priority)
        {
            var response = await _client.PostJson("/api/tasks", $$"""{"title":"x","priority":{{priority}}}""");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData("high", "High")]
        [InlineData("MEDIUM", "Medium")]
        [InlineData("Low", "Low")]
        public async Task Priority_names_are_case_insensitive(string sent, string stored)
        {
            var task = await CreateTask($$"""{"title":"x","priority":"{{sent}}"}""");

            Assert.Equal(stored, task.GetProperty("priority").GetString());
        }

        // Numbers are strict so the OpenAPI document can describe them as plain integers.
        [Fact]
        public async Task Number_sent_as_string_returns_400()
        {
            var response = await _client.PostJson("/api/tasks", """{"title":"x","categoryId":"1"}""");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
