# 📌 TaskHub Backend Development Tasks Plan (.md)

---

# 🚀 TaskHub Backend — Development Workflow

Mục tiêu:

* Build backend production-style bằng ASP.NET Core Web API
* Follow layered architecture
* Secure JWT auth
* Scalable permission system
* Ready for React frontend integration

---

# 📂 Phase 0 — Project Initialization

## 🎯 Goal

Setup project structure và foundation chuẩn trước khi code business logic.

---

## ✅ Tasks

### 0.1 Create ASP.NET Core Web API project

* Create solution:

```bash
dotnet new sln -n TaskHub
```

* Create API project:

```bash
dotnet new webapi -n TaskHub.API
```

* Add project vào solution:

```bash
dotnet sln add TaskHub.API
```

---

### 0.2 Install Required Packages

## Core

```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

## Validation

```bash
dotnet add package FluentValidation.AspNetCore
```

## Password Hash

```bash
dotnet add package BCrypt.Net-Next
```

## Swagger

```bash
dotnet add package Swashbuckle.AspNetCore
```

## Logging

```bash
dotnet add package Serilog.AspNetCore
```

---

### 0.3 Create Folder Structure

Create folders:

```txt
Controllers/
Services/
Services/Interfaces/
Models/
DTOs/
Validators/
Middleware/
Helpers/
Common/
Data/
Enums/
Configurations/
```

---

### 0.4 Setup appsettings.json

Add:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  },

  "Jwt": {
    "Secret": "",
    "Issuer": "TaskHub",
    "Audience": "TaskHubClient",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 7
  }
}
```

---

# 📂 Phase 1 — Database Setup

## 🎯 Goal

Setup EF Core + SQL Server + initial schema.

---

## ✅ Tasks

### 1.1 Create Enums

Create:

* GlobalRole.cs
* TaskStatus.cs
* TaskPriority.cs

---

### 1.2 Create Models

Create:

* User.cs
* TaskItem.cs
* RefreshToken.cs

---

### 1.3 Create BaseEntity

Add:

* Id
* CreatedAt
* UpdatedAt
* IsDeleted

---

### 1.4 Create AppDbContext

Add DbSets:

* Users
* Tasks
* RefreshTokens

---

### 1.5 Configure Relationships

Configure:

* User → Tasks
* User → RefreshTokens

---

### 1.6 Add EF Migration

```bash
dotnet ef migrations add InitialCreate
```

---

### 1.7 Update Database

```bash
dotnet ef database update
```

---

# 📂 Phase 2 — Common Infrastructure

## 🎯 Goal

Build reusable core infrastructure.

---

## ✅ Tasks

### 2.1 Create ApiResponse Wrapper

Create:

```txt
Common/ApiResponse.cs
```

---

### 2.2 Create ExceptionHandlerMiddleware

Features:

* catch exceptions
* map status code
* return ApiResponse.Fail

---

### 2.3 Setup Serilog

Features:

* console log
* file log
* request log

---

### 2.4 Setup Swagger JWT

Features:

* JWT bearer auth
* authorize button

---

### 2.5 Configure Dependency Injection

Register:

* Services
* DbContext
* Validators

---

# 📂 Phase 3 — Authentication Module

## 🎯 Goal

Build secure JWT auth flow.

---

## ✅ Tasks

### 3.1 Create Auth DTOs

Create:

* RegisterRequest
* LoginRequest
* AuthResponse
* RefreshTokenRequest

---

### 3.2 Create Validators

Create:

* RegisterRequestValidator
* LoginRequestValidator

---

### 3.3 Create PasswordHelper

Functions:

* HashPassword
* VerifyPassword

---

### 3.4 Create JwtHelper

Functions:

* GenerateAccessToken
* GenerateRefreshToken

---

### 3.5 Create AuthService Interface

Create:

```txt
IAuthService.cs
```

Methods:

* RegisterAsync
* LoginAsync
* RefreshTokenAsync
* LogoutAsync

---

### 3.6 Implement AuthService

Features:

* register
* login
* JWT generation
* refresh token rotation
* revoke token

---

### 3.7 Create AuthController

Endpoints:

* POST /register
* POST /login
* POST /refresh
* POST /logout

---

### 3.8 Test Auth Flow

Test:

* register
* login
* refresh
* logout

Using:

* Swagger
* Postman

---

# 📂 Phase 4 — User Module

## 🎯 Goal

Build user profile & admin lock system.

---

## ✅ Tasks

### 4.1 Create User DTOs

Create:

* UserProfileResponse

---

### 4.2 Create IUserService

Methods:

* GetProfileAsync
* SetLockStatusAsync

---

### 4.3 Implement UserService

Features:

* get current user profile
* lock/unlock account

---

### 4.4 Create UsersController

Endpoints:

* GET /users/me
* PATCH /users/{id}/lock

---

### 4.5 Add Admin Authorization Policy

Create:

```txt
AdminOnly
```

---

# 📂 Phase 5 — Permission System

## 🎯 Goal

Build centralized permission logic.

---

## ✅ Tasks

### 5.1 Create IPermissionService

Methods:

* CanAccessTask
* CanModifyTask
* CanDeleteTask

