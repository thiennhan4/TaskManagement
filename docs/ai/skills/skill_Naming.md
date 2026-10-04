# skill_Naming.md — Naming Convention

## Stack: C# .NET 8 Backend + React Frontend

---

## 1. Backend — C# Conventions

### Classes & Interfaces
| Loại | Convention | Ví dụ |
|---|---|---|
| Interface | `I` prefix + PascalCase | `ITaskService`, `IUserRepository` |
| Service | PascalCase + `Service` | `TaskService`, `AuthService` |
| Repository | PascalCase + `Repository` | `TaskRepository`, `UserRepository` |
| Controller | PascalCase + `Controller` | `TaskController`, `AuthController` |
| Middleware | PascalCase + `Middleware` | `ExceptionMiddleware`, `LoggingMiddleware` |
| Validator | PascalCase + `Validator` | `CreateTaskValidator`, `RegisterValidator` |
| DTO | PascalCase + `Dto` | `TaskDto`, `CreateTaskDto`, `UpdateTaskDto` |
| Enum | PascalCase, values PascalCase | `TaskStatus`, `UserRole`, `TaskPriority` |
| Entity/Model | PascalCase, singular | `TaskItem`, `User`, `Project`, `TeamMember` |
| Exception | PascalCase + `Exception` | `NotFoundException`, `ForbiddenException` |

### Methods
| Convention | Ví dụ |
|---|---|
| PascalCase | `GetByIdAsync`, `CreateTaskAsync` |
| Async methods thêm `Async` suffix | `GetAllAsync()`, `SaveChangesAsync()` |
| Boolean trả về: `Is`, `Has`, `Can` prefix | `IsDeleted`, `HasPermission`, `CanEdit` |

### Fields & Properties
```csharp
// Private field: _camelCase
private readonly ITaskService _taskService;

// Public property: PascalCase
public string Title { get; set; }
public bool IsCompleted { get; set; }
```

### Constants & Enums
```csharp
// Enum — PascalCase
public enum TaskStatus
{
    Todo,
    InProgress,
    Done,
    Cancelled
}

public enum UserRole
{
    Admin,
    Manager,
    Member
}

public enum TaskPriority
{
    Low,
    Medium,
    High,
    Critical
}

// Constant — PascalCase trong static class
public static class AppConstants
{
    public const string AdminRole = "Admin";
    public const int MaxPageSize = 100;
    public const int DefaultPageSize = 10;
}
```

---

## 2. File & Folder Naming — Backend

```
TaskManagement.Api/
├── Controllers/
│   ├── AuthController.cs
│   ├── TaskController.cs
│   └── ProjectController.cs
├── Services/
│   ├── Interfaces/
│   │   ├── IAuthService.cs
│   │   └── ITaskService.cs
│   ├── AuthService.cs
│   └── TaskService.cs
├── Repositories/
│   ├── Interfaces/
│   │   ├── IGenericRepository.cs
│   │   └── ITaskRepository.cs
│   ├── GenericRepository.cs
│   └── TaskRepository.cs
├── DTOs/
│   ├── Auth/
│   │   ├── LoginDto.cs
│   │   └── RegisterDto.cs
│   ├── Task/
│   │   ├── TaskDto.cs
│   │   ├── CreateTaskDto.cs
│   │   └── UpdateTaskDto.cs
│   └── Common/
│       ├── ApiResponse.cs
│       └── PagedResponse.cs
├── Models/
│   ├── Base/
│   │   └── BaseEntity.cs
│   ├── User.cs
│   ├── TaskItem.cs       ← Tránh dùng "Task" vì trùng với C# Task<T>
│   └── Project.cs
├── Enums/
│   ├── UserRole.cs
│   ├── TaskStatus.cs
│   └── TaskPriority.cs
├── Exceptions/
│   ├── AppException.cs
│   ├── NotFoundException.cs
│   └── ForbiddenException.cs
├── Middleware/
│   └── ExceptionMiddleware.cs
└── Validators/
    ├── CreateTaskValidator.cs
    └── RegisterValidator.cs
```

---

## 3. Database — SQL Server Conventions

| Loại | Convention | Ví dụ |
|---|---|---|
| Table | PascalCase, **plural** | `Users`, `Tasks`, `Projects`, `TeamMembers` |
| Column | PascalCase | `Id`, `Title`, `CreatedAt`, `AssignedUserId` |
| Primary Key | `Id` | `Id INT IDENTITY(1,1)` |
| Foreign Key | `{Entity}Id` | `ProjectId`, `AssignedUserId`, `CreatedById` |
| Index | `IX_{Table}_{Column}` | `IX_Tasks_ProjectId`, `IX_Users_Email` |
| Stored Procedure | `sp_{Action}_{Entity}` | `sp_Get_TasksByProject` |

---

## 4. Frontend — React Conventions

### File & Folder
```
src/
├── api/
│   ├── axiosInstance.js       # lowercase
│   ├── taskApi.js
│   └── authApi.js
├── components/
│   ├── TaskCard/
│   │   ├── TaskCard.jsx       # PascalCase component
│   │   ├── TaskCard.css
│   │   └── index.js
│   └── common/
│       └── Button.jsx
├── pages/
│   ├── TaskListPage.jsx       # PascalCase + Page suffix
│   ├── TaskDetailPage.jsx
│   └── LoginPage.jsx
├── hooks/
│   ├── useAuth.js             # camelCase + use prefix
│   └── useTasks.js
├── context/
│   └── AuthContext.jsx        # PascalCase + Context suffix
└── utils/
    ├── dateUtils.js           # camelCase + Utils suffix
    └── formatUtils.js
```

### Variables & Functions
```js
// camelCase for variables and functions
const taskList = [];
const getCurrentUser = () => { ... };

// PascalCase for components
function TaskCard({ task }) { ... }

// UPPER_SNAKE_CASE for constants
const API_BASE_URL = '/api';
const MAX_RETRY = 3;
```

### Props & Events
```jsx
// Props: camelCase
<TaskCard taskId={1} isCompleted={false} onDelete={handleDelete} />

// Event handlers: handle + PascalCase action
const handleSubmit = () => { ... };
const handleTaskDelete = (id) => { ... };
```

---

## 5. API Endpoint Naming (REST)

| Action | Method | URL |
|---|---|---|
| Lấy danh sách | GET | `/api/tasks` |
| Lấy 1 item | GET | `/api/tasks/{id}` |
| Tạo mới | POST | `/api/tasks` |
| Cập nhật toàn bộ | PUT | `/api/tasks/{id}` |
| Cập nhật một phần | PATCH | `/api/tasks/{id}` |
| Xóa | DELETE | `/api/tasks/{id}` |
| Sub-resource | GET | `/api/projects/{id}/tasks` |
| Action đặc biệt | POST | `/api/tasks/{id}/assign` |

- URL dùng **kebab-case**, **plural noun**
- KHÔNG dùng verb trong URL: ~~`/api/getTasks`~~, ~~`/api/deleteTask/1`~~
