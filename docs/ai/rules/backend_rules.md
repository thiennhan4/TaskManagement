# 🏗 Backend Rules — TaskHub

## Stack: C# .NET 8 Web API + Entity Framework Core + SQL Server

---

## 1. Architecture Rules

- ❌ Do NOT put business logic in Controller
- ✅ Always use Service layer for all business logic
- ✅ Repository handles data access only — no business logic
- ✅ Use DTOs for all input/output — NEVER expose Entity directly
- ✅ Use async/await throughout all layers
- ✅ Use dependency injection — NEVER `new` up services manually
- ✅ Register all services as `Scoped` in `Program.cs`

---

## 2. Layered Architecture

```txt
Controller → Service → Repository → DbContext → Database
```

| Layer       | Responsibility                              | Rules                                      |
| ----------- | ------------------------------------------- | ------------------------------------------ |
| Controller  | Request/Response handling                    | NO business logic, NO try/catch             |
| Service     | Business logic, permission check             | Throw typed exceptions, return DTOs         |
| Repository  | Data access (CRUD)                           | NO business logic, return Entity            |
| Middleware  | Cross-cutting concerns (auth, error, logging)| Global pipeline                             |

---

## 3. Dependency Injection

```csharp
// ✅ Correct — Register in Program.cs
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBoardService, BoardService>();
builder.Services.AddScoped<ITaskItemService, TaskItemService>();

// ❌ Wrong — Never new up services
var service = new TaskItemService(context); // NEVER DO THIS
```

---

## 4. Code Quality Rules

- ❌ NEVER `async void` — always `async Task`
- ✅ Use `CancellationToken` on all async controller actions
- ✅ Use `ILogger<T>` (injected) in Service layer — NOT `Console.WriteLine`
- ✅ Log Warning for expected errors (404, 403), Log Error for unexpected (500)
- ✅ Use `ReferenceHandler.IgnoreCycles` for JSON serialization
- ✅ Use `JsonIgnoreCondition.WhenWritingNull` to skip null fields

---

## 5. Service Rules

```csharp
// ✅ Service responsibilities:
// - Business logic
// - Permission checking
// - DB operations via Repository
// - Map entities to DTOs

// ✅ Service MUST:
// - Use async methods
// - Throw typed exceptions (NotFoundException, ForbiddenException)
// - Return DTOs, not Entities

// ❌ Service must NOT:
// - Return IActionResult
// - Access HttpContext directly
// - Contain controller logic
// - Use Console.WriteLine
```

---

## 6. Controller Rules

```csharp
// ✅ Controller MUST:
// - Only call Service methods
// - Extract userId from JWT claims
// - Return ApiResponse<T> wrapper
// - Use [Authorize] attribute

// ❌ Controller must NOT:
// - Have try/catch (let GlobalExceptionMiddleware handle)
// - Contain business logic
// - Access DbContext directly
// - Call Repository directly
```

---

## 7. Project Structure

```txt
TaskHub/
├── backend/
│   ├── Controllers/          # API endpoints
│   ├── Services/             # Business logic
│   │   └── Interfaces/       # Service contracts
│   ├── Repositories/         # Data access
│   │   └── Interfaces/       # Repository contracts
│   ├── Models/               # Entity classes
│   ├── DTOs/                 # Data Transfer Objects
│   ├── Data/                 # DbContext
│   ├── Middleware/            # Exception handling
│   └── View/                 # View models (if needed)
├── Migrations/               # EF Core migrations
├── docs/                     # Documentation
└── Program.cs                # App configuration & DI
```

---

## 8. JSON Serialization

```csharp
// Program.cs — đã cấu hình
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            JsonIgnoreCondition.WhenWritingNull;
    });
```

---

## 9. Middleware Pipeline Order

```csharp
// Program.cs — THỨ TỰ QUAN TRỌNG
app.UseMiddleware<GlobalExceptionMiddleware>();  // 1. Exception handling (first!)
app.UseCors("TaskHubCors");                      // 2. CORS
app.UseRateLimiter();                            // 3. Rate limiting
app.UseAuthentication();                          // 4. JWT validation
app.UseAuthorization();                           // 5. Authorization
app.MapControllers();                             // 6. Routing
```

---
