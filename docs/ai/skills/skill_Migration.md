# skill_Migration.md — EF Core Migration Pattern

## Stack: C# .NET 8 + Entity Framework Core + SQL Server

---

## 1. Nguyên tắc

- ✅ Luôn dùng **Code First** migration
- ✅ Review migration file trước khi apply
- ✅ Đặt tên migration mô tả rõ ràng
- ❌ KHÔNG sửa migration đã apply vào DB
- ❌ KHÔNG xóa migration đã apply
- ❌ KHÔNG modify DB schema bằng tay

---

## 2. Lệnh cơ bản

```bash
# Tạo migration mới
dotnet ef migrations add <TenMigration>

# Apply migration vào DB
dotnet ef database update

# Rollback về migration trước
dotnet ef database update <TenMigrationTruoc>

# Xóa migration chưa apply
dotnet ef migrations remove

# Xem danh sách migrations
dotnet ef migrations list

# Generate SQL script (cho production)
dotnet ef migrations script
```

---

## 3. Naming Convention

| Hành động | Tên migration | Ví dụ |
|-----------|--------------|-------|
| Tạo schema ban đầu | `InitialCreate` / `AuthInit` | `20260424_AuthInit` |
| Thêm table mới | `Add{TableName}` | `AddTeamAndComments` |
| Thêm column | `Add{Column}To{Table}` | `AddAvatarToUsers` |
| Sửa column | `Update{Table}Schema` | `UpdateUserSchema` |
| Thêm index | `Add{Index}` | `AddEmailIndex` |
| Xóa table | `Remove{Table}` | `RemoveOldLogs` |

---

## 4. Migration History (TaskHub)

| Migration | Ngày | Mô tả |
|-----------|------|--------|
| `20260424191146_AuthInit` | 2026-04-24 | Initial schema: Users, Boards, Lists, Tasks, RefreshTokens, AuditLogs |
| `20260427174003_UpdateUserSchema` | 2026-04-27 | Update user schema |

---

## 5. Workflow khi thay đổi Model

```txt
1. Sửa Model class (thêm/sửa property)
2. Sửa DbContext nếu cần (relationships, indexes)
3. Tạo migration: dotnet ef migrations add <Tên>
4. Review file migration (Up + Down methods)
5. Apply: dotnet ef database update
6. Test lại app
7. Commit migration files
```

---

## 6. Ví dụ: Thêm Team system

### Step 1: Tạo Models

```csharp
public class Team
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;
    public Guid CreatedById { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public AppUser CreatedBy { get; set; } = null!;
    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
}
```

### Step 2: Thêm DbSet

```csharp
public DbSet<Team> Teams { get; set; }
public DbSet<TeamMember> TeamMembers { get; set; }
```

### Step 3: Configure relationships

```csharp
modelBuilder.Entity<Team>()
    .HasMany(t => t.Members)
    .WithOne(m => m.Team)
    .HasForeignKey(m => m.TeamId)
    .OnDelete(DeleteBehavior.Cascade);
```

### Step 4: Create & apply migration

```bash
dotnet ef migrations add AddTeamSystem
dotnet ef database update
```

---

## 7. Troubleshooting

### Lỗi "Build failed"
```bash
# Fix build errors trước khi tạo migration
dotnet build
```

### Lỗi "Pending migrations"
```bash
# Apply all pending migrations
dotnet ef database update
```

### Cần rollback
```bash
# Rollback về migration cụ thể
dotnet ef database update 20260424191146_AuthInit

# Xóa migration vừa tạo (chưa apply)
dotnet ef migrations remove
```

### Reset toàn bộ DB (dev only!)
```bash
dotnet ef database drop --force
dotnet ef database update
```

---

## 8. Production Deployment

```bash
# Generate SQL script cho DBA review
dotnet ef migrations script --idempotent -o migrate.sql

# Hoặc auto-migrate trong Program.cs (KHÔNG recommended cho production)
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
db.Database.Migrate();  // ⚠️ Only for dev
```

---