---

### 5.2 Implement PermissionService

Rules:

* creator owns personal task
* admin override delete

---

### 5.3 Integrate PermissionService

Use inside:

* TaskService

NOT:

* Controller

---

# 📂 Phase 6 — Task Module

## 🎯 Goal

Build personal task CRUD system.

---

## ✅ Tasks

### 6.1 Create Task DTOs

Create:

* CreateTaskRequest
* UpdateTaskRequest
* TaskResponse

---

### 6.2 Create Validators

Create:

* CreateTaskRequestValidator
* UpdateTaskRequestValidator

---

### 6.3 Create ITaskService

Methods:

* CreateTaskAsync
* GetMyTasksAsync
* GetTaskByIdAsync
* UpdateTaskAsync
* DeleteTaskAsync

---

### 6.4 Implement TaskService

Features:

* create task
* update task
* delete task
* permission check
* set UpdatedAt

---

### 6.5 Create TasksController

Endpoints:

* POST /tasks
* GET /tasks
* GET /tasks/{id}
* PUT /tasks/{id}
* DELETE /tasks/{id}

---

### 6.6 Add Pagination

Support:

* page
* pageSize

---

### 6.7 Add Filtering

Support:

* status
* priority

---

### 6.8 Add Sorting

Support:

* createdAt
* dueDate

---

### 6.9 Test Task Flow

Test:

* create
* update
* delete
* unauthorized access

---

# 📂 Phase 7 — Security Hardening

## 🎯 Goal

Production security improvements.

---

## ✅ Tasks

### 7.1 Add Rate Limiting

Protect:

* login
* register
* refresh

---

### 7.2 Add HTTPS Redirection

```csharp
app.UseHttpsRedirection();
```

---

### 7.3 Configure CORS

Allow frontend:

```txt
localhost:5173
```

---

### 7.4 Add JWT Expiration Validation

Ensure:

* expired token rejected

---

### 7.5 Add Refresh Token Hashing

Store:

* SHA256 hash only

---

# 📂 Phase 8 — Team System (Phase 2)

## 🎯 Goal

Build multi-user collaboration system.

---

## ✅ Tasks

### 8.1 Create Team Models

Create:

* Team
* TeamMember

---

### 8.2 Create TeamRole Enum

Values:

* Owner
* Manager
* Member

---

### 8.3 Create Team CRUD

Features:

* create
* update
* delete

---

### 8.4 Create Team Member Management

Features:

* invite member
* remove member
* change role

---

### 8.5 Extend PermissionService

Add:

* team permission logic
* role matrix

---

### 8.6 Team Task System

Features:

* assign task
* shared visibility
* role-based update/delete

---

# 📂 Phase 9 — Comment System

## 🎯 Goal

Task discussion system.

---

## ✅ Tasks

### 9.1 Create Comment Model

Fields:

* Content
* TaskId
* UserId

---

### 9.2 Create Comment APIs

Endpoints:

* POST /tasks/{id}/comments
* GET /tasks/{id}/comments

---

### 9.3 Add Comment Permission Check

Rules:

* assigned member
* manager
* owner

---

# 📂 Phase 10 — Dashboard & Optimization

## 🎯 Goal

Performance & analytics.

---

## ✅ Tasks

### 10.1 Dashboard APIs

Features:

* task count
* overdue tasks
* status summary

---

### 10.2 Add Database Indexes

Indexes:

* Users.Email
* Tasks.CreatedById
* RefreshTokens.UserId

---

### 10.3 Add AsNoTracking

Use for:

* read-only queries

---

### 10.4 Add Soft Delete

Fields:

* IsDeleted
* DeletedAt

---

### 10.5 Add API Versioning

Format:

```txt
/api/v1/tasks
```

---

# 📂 Phase 11 — Testing

## 🎯 Goal

Improve backend reliability.

---

## ✅ Tasks

### 11.1 Unit Test

Test:

* AuthService
* PermissionService
* TaskService

---

### 11.2 Integration Test

Test:

* auth flow
* task flow
* permission flow

---

### 11.3 Postman Collection

Create:

* full API collection
* environment variables

---

# 📂 Phase 12 — Deployment Ready

## 🎯 Goal

Prepare production deployment.

---

## ✅ Tasks

### 12.1 Environment Variables

Move secrets:

* JWT secret
* DB connection

---

### 12.2 Setup CI/CD

GitHub Actions:

* build
* test

---

### 12.3 Production Logging

Store:

* rolling log files

---

### 12.4 Deployment

Deploy:

* backend
* SQL Server

---

# ✅ FINAL CHECKLIST

* [ ] Clean layered architecture
* [ ] JWT auth completed
* [ ] Refresh token rotation completed
* [ ] PermissionService centralized
* [ ] Personal task CRUD completed
* [ ] Team system completed
* [ ] Comment system completed
* [ ] Pagination/filtering/sorting completed
* [ ] Validation completed
* [ ] Swagger JWT completed
* [ ] Logging completed
* [ ] Rate limiting completed
* [ ] Soft delete completed
* [ ] API versioning completed
* [ ] Unit tests completed
* [ ] Integration tests completed
* [ ] Production deployment ready
