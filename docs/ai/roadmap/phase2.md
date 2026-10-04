# 📂 Phase 2 — Team & Collaboration System

---

# 🎯 Mục tiêu

Mở rộng TaskHub từ quản lý task cá nhân sang **hệ thống team collaboration** với:

* Team CRUD
* Team member management (invite, remove, change role)
* Team-based task permission
* Comment system

---

# 1. 🏗 Team System

## 1.1 Models cần tạo

### Team

```csharp
public class Team
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public Guid CreatedById { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;

    public AppUser CreatedBy { get; set; } = null!;
    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
}
```

### TeamMember

```csharp
public enum TeamRole { Owner, Manager, Member }

public class TeamMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TeamId { get; set; }
    public Guid UserId { get; set; }
    public TeamRole Role { get; set; } = TeamRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public Team Team { get; set; } = null!;
    public AppUser User { get; set; } = null!;
}
```

---

## 1.2 DTOs

```csharp
// CreateTeamDto
public class CreateTeamDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

// TeamDto (response)
public class TeamDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int MemberCount { get; set; }
    public string MyRole { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

// InviteMemberDto
public class InviteMemberDto
{
    public string Email { get; set; } = null!;
    public TeamRole Role { get; set; } = TeamRole.Member;
}

// ChangeRoleDto
public class ChangeRoleDto
{
    public TeamRole NewRole { get; set; }
}
```

---

## 1.3 API Endpoints

| Method | Endpoint                              | Permission          |
| ------ | ------------------------------------- | ------------------- |
| POST   | `/api/teams`                          | Any authenticated   |
| GET    | `/api/teams`                          | My teams            |
| GET    | `/api/teams/{id}`                     | Team member          |
| PUT    | `/api/teams/{id}`                     | Owner/Manager        |
| DELETE | `/api/teams/{id}`                     | Owner/Admin          |
| POST   | `/api/teams/{id}/members`             | Owner/Manager        |
| GET    | `/api/teams/{id}/members`             | Team member          |
| PATCH  | `/api/teams/{id}/members/{userId}`    | Owner only           |
| DELETE | `/api/teams/{id}/members/{userId}`    | Owner/Manager/Self   |

---

## 1.4 Service Interface

```csharp
public interface ITeamService
{
    Task<TeamDto> CreateAsync(CreateTeamDto dto, Guid userId);
    Task<List<TeamDto>> GetMyTeamsAsync(Guid userId);
    Task<TeamDto> GetByIdAsync(Guid teamId, Guid userId);
    Task UpdateAsync(Guid teamId, CreateTeamDto dto, Guid userId);
    Task DeleteAsync(Guid teamId, Guid userId);
    Task InviteMemberAsync(Guid teamId, InviteMemberDto dto, Guid userId);
    Task ChangeMemberRoleAsync(Guid teamId, Guid targetUserId, ChangeRoleDto dto, Guid userId);
    Task RemoveMemberAsync(Guid teamId, Guid targetUserId, Guid userId);
}
```

---

## 1.5 Business Rules

1. Khi tạo team → creator tự động trở thành **Owner**
2. Owner KHÔNG thể bị remove (phải transfer ownership trước)
3. Manager có thể invite Member, nhưng không thể invite Manager/Owner
4. Owner có thể change role của tất cả member
5. Member có thể tự rời team (remove self)
6. 1 user chỉ có 1 role trong 1 team
7. Không thể invite user đã là member

---

# 2. 🔐 Team-based Permission

## 2.1 Mở rộng TaskItem

Thêm field `TeamId` vào TaskItem:

```csharp
public Guid? TeamId { get; set; }
public Team? Team { get; set; }
```

* `TeamId = null` → Task cá nhân
* `TeamId != null` → Task thuộc team

---

## 2.2 Permission Matrix

| Action        | Admin | Owner | Manager | Member (assigned) | Member (not assigned) |
| ------------- | ----- | ----- | ------- | ----------------- | -------------------- |
| View task     | ✔     | ✔     | ✔       | ✔                 | ✔                    |
| Create task   | ✔     | ✔     | ✔       | ✖                 | ✖                    |
| Update task   | ✔     | ✔     | ✔       | ✔                 | ✖                    |
| Delete task   | ✔     | ✔     | ✔       | ✖                 | ✖                    |
| Assign task   | ✔     | ✔     | ✔       | ✖                 | ✖                    |
| Comment       | ✔     | ✔     | ✔       | ✔                 | ✖                    |

---

## 2.3 Permission Logic

```pseudo
function canModifyTeamTask(user, task):
    IF user.role == Admin → ALLOW

    member = getTeamMember(user.id, task.teamId)
    IF member == null → DENY

    IF member.role == Owner → ALLOW
    IF member.role == Manager → ALLOW
    IF member.role == Member:
        IF task.assignedTo == user.id → ALLOW
        ELSE → DENY
```

---

# 3. 💬 Comment System

## 3.1 Model

```csharp
public class Comment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Content { get; set; } = null!;
    public Guid TaskId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;

    public TaskItem Task { get; set; } = null!;
    public AppUser User { get; set; } = null!;
}
```

---

## 3.2 API Endpoints

| Method | Endpoint                       | Permission        |
| ------ | ------------------------------ | ----------------- |
| POST   | `/api/tasks/{id}/comments`     | Task member        |
| GET    | `/api/tasks/{id}/comments`     | Task member        |
| PUT    | `/api/comments/{id}`           | Comment owner      |
| DELETE | `/api/comments/{id}`           | Comment owner/Admin|

---

## 3.3 DTOs

```csharp
public class CreateCommentDto
{
    public string Content { get; set; } = null!;
}

public class CommentDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string? UserAvatar { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

---

# 4. 🗄️ Database Changes

## 4.1 New Tables

* Teams
* TeamMembers
* Comments

## 4.2 Migration

```bash
dotnet ef migrations add AddTeamAndComments
dotnet ef database update
```

## 4.3 DbContext Updates

```csharp
public DbSet<Team> Teams { get; set; }
public DbSet<TeamMember> TeamMembers { get; set; }
public DbSet<Comment> Comments { get; set; }
```

---

# 5. ✅ Checklist Phase 2

* [ ] Team model + migration
* [ ] TeamMember model + migration
* [ ] TeamService + TeamController
* [ ] Team invite/remove/change role
* [ ] Extend TaskItem with TeamId
* [ ] Update PermissionService for team
* [ ] Comment model + migration
* [ ] CommentService + CommentController
* [ ] Frontend: Team management page
* [ ] Frontend: Comment section in task detail
* [ ] Integration tests cho team flow

---
