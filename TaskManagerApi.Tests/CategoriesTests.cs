using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskManagerApi.Tests
{
    public class CategoriesTests : IClassFixture<ApiFactory>
    {
        private readonly HttpClient _client;

        public CategoriesTests(ApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        private async Task<int> CreateCategory(string name)
        {
            var response = await _client.PostJson("/api/categories", $$"""{"name":"{{name}}"}""");
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (await response.ReadJson()).GetProperty("id").GetInt32();
        }

        private async Task<int> CreateTask(string title, int categoryId)
        {
            var response = await _client.PostJson("/api/tasks", $$"""{"title":"{{title}}","categoryId":{{categoryId}}}""");
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (await response.ReadJson()).GetProperty("id").GetInt32();
        }

        [Fact]
        public async Task Create_returns_201_with_location()
        {
            var response = await _client.PostJson("/api/categories", """{"name":"Work"}""");

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = await response.ReadJson();
            Assert.Equal("Work", body.GetProperty("name").GetString());
            Assert.Equal($"/api/Categories/{body.GetProperty("id").GetInt32()}", response.Headers.Location!.AbsolutePath);
        }

        [Fact]
        public async Task Create_without_name_returns_400()
        {
            var response = await _client.PostJson("/api/categories", "{}");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Get_by_id_returns_category_with_its_tasks()
        {
            var id = await CreateCategory("Home");
            await CreateTask("Clean", id);

            var body = await (await _client.GetAsync($"/api/categories/{id}")).ReadJson();

            Assert.Equal("Home", body.GetProperty("name").GetString());
            Assert.Equal("Clean", body.GetProperty("tasks")[0].GetProperty("title").GetString());
        }

        [Fact]
        public async Task Get_missing_returns_404()
        {
            var response = await _client.GetAsync("/api/categories/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // Review point 3: the list endpoint used to show "tasks": [] for every category.
        [Fact]
        public async Task Get_all_includes_each_categorys_tasks()
        {
            var id = await CreateCategory("Listed");
            await CreateTask("One", id);
            await CreateTask("Two", id);

            var list = await (await _client.GetAsync("/api/categories")).ReadJson();
            var listed = list.EnumerateArray().Single(c => c.GetProperty("id").GetInt32() == id);

            Assert.Equal(2, listed.GetProperty("tasks").GetArrayLength());
        }

        [Fact]
        public async Task Update_renames_category()
        {
            var id = await CreateCategory("Old");

            var response = await _client.PutJson($"/api/categories/{id}", """{"name":"New"}""");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("New", (await response.ReadJson()).GetProperty("name").GetString());
            var reloaded = await (await _client.GetAsync($"/api/categories/{id}")).ReadJson();
            Assert.Equal("New", reloaded.GetProperty("name").GetString());
        }

        [Fact]
        public async Task Update_missing_returns_404()
        {
            var response = await _client.PutJson("/api/categories/999999", """{"name":"x"}""");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Delete_keeps_tasks_without_a_category()
        {
            var id = await CreateCategory("Doomed");
            var taskId = await CreateTask("Survivor", id);

            var response = await _client.DeleteAsync($"/api/categories/{id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/categories/{id}")).StatusCode);
            var task = await (await _client.GetAsync($"/api/tasks/{taskId}")).ReadJson();
            Assert.Equal(JsonValueKind.Null, task.GetProperty("categoryId").ValueKind);
        }

        [Fact]
        public async Task Delete_missing_returns_404()
        {
            var response = await _client.DeleteAsync("/api/categories/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // Review point 1: an explicit id used to reach SQL Server's identity column (500).
        [Fact]
        public async Task Create_ignores_id_sent_by_client()
        {
            var response = await _client.PostJson("/api/categories", """{"id":424242,"name":"WithId"}""");

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotEqual(424242, (await response.ReadJson()).GetProperty("id").GetInt32());
        }

        // Review point 2: a nested tasks array used to create tasks behind the API's back.
        [Fact]
        public async Task Create_ignores_nested_tasks()
        {
            var response = await _client.PostJson("/api/categories", """{"name":"Nested","tasks":[{"title":"Hidden"}]}""");

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal(0, (await response.ReadJson()).GetProperty("tasks").GetArrayLength());
            var tasks = await _client.GetFromJsonAsync<JsonElement>("/api/tasks");
            Assert.DoesNotContain(tasks.EnumerateArray(), t => t.GetProperty("title").GetString() == "Hidden");
        }
    }
}
