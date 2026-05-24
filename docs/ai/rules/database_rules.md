# 🗄️ Database Rules — TaskHub

## Stack: Entity Framework Core + SQL Server

---

## 1. General Rules

- ✅ Use EF Core Code First
- ✅ Use Guid as primary key
- ✅ Use Migrations for schema changes
- ❌ NEVER string-concatenate SQL
- ❌ NEVER access DbContext directly in Controller

---

## 2. Entity Design

```csharp
public class EntityName
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
```

### Soft Delete (Planned)

- Set `IsDeleted = true` — NEVER hard delete
- Apply Global Query Filter

---

## 3. Naming Convention

| Loại | Convention | Ví dụ |
|------|-----------|-------|
| Table | PascalCase, plural | `Users`, `Tasks` |
| Column | PascalCase | `Id`, `CreatedAt` |
| PK | `Id` | `Guid Id` |
| FK | `{Entity}Id` | `OwnerId`, `BoardId` |
| Index | `IX_{Table}_{Column}` | `IX_Users_Email` |

---

## 4. Delete Behavior

| Parent | Child | OnDelete |
|--------|-------|----------|
| AppUser | Board | Cascade |
| AppUser | RefreshToken | Cascade |
| AppUser | TaskItem | NoAction |
| AppUser | AuditLog | NoAction |
| Board | BoardList | Cascade |
| BoardList | TaskItem | Cascade |

---

## 5. Indexes

| Table | Column | Type | Mục đích |
|-------|--------|------|----------|
| AppUser | Email | UNIQUE | Login lookup |
| RefreshToken | Token | UNIQUE | Token validation |

---

## 6. Migration Rules

```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

- ✅ Tên migration mô tả rõ
- ❌ KHÔNG sửa migration đã apply

---

## 7. Query Optimization

- ✅ AsNoTracking cho read-only
- ✅ Select projection thay vì Include all
- ❌ KHÔNG ToList() trước Where()

---
