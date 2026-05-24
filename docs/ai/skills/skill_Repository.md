# skill_Repository.md — Repository & Unit of Work Pattern

## Stack: C# .NET 8 + Entity Framework Core + SQL Server

---

## 1. Nguyên tắc

- Dùng **Generic Repository** cho các CRUD cơ bản
- Dùng **Specific Repository** cho các query phức tạp liên quan đến domain
- Dùng **Unit of Work** khi cần transaction nhiều bảng
- Service chỉ gọi Repository — KHÔNG gọi `DbContext` trực tiếp trong Service

---

## 2. Generic Repository

```csharp
// Repositories/Interfaces/IGenericRepository.cs
public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
}

// Repositories/GenericRepository.cs
public class GenericRepository<T>(AppDbContext context) : IGenericRepository<T>
    where T : class
{
    protected readonly AppDbContext _context = context;
    protected readonly DbSet<T> _dbSet = context.Set<T>();

    public async Task<T?> GetByIdAsync(int id) =>
        await _dbSet.FindAsync(id);

    public async Task<IEnumerable<T>> GetAllAsync() =>
        await _dbSet.ToListAsync();

    public async Task AddAsync(T entity) =>
        await _dbSet.AddAsync(entity);

    public void Update(T entity) =>
        _dbSet.Update(entity);

    public void Delete(T entity) =>
        _dbSet.Remove(entity);
}
```

---

## 3. Specific Repository (Task Management)

```csharp
// Repositories/Interfaces/ITaskRepository.cs
public interface ITaskRepository : IGenericRepository<TaskItem>
{
    Task<IEnumerable<TaskItem>> GetByProjectAsync(int projectId);
    Task<IEnumerable<TaskItem>> GetAssignedToUserAsync(int userId);
    Task<PagedResponse<TaskItem>> GetPagedAsync(int projectId, int page, int pageSize);
    Task<bool> IsUserAssignedToTaskAsync(int taskId, int userId);
}

// Repositories/TaskRepository.cs
public class TaskRepository(AppDbContext context)
    : GenericRepository<TaskItem>(context), ITaskRepository
{
    public async Task<IEnumerable<TaskItem>> GetByProjectAsync(int projectId) =>
        await _dbSet
            .Where(t => t.ProjectId == projectId && !t.IsDeleted)
            .Include(t => t.AssignedUser)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<TaskItem>> GetAssignedToUserAsync(int userId) =>
        await _dbSet
            .Where(t => t.AssignedUserId == userId && !t.IsDeleted)
            .Include(t => t.Project)
            .ToListAsync();

    public async Task<PagedResponse<TaskItem>> GetPagedAsync(
        int projectId, int page, int pageSize)
    {
        var query = _dbSet.Where(t => t.ProjectId == projectId && !t.IsDeleted);
        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<TaskItem>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<bool> IsUserAssignedToTaskAsync(int taskId, int userId) =>
        await _dbSet.AnyAsync(t => t.Id == taskId && t.AssignedUserId == userId);
}
```

---

## 4. Unit of Work

Dùng khi một thao tác cần ghi vào **nhiều bảng** trong cùng 1 transaction.

```csharp
// Repositories/Interfaces/IUnitOfWork.cs
public interface IUnitOfWork : IDisposable
{
    ITaskRepository Tasks { get; }
    IProjectRepository Projects { get; }
    ITeamMemberRepository TeamMembers { get; }
    Task<int> SaveChangesAsync();
}

// Repositories/UnitOfWork.cs
public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public ITaskRepository Tasks { get; } = new TaskRepository(context);
    public IProjectRepository Projects { get; } = new ProjectRepository(context);
    public ITeamMemberRepository TeamMembers { get; } = new TeamMemberRepository(context);

    public async Task<int> SaveChangesAsync() =>
        await context.SaveChangesAsync();

    public void Dispose() => context.Dispose();
}
```

Đăng ký DI:
```csharp
// Program.cs
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
```

---

## 5. Cách dùng trong Service

```csharp
// Services/TaskService.cs
public class TaskService(IUnitOfWork uow, IMapper mapper) : ITaskService
{
    // Query đơn giản
    public async Task<TaskDto> GetByIdAsync(int taskId, int userId)
    {
        var task = await uow.Tasks.GetByIdAsync(taskId)
            ?? throw new NotFoundException("Task", taskId);

        return mapper.Map<TaskDto>(task);
    }

    // Cần transaction nhiều bảng
    public async Task<TaskDto> CreateWithActivityLogAsync(
        CreateTaskDto dto, int creatorId)
    {
        var task = mapper.Map<TaskItem>(dto);
        task.CreatedById = creatorId;

        await uow.Tasks.AddAsync(task);

        // Ghi activity log cùng transaction
        await uow.ActivityLogs.AddAsync(new ActivityLog
        {
            Action = "TASK_CREATED",
            UserId = creatorId,
            CreatedAt = DateTime.UtcNow
        });

        await uow.SaveChangesAsync(); // 1 lần duy nhất
        return mapper.Map<TaskDto>(task);
    }
}
```

---

## 6. Soft Delete Convention

Mọi entity có thể xóa đều implement soft delete — KHÔNG xóa khỏi DB.

```csharp
// Models/Base/BaseEntity.cs
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}
```

Query mặc định luôn filter `IsDeleted = false`:
```csharp
// AppDbContext.cs — Global Query Filter
modelBuilder.Entity<TaskItem>().HasQueryFilter(t => !t.IsDeleted);
```
