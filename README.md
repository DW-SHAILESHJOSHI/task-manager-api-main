# Task Manager API

A RESTful Task Management API built with **ASP.NET Core (.NET 10)**, demonstrating core Object-Oriented Programming principles (Abstraction, Inheritance, Polymorphism, Encapsulation) integrated into a real CRUD API — not just theoretical examples.

---

## 📋 Overview

This project is a Task Manager API backed by SQL Server through Entity Framework Core. It supports creating, reading, updating, and deleting tasks and the categories they belong to. It also demonstrates a practical use of **polymorphism**: when a task is marked as completed, the API dynamically triggers a notification (Email or SMS) using an abstract base class design.

---

## 🚀 Features

- Full CRUD operations for tasks and categories (Create, Read, Update, Delete)
- One-to-many relationship: a category has many tasks
- RESTful endpoint design following standard HTTP conventions
- Proper HTTP status codes (`200`, `201`, `204`, `400`, `404`)
- Request and response DTOs: clients send only the fields they may set, and responses never embed database entities
- Input validation: required fields, existing category, priority limited to `Low` / `Medium` / `High`
- OOP-based notification system triggered on task completion
- SQL Server persistence with EF Core migrations
- Integration tests run against a real SQL Server (LocalDB) database

---

## 🛠️ Tech Stack

| Technology | Purpose |
|---|---|
| **C# / .NET 10** | Core language and runtime |
| **ASP.NET Core Web API** | REST API framework |
| **Entity Framework Core** | ORM and migrations |
| **SQL Server / LocalDB** | Database |
| **OpenAPI + Swagger UI** | Interactive API documentation |
| **xUnit + WebApplicationFactory** | Integration tests |
| **Kestrel** | Built-in web server |

---

## 📂 Project Structure

```
TaskManagerApi.slnx                  # Solution: API + tests
TaskManagerApi/
├── Controllers/
│   ├── TasksController.cs           # Task endpoints (CRUD)
│   └── CategoriesController.cs      # Category endpoints (CRUD)
├── Data/
│   └── AppDbContext.cs              # EF Core DbContext
├── Dtos/
│   ├── TaskRequest.cs               # Body for creating/updating a task
│   ├── TaskResponse.cs              # Task as returned by the API
│   ├── CategoryRequest.cs           # Body for creating/updating a category
│   └── CategoryResponse.cs          # Category (with its tasks) as returned by the API
├── Migrations/                      # EF Core database migrations
├── Models/
│   ├── TaskItem.cs                  # Task entity
│   ├── Category.cs                  # Category entity
│   ├── Priority.cs                  # Low / Medium / High enum
│   ├── NotificationBase.cs          # Abstract base class for notifications
│   ├── EmailReminder.cs             # Email notification implementation
│   └── SmsReminder.cs               # SMS notification implementation
├── Program.cs                       # App entry point & configuration
├── TaskManagerApi.http              # Ready-made requests for every endpoint
└── appsettings.json                 # App configuration
TaskManagerApi.Tests/                # Integration tests (xUnit)
```

---

## 🧩 Design Highlights

### Object-Oriented Design

- **`NotificationBase`** is an abstract class defining a contract (`SendReminder`) that all notification types must implement.
- **`EmailReminder`** and **`SmsReminder`** each provide their own implementation — a practical demonstration of **inheritance** and **runtime polymorphism**.
- When a task transitions from incomplete to complete via the `PUT` endpoint, the API instantiates a notification object (typed as the abstract base class) and calls its `SendReminder()` method — the correct derived implementation runs automatically at runtime.

```csharp
NotificationBase notification = new EmailReminder();
notification.SendReminder(task.Title);
```

### DTOs instead of entities

Controllers never accept or return the EF Core entities (`TaskItem`, `Category`) directly:

- **Requests** (`TaskRequest`, `CategoryRequest`) contain only the fields a client may set. A client cannot choose an `id` or slip in a nested `category` / `tasks` object that EF Core would insert.
- **Responses** (`TaskResponse`, `CategoryResponse`) are built with a LINQ `Select` projection, so EF reads only the needed columns. A task carries `categoryId` and `categoryName` instead of the whole category, which keeps the payload flat with no circular references.

