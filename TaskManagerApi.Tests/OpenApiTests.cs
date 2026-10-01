using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskManagerApi.Tests
{
    public class OpenApiTests : IClassFixture<ApiFactory>
    {
        private readonly HttpClient _client;

        public OpenApiTests(ApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Swagger_ui_is_served()
        {
            var response = await _client.GetAsync("/swagger/index.html");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Document_lists_all_ten_crud_operations()
        {
            var doc = await _client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");

            var operations = doc.GetProperty("paths").EnumerateObject()
                .SelectMany(p => p.Value.EnumerateObject().Select(op => $"{op.Name.ToUpper()} {p.Name}"))
                .ToList();

            Assert.Equal(10, operations.Count);
        }

        // Swagger UI could not send {id} requests while ids were typed ["integer", "string"].
        [Theory]
        [InlineData("/api/Tasks/{id}")]
        [InlineData("/api/Categories/{id}")]
        public async Task Id_parameter_is_a_plain_integer(string path)
        {
            var doc = await _client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");

            foreach (var operation in doc.GetProperty("paths").GetProperty(path).EnumerateObject())
            {
                var schema = operation.Value.GetProperty("parameters")[0].GetProperty("schema");
                Assert.Equal("integer", schema.GetProperty("type").GetString());
            }
        }

        [Fact]
        public async Task Priority_is_documented_as_an_enum()
        {
            var doc = await _client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");

            var values = doc.GetProperty("components").GetProperty("schemas").GetProperty("Priority")
                .GetProperty("enum").EnumerateArray().Select(v => v.GetString());

            Assert.Equal(new[] { "Low", "Medium", "High" }, values);
        }

        // Request bodies must be DTOs: entities made Swagger's example body circular.
        [Theory]
        [InlineData("/api/Tasks", "TaskRequest")]
        [InlineData("/api/Categories", "CategoryRequest")]
        public async Task Create_body_uses_request_dto(string path, string schemaName)
        {
            var doc = await _client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");

            var reference = doc.GetProperty("paths").GetProperty(path).GetProperty("post")
                .GetProperty("requestBody").GetProperty("content").GetProperty("application/json")
                .GetProperty("schema").GetProperty("$ref").GetString();

            Assert.Equal($"#/components/schemas/{schemaName}", reference);
        }
    }
}
