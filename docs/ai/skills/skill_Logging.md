# skill_Logging.md — Logging Pattern

## Stack: C# .NET 8 + ILogger + Serilog (planned)

---

## 1. Nguyên tắc

- ✅ Dùng `ILogger<T>` (injected) — KHÔNG dùng `Console.WriteLine`
- ✅ Log ở **Service layer** — KHÔNG log trong Controller
- ✅ Log Warning cho expected errors (404, 403)
- ✅ Log Error cho unexpected errors (500)
- ✅ Log Information cho business events quan trọng
- ❌ KHÔNG log sensitive data (password, token, connection string)

---

## 2. Cách inject ILogger

```csharp
public class TaskItemService : ITaskItemService
{
    private readonly ILogger<TaskItemService> _logger;
    private readonly ITaskItemRepository _repo;

    public TaskItemService(
        ILogger<TaskItemService> logger,
        ITaskItemRepository repo)
    {
        _logger = logger;
        _repo = repo;
    }
}
```

---

## 3. Log Levels

| Level       | Khi nào dùng                              | Ví dụ                                    |
| ----------- | ----------------------------------------- | ---------------------------------------- |
| Debug       | Dev only, chi tiết internal               | `_logger.LogDebug("Query: {Sql}", sql)`  |
| Information | Business event thành công                 | `_logger.LogInformation("User {Id} created task", userId)` |
| Warning     | Expected error, không crash               | `_logger.LogWarning("Task {Id} not found", id)` |
| Error       | Unexpected error, cần investigate         | `_logger.LogError(ex, "Failed to create task")` |
| Critical    | App crash, service down                   | `_logger.LogCritical(ex, "Database connection failed")` |

---

## 4. Structured Logging

```csharp
// ✅ Dùng structured logging (message template)
_logger.LogInformation("User {UserId} created task {TaskId}", userId, taskId);

// ❌ KHÔNG dùng string interpolation
_logger.LogInformation($"User {userId} created task {taskId}"); // WRONG
```

---

## 5. Logging trong Service

```csharp
public async Task<TaskDto> CreateAsync(CreateTaskDto dto, Guid userId)
{
    _logger.LogInformation("Creating task for user {UserId}", userId);

    var task = new TaskItem
    {
        Title = dto.Title,
        OwnerId = userId
    };

    await _repo.AddAsync(task);
    await _repo.SaveChangesAsync();

    _logger.LogInformation("Task {TaskId} created successfully", task.Id);
    return MapToDto(task);
}

public async Task DeleteAsync(Guid taskId, Guid userId)
{
    var task = await _repo.GetByIdAsync(taskId);
    if (task == null)
    {
        _logger.LogWarning("Task {TaskId} not found for delete", taskId);
        throw new NotFoundException("Task", taskId);
    }

    if (task.OwnerId != userId)
    {
        _logger.LogWarning("User {UserId} forbidden to delete task {TaskId}", userId, taskId);
        throw new ForbiddenException();
    }

    _repo.Delete(task);
    await _repo.SaveChangesAsync();
    _logger.LogInformation("Task {TaskId} deleted by user {UserId}", taskId, userId);
}
```

---

## 6. Logging trong Middleware

```csharp
// GlobalExceptionMiddleware.cs
public async Task InvokeAsync(HttpContext context)
{
    try
    {
        await _next(context);
    }
    catch (AppException ex)
    {
        _logger.LogWarning(ex, "Application exception: {Message}", ex.Message);
        // return error response...
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
            context.Request.Method, context.Request.Path);
        // return 500 response...
    }
}
```

---

## 7. Serilog Setup (Planned)

```csharp
// Program.cs
builder.Host.UseSerilog((context, config) =>
{
    config
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .WriteTo.File("logs/taskhub-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30);
});

// appsettings.json
{
    "Serilog": {
        "MinimumLevel": {
            "Default": "Information",
            "Override": {
                "Microsoft.AspNetCore": "Warning",
                "Microsoft.EntityFrameworkCore": "Warning"
            }
        }
    }
}
```

---

## 8. Audit Log (AuditLog table)

```csharp
// Ghi lại action quan trọng vào DB
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Action { get; set; } = null!;      // "LOGIN", "CREATE_TASK"
    public string EntityType { get; set; } = null!;   // "Task", "Board"
    public Guid? EntityId { get; set; }
    public string? Detail { get; set; }               // JSON
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

---

## 9. Checklist

- [x] ILogger injection trong services
- [x] GlobalExceptionMiddleware logging
- [x] AuditLog table trong DB
- [ ] Serilog integration
- [ ] File logging (rolling daily)
- [ ] Request/response logging middleware
- [ ] Log correlation ID

---
