# skill_Error.md — Error Handling Pattern

## Stack: C# .NET 8 Web API + React

---

## 1. Custom Exception Classes

Tạo các custom exception riêng biệt, KHÔNG throw `Exception` trực tiếp trong Service.

```csharp
// Exceptions/AppException.cs
public class AppException : Exception
{
    public int StatusCode { get; }
    public AppException(string message, int statusCode = 400)
        : base(message) => StatusCode = statusCode;
}

public class NotFoundException : AppException
{
    public NotFoundException(string resource, object id)
        : base($"{resource} with id '{id}' not found.", 404) { }
}

public class ForbiddenException : AppException
{
    public ForbiddenException(string message = "You do not have permission to perform this action.")
        : base(message, 403) { }
}

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Unauthorized.")
        : base(message, 401) { }
}

public class ConflictException : AppException
{
    public ConflictException(string message)
        : base(message, 409) { }
}
```

---

## 2. Global Exception Middleware

Đặt ở `Middleware/ExceptionMiddleware.cs`. Đây là **nơi DUY NHẤT** xử lý exception toàn app.

```csharp
// Middleware/ExceptionMiddleware.cs
public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException ex)
        {
            logger.LogWarning(ex, "Application exception: {Message}", ex.Message);
            context.Response.StatusCode = ex.StatusCode;
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(
                ApiResponse<object>.Fail("An unexpected error occurred."));
        }
    }
}
```

Đăng ký trong `Program.cs`:
```csharp
app.UseMiddleware<ExceptionMiddleware>();
```

---

## 3. Cách dùng trong Service

```csharp
// Services/TaskService.cs
public async Task<TaskDto> GetByIdAsync(int taskId, int requestingUserId)
{
    var task = await _repo.GetByIdAsync(taskId)
        ?? throw new NotFoundException("Task", taskId);

    if (task.AssignedUserId != requestingUserId)
        throw new ForbiddenException();

    return _mapper.Map<TaskDto>(task);
}
```

**Rules:**
- ✅ Service throw custom exception
- ✅ Controller KHÔNG try/catch — để middleware xử lý
- ✅ KHÔNG throw `Exception` trực tiếp
- ❌ KHÔNG return null khi resource không tồn tại → throw `NotFoundException`

---

## 4. Frontend — Error Handling (React + axios)

```js
// api/axiosInstance.js
import axios from 'axios';

const api = axios.create({ baseURL: '/api' });

api.interceptors.request.use(config => {
    const token = localStorage.getItem('token');
    if (token) config.headers.Authorization = `Bearer ${token}`;
    return config;
});

api.interceptors.response.use(
    res => res,
    error => {
        const status = error.response?.status;
        const message = error.response?.data?.message || 'Something went wrong';

        if (status === 401) {
            // redirect to login
            window.location.href = '/login';
        }
        if (status === 403) {
            // show forbidden UI
        }
        return Promise.reject({ status, message });
    }
);

export default api;
```
