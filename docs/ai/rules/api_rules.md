# 🌐 API Rules — TaskHub

## Stack: C# .NET 8 Web API

---

## 1. Response Format

Mọi endpoint đều trả về `ApiResponse<T>`. KHÔNG return raw object.

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null) => new()
    {
        Success = true, Data = data, Message = message
    };

    public static ApiResponse<T> Fail(string message, List<string>? errors = null) => new()
    {
        Success = false, Message = message, Errors = errors
    };
}
```

---

## 2. HTTP Status Code Convention

| Scenario             | Status Code             | Response                        |
| -------------------- | ----------------------- | ------------------------------- |
| Lấy/cập nhật thành công | `200 OK`             | `ApiResponse<T>.Ok(data)`       |
| Tạo mới thành công   | `201 Created`           | `ApiResponse<T>.Ok(data)`       |
| Xóa thành công       | `200 OK`                | `ApiResponse.Ok("Deleted")`     |
| Validation lỗi       | `400 Bad Request`       | `ApiResponse.Fail(...)`         |
| Chưa đăng nhập       | `401 Unauthorized`      | `ApiResponse.Fail(...)`         |
| Không có quyền       | `403 Forbidden`         | `ApiResponse.Fail(...)`         |
| Không tìm thấy       | `404 Not Found`         | `ApiResponse.Fail(...)`         |
| Rate limit           | `429 Too Many Requests` | Auto by middleware               |
| Lỗi server           | `500 Internal Error`    | `ApiResponse.Fail(...)`         |

---

## 3. URL Convention

- URL dùng **kebab-case**, **plural noun**
- KHÔNG dùng verb trong URL

| ✅ Đúng                    | ❌ Sai                        |
| ------------------------- | ----------------------------- |
| `GET /api/tasks`          | `GET /api/getTasks`           |
| `POST /api/boards`        | `POST /api/createBoard`       |
| `DELETE /api/tasks/{id}`  | `POST /api/deleteTask/{id}`   |
| `GET /api/boards/{id}/lists` | `GET /api/getBoardLists`   |

---

## 4. Controller Pattern

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskItemService _taskService;

    public TasksController(ITaskItemService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<TaskDto>>> GetById(Guid id)
    {
        var userId = GetUserId();
        var task = await _taskService.GetByIdAsync(id, userId);
        return Ok(ApiResponse<TaskDto>.Ok(task));
    }

    // Helper — lấy userId từ JWT claim
    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
```

---

## 5. Endpoint Design

### Auth (Public)

| Method | Endpoint              | Mô tả           |
| ------ | --------------------- | ---------------- |
| POST   | `/api/auth/register`  | Đăng ký          |
| POST   | `/api/auth/login`     | Đăng nhập        |
| POST   | `/api/auth/refresh`   | Refresh token    |
| POST   | `/api/auth/logout`    | Đăng xuất        |

### Boards (Authenticated)

| Method | Endpoint                        | Mô tả                |
| ------ | ------------------------------- | --------------------- |
| POST   | `/api/boards`                   | Tạo board             |
| GET    | `/api/boards`                   | Lấy boards của user   |
| GET    | `/api/boards/{id}`              | Chi tiết board        |
| PUT    | `/api/boards/{id}`              | Cập nhật board        |
| DELETE | `/api/boards/{id}`              | Xóa board             |

### Board Lists (Authenticated)

| Method | Endpoint                             | Mô tả            |
| ------ | ------------------------------------ | ----------------- |
| POST   | `/api/boards/{boardId}/lists`        | Tạo list          |
| GET    | `/api/boards/{boardId}/lists`        | Lấy lists         |
| PUT    | `/api/lists/{id}`                    | Cập nhật list     |
| DELETE | `/api/lists/{id}`                    | Xóa list          |

### Tasks (Authenticated)

| Method | Endpoint                        | Mô tả                |
| ------ | ------------------------------- | --------------------- |
| POST   | `/api/lists/{listId}/tasks`     | Tạo task trong list   |
| GET    | `/api/lists/{listId}/tasks`     | Lấy tasks trong list  |
| GET    | `/api/tasks/{id}`               | Chi tiết task         |
| PUT    | `/api/tasks/{id}`               | Cập nhật task         |
| DELETE | `/api/tasks/{id}`               | Xóa task              |
| PUT    | `/api/tasks/{id}/move`          | Di chuyển task        |
| PATCH  | `/api/tasks/{id}/progress`      | Cập nhật progress     |

### Dashboard (Authenticated)

| Method | Endpoint                       | Mô tả               |
| ------ | ------------------------------ | -------------------- |
| GET    | `/api/dashboard/stats`         | Task statistics       |
| GET    | `/api/dashboard/recent-tasks`  | Gần đây              |
| GET    | `/api/v1/dashboard/velocity`  | Scoped task velocity for Week, Month, SixMonths, or Year |
| GET    | `/api/v1/analytics/overview`  | Scoped task analytics and weekly velocity |

`stats`, `velocity`, and `analytics/overview` accept `scope=Personal` (default) or
`scope=Team&teamId={id}`. Team scope requires membership. Analytics also accepts
`days` (1–365) and optional `boardId`. Velocity accepts `timeframe=Week|Month|SixMonths|Year`
(default `SixMonths`). All boundaries are UTC and half-open `[start, end)`:
Week is Monday–Sunday with seven daily buckets; Month is the current calendar
month with one bucket per day; SixMonths is the current month and five previous
months with six monthly buckets; Year is January–December of the current year
with twelve monthly buckets. Future buckets and periods with no data are zero.
Velocity `completed` counts distinct tasks that transition into `Done` in each
bucket, using the existing `TaskActivityLog` status-change records. A task can
count once in each bucket where it is completed again. `created` is a comparison
series, not the definition of velocity. Older status changes without a log
cannot be dated.
The unversioned dashboard and analytics routes remain available to existing clients.

### Settings (Authenticated)

| Method | Endpoint                    | Mô tả             |
| ------ | --------------------------- | ------------------ |
| GET    | `/api/settings/profile`     | Lấy profile        |
| PUT    | `/api/settings/profile`     | Cập nhật profile   |
| PUT    | `/api/settings/password`    | Đổi mật khẩu      |
| GET    | `/api/settings/activity`    | Activity log       |

---

## 6. Rate Limiting

```csharp
// Auth endpoints — 5 requests/minute
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("AuthRateLimit", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
    options.RejectionStatusCode = 429;
});
```

---

## 7. Pagination (Planned)

```csharp
// Query params
GET /api/tasks?page=1&pageSize=10&status=Todo&sortBy=createdAt

// Response
{
    "success": true,
    "data": {
        "items": [...],
        "totalCount": 50,
        "page": 1,
        "pageSize": 10,
        "totalPages": 5,
        "hasNext": true,
        "hasPrev": false
    }
}
```

---