---

## 📡 API Endpoints

### Tasks

| Method | Endpoint | Description | Responses |
|---|---|---|---|
| `GET` | `/api/tasks` | Get all tasks with their category name | `200 OK` |
| `GET` | `/api/tasks/{id}` | Get a single task by ID | `200 OK` / `404 Not Found` |
| `POST` | `/api/tasks` | Create a new task | `201 Created` / `400 Bad Request` (missing title, unknown category, invalid priority) |
| `PUT` | `/api/tasks/{id}` | Update an existing task | `200 OK` / `400 Bad Request` / `404 Not Found` |
| `DELETE` | `/api/tasks/{id}` | Delete a task | `204 No Content` / `404 Not Found` |

### Categories

| Method | Endpoint | Description | Responses |
|---|---|---|---|
| `GET` | `/api/categories` | Get all categories with their tasks | `200 OK` |
| `GET` | `/api/categories/{id}` | Get a category with its tasks | `200 OK` / `404 Not Found` |
| `POST` | `/api/categories` | Create a new category | `201 Created` / `400 Bad Request` (missing name) |
| `PUT` | `/api/categories/{id}` | Rename a category | `200 OK` / `400 Bad Request` / `404 Not Found` |
| `DELETE` | `/api/categories/{id}` | Delete a category; its tasks are kept with no category | `204 No Content` / `404 Not Found` |

### Request/Response Examples

**Create a task**
```bash
curl -X POST http://localhost:5179/api/tasks \
  -H "Content-Type: application/json" \
  -d '{"title":"Learn EF Core","priority":"High","categoryId":1}'
```
Response `201 Created`:
```json
{ "id": 1, "title": "Learn EF Core", "isCompleted": false, "priority": "High", "categoryId": 1, "categoryName": "Work" }
```

`priority` must be `Low`, `Medium` or `High` (case-insensitive) and defaults to `Low`. Any other value returns `400 Bad Request`.

**Mark a task as completed (triggers notification)**
```bash
curl -X PUT http://localhost:5179/api/tasks/1 \
  -H "Content-Type: application/json" \
  -d '{"title":"Learn C#","priority":"High","isCompleted":true}'
```
Console output on the server:
```
Email sent: Task 'Learn C#' is completed!
```

---

## ⚙️ Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server LocalDB (installed with Visual Studio), or any SQL Server instance
- EF Core CLI: `dotnet tool install --global dotnet-ef`

In Development the API uses the LocalDB connection string in `appsettings.Development.json`. To use another SQL Server, change `ConnectionStrings:DefaultConnection`.

### Run Locally

```bash
git clone https://github.com/DW-SHAILESHJOSHI/task-manager-api-main.git
cd task-manager-api-main/TaskManagerApi
dotnet ef database update   # creates the database and applies migrations
dotnet run
```

The API will start on `http://localhost:5179` and open Swagger UI in your browser.

### Swagger UI

In Development, the API documents itself:

| URL | What it is |
|---|---|
| `http://localhost:5179/swagger` | Swagger UI: browse every endpoint and call it with **Try it out** |
| `http://localhost:5179/openapi/v1.json` | The raw OpenAPI 3.1 document Swagger UI is built from |

Each endpoint shows its description, request body and every status code it can return. The document is generated by ASP.NET Core's built-in `AddOpenApi()`, with descriptions taken from the `///` comments and `[ProducesResponseType]` attributes on the controllers.

### Test the Endpoints

Using Swagger UI (above), or `curl`:
```bash
curl http://localhost:5179/api/tasks
```

Or open `TaskManagerApi.http` in VS Code (REST Client extension) or Visual Studio and send the ready-made requests, or use [Postman](https://www.postman.com/).

### Run the Automated Tests

From the repository root:
```bash
dotnet test
```

The tests start the API in memory, create a temporary LocalDB database from the migrations, run every CRUD scenario plus the validation and OpenAPI checks, and delete the database afterwards. Stop the API first (`Ctrl + C`), because a running copy locks the build output.
