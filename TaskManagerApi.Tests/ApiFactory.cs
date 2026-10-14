using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManagerApi.Data;

namespace TaskManagerApi.Tests
{
    // Runs the real API in memory against its own LocalDB database, created from
    // the EF Core migrations before the tests and deleted afterwards.
    public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly string _connectionString =
            $"Server=(localdb)\\MSSQLLocalDB;Database=TaskManagerDb_Tests_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True;";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        }

        public async Task InitializeAsync()
        {
            using var scope = Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            using (var scope = Services.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureDeletedAsync();
            }
            await base.DisposeAsync();
        }
    }

    // Small helpers so tests read as "post this JSON, expect that status".
    public static class HttpExtensions
    {
        public static Task<HttpResponseMessage> PostJson(this HttpClient client, string url, string json) =>
            client.PostAsync(url, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        public static Task<HttpResponseMessage> PutJson(this HttpClient client, string url, string json) =>
            client.PutAsync(url, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        public static async Task<JsonElement> ReadJson(this HttpResponseMessage response) =>
            await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
