# skill_Response.md — API Response Format

## Stack: C# .NET 8 Web API

---

## 1. ApiResponse Wrapper

Mọi endpoint đều trả về `ApiResponse<T>`. KHÔNG return raw object hay raw list.

```csharp
// DTOs/Common/ApiResponse.cs
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null)
        => new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> OkMessage(string message)
        => new() { Success = true, Message = message };

    public static ApiResponse<T> Fail(string message)
        => new() { Success = false, Message = message };

    public static ApiResponse<T> FailValidation(List<string> errors)
        => new() { Success = false, Message = "Validation failed.", Errors = errors };
}
```

---

## 2. Paginated Response

Dùng cho các API trả về danh sách có phân trang.

```csharp
// DTOs/Common/PagedResponse.cs
public class PagedResponse<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrev => Page > 1;
}
```

---

## 3. Cách dùng trong Controller

```csharp
// Controllers/TaskController.cs
[HttpGet("{id}")]
public async Task<ActionResult<ApiResponse<TaskDto>>> GetById(int id)
{
    var task = await _taskService.GetByIdAsync(id, GetUserId());
    return Ok(ApiResponse<TaskDto>.Ok(task));
}

[HttpGet]
public async Task<ActionResult<ApiResponse<PagedResponse<TaskDto>>>> GetAll(
    [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
{
    var result = await _taskService.GetAllAsync(page, pageSize, GetUserId());
    return Ok(ApiResponse<PagedResponse<TaskDto>>.Ok(result));
}

[HttpPost]
public async Task<ActionResult<ApiResponse<TaskDto>>> Create([FromBody] CreateTaskDto dto)
{
    var task = await _taskService.CreateAsync(dto, GetUserId());
    return CreatedAtAction(nameof(GetById), new { id = task.Id },
        ApiResponse<TaskDto>.Ok(task, "Task created successfully."));
}

[HttpDelete("{id}")]
public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
{
    await _taskService.DeleteAsync(id, GetUserId());
    return Ok(ApiResponse<object>.OkMessage("Task deleted successfully."));
}

// Helper — lấy userId từ JWT claim
private int GetUserId() =>
    int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
```

---

## 4. HTTP Status Code Convention

| Scenario | Status Code | Response |
|---|---|---|
| Lấy/cập nhật thành công | `200 OK` | `ApiResponse<T>.Ok(data)` |
| Tạo mới thành công | `201 Created` | `ApiResponse<T>.Ok(data)` |
| Xóa thành công | `200 OK` | `ApiResponse.OkMessage(...)` |
| Validation lỗi | `400 Bad Request` | `ApiResponse.FailValidation(errors)` |
| Chưa đăng nhập | `401 Unauthorized` | `ApiResponse.Fail(...)` |
| Không có quyền | `403 Forbidden` | `ApiResponse.Fail(...)` |
| Không tìm thấy | `404 Not Found` | `ApiResponse.Fail(...)` |
| Lỗi server | `500 Internal Server Error` | `ApiResponse.Fail(...)` |

---

## 5. Frontend — Parse Response (React)

```js
// api/taskApi.js
import api from './axiosInstance';

export const getTaskById = async (id) => {
    const { data } = await api.get(`/tasks/${id}`);
    // data.success, data.data, data.message
    return data.data;
};

export const createTask = async (payload) => {
    const { data } = await api.post('/tasks', payload);
    return data; // { success, data, message }
};
```
