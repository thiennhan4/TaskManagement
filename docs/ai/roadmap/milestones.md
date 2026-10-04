# 🏁 TaskHub — Milestones & Progress Tracker

---

# Tổng quan tiến độ

| Phase | Tên                     | Status        | Hoàn thành |
| ----- | ----------------------- | ------------- | ---------- |
| 0     | Project Initialization  | ✅ Done       | 100%       |
| 1     | Database Setup          | ✅ Done       | 100%       |
| 2     | Common Infrastructure   | ✅ Done       | 100%       |
| 3     | Authentication Module   | ✅ Done       | 100%       |
| 4     | User Module             | ✅ Done       | 100%       |
| 5     | Permission System       | 🔄 Partial   | 60%        |
| 6     | Task Module             | ✅ Done       | 100%       |
| 7     | Security Hardening      | 🔄 Partial   | 70%        |
| 8     | Team System             | ⏳ Pending   | 0%         |
| 9     | Comment System          | ⏳ Pending   | 0%         |
| 10    | Dashboard & Optimization| 🔄 Partial   | 50%        |
| 11    | Testing                 | ⏳ Pending   | 0%         |
| 12    | Deployment Ready        | ⏳ Pending   | 0%         |

---

# 📌 Milestone 1: MVP Personal Task (DONE ✅)

**Deadline:** Phase 0–6
**Mô tả:** User có thể đăng ký, đăng nhập, và quản lý task cá nhân qua Kanban board.

## Deliverables

- [x] JWT Authentication (register, login, refresh, logout)
- [x] User profile management
- [x] Board CRUD
- [x] BoardList CRUD
- [x] TaskItem CRUD
- [x] Move task between lists
- [x] Update task progress
- [x] Dashboard stats & recent tasks
- [x] Global exception middleware
- [x] ApiResponse wrapper
- [x] Rate limiting on auth endpoints
- [x] CORS configuration
- [x] React frontend (Vite) — Login, Register, Dashboard, Board, Settings

---

# 📌 Milestone 2: Security & Quality (IN PROGRESS 🔄)

**Deadline:** Phase 7
**Mô tả:** Harden security và improve code quality.

## Deliverables

- [x] Rate limiting (AuthRateLimit — 5 req/min)
- [x] CORS (localhost:5173 allowed)
- [x] JWT validation (iss, aud, exp, signing key)
- [x] ClockSkew = Zero
- [ ] HTTPS redirection
- [ ] Refresh token hashing (SHA256)
- [ ] Input validation (FluentValidation — chuyển từ DataAnnotations)
- [ ] Serilog structured logging
- [ ] Audit log enhancement

---

# 📌 Milestone 3: Team Collaboration (PENDING ⏳)

**Deadline:** Phase 8–9
**Mô tả:** Multi-user team system với RBAC.

## Deliverables

- [ ] Team model & migration
- [ ] TeamMember management (invite, remove, role change)
- [ ] Team-based task permission
- [ ] Extend TaskItem with TeamId
- [ ] Comment system
- [ ] Frontend: Team management page
- [ ] Frontend: Comment section

---

# 📌 Milestone 4: Production Ready (PENDING ⏳)

**Deadline:** Phase 10–12
**Mô tả:** Optimize, test, deploy.

## Deliverables

- [ ] Unit tests (AuthService, TaskService, PermissionService)
- [ ] Integration tests (auth flow, task flow)
- [ ] Postman collection
- [ ] Soft delete implementation
- [ ] AsNoTracking for read queries
- [ ] API versioning (/api/v1/...)
- [ ] Environment variables for secrets
- [ ] CI/CD (GitHub Actions)
- [ ] Production logging (rolling files)
- [ ] Deployment

---

# 📊 Thống kê hiện tại

## Backend

| Component           | Files | Status |
| ------------------- | ----- | ------ |
| Controllers         | 7     | ✅     |
| Services            | 6     | ✅     |
| Service Interfaces  | —     | ✅     |
| Repositories        | 3     | ✅     |
| Repo Interfaces     | —     | ✅     |
| Models              | 4     | ✅     |
| DTOs                | 5     | ✅     |
| Middleware           | 1     | ✅     |
| Migrations          | 2     | ✅     |

## Frontend

| Component    | Status |
| ------------ | ------ |
| Auth pages   | ✅     |
| Dashboard    | ✅     |
| Board/Kanban | ✅     |
| Settings     | ✅     |
| Navbar       | ✅     |
| API layer    | ✅     |
| Auth context | ✅     |

---

# 🔄 Cập nhật lần cuối

**Ngày:** 2026-05-07
**Ghi chú:** Hoàn thành Milestone 1 (MVP). Đang trong quá trình hardening security (Milestone 2).

---
