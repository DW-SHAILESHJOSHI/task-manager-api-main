# Task Manager API

A RESTful Task Management API built with **ASP.NET Core (.NET 10)**, demonstrating core Object-Oriented Programming principles (Abstraction, Inheritance, Polymorphism, Encapsulation) integrated into a real CRUD API — not just theoretical examples.

---

## 📋 Overview

This project is a simple in-memory Task Manager API that allows creating, reading, updating, and deleting tasks. It also demonstrates a practical use of **polymorphism**: when a task is marked as completed, the API dynamically triggers a notification (Email or SMS) using an abstract base class design.

---

## 🚀 Features

- Full CRUD operations for tasks (Create, Read, Update, Delete)
- RESTful endpoint design following standard HTTP conventions
- Proper HTTP status codes (`200`, `201`, `204`, `404`)
- OOP-based notification system triggered on task completion
- In-memory data store (no database dependency — lightweight and easy to run)

---

## 🛠️ Tech Stack

| Technology | Purpose |
|---|---|
| **C# / .NET 10** | Core language and runtime |
| **ASP.NET Core Web API** | REST API framework |
| **Kestrel** | Built-in web server |

---

## 📂 Project Structure

```
TaskManagerApi/
├── Controllers/
│   └── TasksController.cs      # API endpoints (CRUD)
├── Models/
│   ├── TaskItem.cs              # Task entity
│   ├── NotificationBase.cs      # Abstract base class for notifications
│   ├── EmailReminder.cs         # Email notification implementation
│   └── SmsReminder.cs           # SMS notification implementation
├── Program.cs                    # App entry point & configuration
└── appsettings.json               # App configuration
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

---

## 📡 API Endpoints

| Method | Endpoint | Description | Success Response |
|---|---|---|---|
| `GET` | `/api/tasks` | Get all tasks | `200 OK` |
| `GET` | `/api/tasks/{id}` | Get a single task by ID | `200 OK` / `404 Not Found` |
| `POST` | `/api/tasks` | Create a new task | `201 Created` |
| `PUT` | `/api/tasks/{id}` | Update an existing task | `200 OK` / `404 Not Found` |
| `DELETE` | `/api/tasks/{id}` | Delete a task | `204 No Content` / `404 Not Found` |

### Request/Response Examples

**Create a task**
```bash
curl -X POST http://localhost:5179/api/tasks \
  -H "Content-Type: application/json" \
  -d '{"title":"Learn EF Core","priority":"High"}'
```

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

### Run Locally

```bash
git clone https://github.com/<your-username>/task-manager-api.git
cd task-manager-api/TaskManagerApi
dotnet run
```

The API will start on `http://localhost:5179` (port may vary — check terminal output).

### Test the Endpoints

Using `curl`:
```bash
curl http://localhost:5179/api/tasks
```

Or use tools like [Postman](https://www.postman.com/) or the VS Code REST Client extension for a more visual testing experience.
