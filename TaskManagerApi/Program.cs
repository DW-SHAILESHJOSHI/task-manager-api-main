using System.Text.Json.Serialization;
using TaskManagerApi.Models;
using TaskManagerApi.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ASP.NET's default JSON settings also accept numbers written as strings ("5"),
// so the OpenAPI document described every int as ["integer", "string"]. Swagger UI
// cannot handle a parameter with two types and discarded the typed {id}, so no
// GET/PUT/DELETE by id could be sent from Swagger. Strict numbers make ints plain
// "integer" in the document (HTTP JSON options) and in the API itself (MVC options).
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict);
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Generates the OpenAPI (Swagger) document describing every endpoint.
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Serves the document at /openapi/v1.json
    app.MapOpenApi();

    // Interactive Swagger UI at /swagger, built from the document above.
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Task Manager API v1");
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Makes the generated Program class visible to WebApplicationFactory in TaskManagerApi.Tests.
public partial class Program { }