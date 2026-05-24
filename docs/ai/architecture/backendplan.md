# 🚀 TaskHub Backend Plan (Production-ready SaaS)

---

# 1. 🎯 Mục tiêu hệ thống

TaskHub là hệ thống quản lý công việc theo mô hình SaaS với:

* Quản lý task cá nhân & team
* Phân quyền đa tầng (RBAC + Resource-based)
* Kiến trúc dễ mở rộng, maintain lâu dài

---

# 2. 🧠 Kiến trúc tổng thể

## 2.1 Layered Architecture

```txt
Controller → Service → Repository (optional) → DbContext → Database
```

## 2.2 Nguyên tắc

* Controller: chỉ xử lý request/response
* Service: xử lý business logic
* DbContext: truy cập DB
* Middleware: xử lý global (auth, exception)

---

# 3. 🛠 Tech Stack

## Backend

* ASP.NET Core Web API (.NET 8)
* Entity Framework Core (Code First)
* SQL Server

## Auth

* JWT (Access Token)
* Refresh Token

## Dev Tools

* Swagger / OpenAPI
* Postman

---

# 4. 🔐 Authentication

## 4.1 Register

* Tạo user mới
* Role mặc định: `User`

## 4.2 Login

* Trả JWT Token
* Claims:

  * UserId
  * Email
  * GlobalRole

## 4.3 Refresh Token

* Gia hạn access token

## 4.4 Logout

* Revoke refresh token

---

# 5. 🔑 Authorization (CORE)

## 5.1 Global Roles

| Role  | Quyền               |
| ----- | ------------------- |
| Admin | Toàn quyền hệ thống |
| User  | Người dùng thường   |

---

## 5.2 Team Roles

| Role    | Quyền                 |
| ------- | --------------------- |
| Owner   | Full quyền trong team |
| Manager | Quản lý task          |
| Member  | Làm task              |

---

## 5.3 Resource-based Authorization

👉 Quyền không chỉ dựa vào role mà còn phụ thuộc vào resource:

Ví dụ:

* User thuộc team A → không được sửa task team B

---

## 5.4 Authorization Logic

```pseudo
IF Admin → allow

IF Task.TeamId == NULL:
    IF CreatedBy == user → allow

IF Task.TeamId != NULL:
    role = getUserRoleInTeam(user, task.TeamId)

    IF role == Owner → allow
    IF role == Manager → allow
    IF role == Member:
        IF task.AssignedTo == user → allow
        ELSE deny
```

---

# 6. 👤 User Module

## Features

* Register / Login
* Lock / Unlock account (Admin)
* View profile
* Update profile / Change password

---

# 7. 👥 Team Module

## Features

* Create team
* Update team
* Delete team (Owner/Admin)

---

## Team Member

* Invite user
* Remove user
* Change role

---

## Rule

* 1 team có nhiều member
* 1 user có thể thuộc nhiều team

---

# 8. 📋 Task Module

## 8.1 Task cá nhân

* `TeamId = NULL`
* Chỉ creator được CRUD

---

## 8.2 Task team

| Action | Owner | Manager | Member   |
| ------ | ----- | ------- | -------- |
| Create | ✔     | ✔       | ✖        |
| Assign | ✔     | ✔       | ✖        |
| View   | ✔     | ✔       | ✔        |
| Update | ✔     | ✔       | Assigned |
| Delete | ✔     | ✔       | ✖        |

---

## 8.3 Task Fields

* Id (Guid)
* Title
* Description
* Status (Todo, Doing, Done)
* Priority
* OwnerId
* ListId
* DueDate
* Label
* Progress
* Position
* CreatedAt / UpdatedAt

---

# 9. 📋 Board & List Module

## Board

* Mỗi user có thể tạo nhiều Board
* Board chứa nhiều BoardList
* BoardList chứa nhiều TaskItem

## Hierarchy

```txt
User → Board → BoardList → TaskItem
```

---

# 10. 💬 Comment Module

## Features

* Add comment vào task
* Xem danh sách comment

## Permission

* Member: comment task được assign
* Manager/Owner: comment tất cả

---

# 11. 🌐 API Design

## Auth

* POST /api/auth/register
* POST /api/auth/login
* POST /api/auth/refresh
* POST /api/auth/logout

---

## Users / Settings

* GET /api/settings/profile
* PUT /api/settings/profile
* PUT /api/settings/password
* GET /api/settings/activity

---

## Dashboard

* GET /api/dashboard/stats
* GET /api/dashboard/recent-tasks

---

## Boards

* POST /api/boards
* GET /api/boards
* GET /api/boards/{id}
* PUT /api/boards/{id}
* DELETE /api/boards/{id}

---

## Board Lists

* POST /api/boards/{boardId}/lists
* GET /api/boards/{boardId}/lists
* PUT /api/lists/{id}
* DELETE /api/lists/{id}

---

## Tasks

* POST /api/lists/{listId}/tasks
* GET /api/lists/{listId}/tasks
* GET /api/tasks/{id}
* PUT /api/tasks/{id}
* DELETE /api/tasks/{id}
* PUT /api/tasks/{id}/move
* PATCH /api/tasks/{id}/progress

---

## Comments

* POST /api/tasks/{id}/comments
* GET /api/tasks/{id}/comments

---

# 12. 🧱 Database Design

## Tables

* Users (AppUser)
* Boards
* BoardLists (Lists)
* Tasks (TaskItem)
* RefreshTokens
* AuditLogs
* Comments (Phase 2)
* Teams (Phase 2)
* TeamMembers (Phase 2)

---

# 13. 🏗 Project Structure

```txt
TaskHub/
├── backend/
│   ├── Controllers/
│   ├── Services/
│   │   └── Interfaces/
│   ├── Repositories/
│   │   └── Interfaces/
│   ├── Models/
│   ├── DTOs/
│   ├── Data/
│   ├── Middleware/
│   └── View/
├── taskhub/          (React Frontend - Vite)
│   ├── src/
│   │   ├── api/
│   │   ├── components/
│   │   ├── context/
│   │   ├── pages/
│   │   └── styles/
├── Migrations/
├── docs/
└── Program.cs
```

---

# 14. ⚙️ Core Components đã build

## 14.1 AuthService

* Register / Login
* Generate JWT + Refresh Token
* Token rotation

## 14.2 TokenService

* Generate Access Token
* Generate Refresh Token
* Validate token

## 14.3 BoardService

* CRUD Board

## 14.4 BoardListService

* CRUD BoardList

## 14.5 TaskItemService

* CRUD TaskItem
* Move task between lists
* Update progress

---

# 15. 🚀 Development Roadmap

## Phase 1 ✅

* Auth (JWT + Refresh Token)
* User profile & settings
* Board CRUD
* BoardList CRUD
* Task CRUD
* Dashboard

## Phase 2

* Team system
* Team-based permission
* Comment system

## Phase 3

* Advanced authorization
* Notifications
* Performance optimization

---

# 16. ⚠️ Best Practices

* Không viết logic trong Controller
* Dùng Enum thay vì string (đang dùng string cho Status)
* Validate DTO
* Tách Permission logic riêng
* Không hardcode role
* Dùng ApiResponse<T> wrapper cho mọi response

---

# 17. 🎯 Mục tiêu cuối

* Backend clean & scalable
* Dễ maintain
* Dễ tích hợp frontend
* Đạt chuẩn production SaaS

---
