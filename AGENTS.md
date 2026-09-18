# AGENTS.md — TaskHub

Tài liệu này là nguồn quy tắc chính thức cho AI agent (Codex/Claude/Copilot...) và
developer khi làm việc trên codebase TaskHub. Khi có xung đột giữa tài liệu này và
các file trong `docs/ai/*` hoặc docs ở root, **AGENTS.md thắng**, vì nó được đồng bộ
sát với code thực tế nhất.

---

## 1. Tổng quan kiến trúc

- **Backend**: .NET 9, layered architecture:
  `TaskHub.API` → `TaskHub.Application` → `TaskHub.Domain` ← `TaskHub.Infrastructure`
- **Frontend**: Vite + React 19 + React Router 7 + Tailwind 4 + Zustand + Axios + SignalR
- **Database**: SQL Server, EF Core Code First, migrations
- **Auth**: JWT access token (in-memory ở frontend) + refresh token (HTTP-only cookie), BCrypt cho password, hỗ trợ Google login

Luồng backend bắt buộc:

```
Controller  → chỉ route, authorize, map request/response
Service     → toàn bộ business logic, gọi Repository
Repository  → EF Core queries/persistence
DbContext   → chỉ được gọi từ Repository (KHÔNG gọi trực tiếp từ Service)
```

> Đây là thay đổi so với hiện trạng: một số Service đang gọi thẳng `IAppDbContext`.
> Coi đó là **tech debt**, không phải chuẩn để làm theo (xem mục 3.1).

---

## 2. Git Workflow — GitFlow (solo dev)

- `main` — luôn deployable, chỉ merge từ `release/*` hoặc `hotfix/*`
- `develop` — nhánh tích hợp chính, mọi feature merge vào đây trước
- `feature/<ten-tinh-nang>` — tạo từ `develop`, ví dụ `feature/task-time-tracking`
- `release/<version>` — cắt từ `develop` khi chuẩn bị release
- `hotfix/<ten>` — cắt từ `main` khi cần vá gấp production

Quy tắc commit:
- Conventional Commits: `feat:`, `fix:`, `refactor:`, `chore:`, `docs:`, `test:`
- Mỗi commit nên build được (không commit code half-broken)
- Trước khi mở PR / merge vào `develop`: chạy `dotnet build TaskHub.sln` và
  `npm run lint && npm run build`

Nguyên tắc cho AI agent khi sửa code:
- **Không đụng vào file untracked/refactor dở dang không liên quan tới task đang làm**
- Luôn `git status` trước khi edit để biết working tree đang "bẩn" chỗ nào
- Không tự ý xoá 2 thư mục migrations cũ/mới — phải xác nhận thư mục nào là chính thức
  trước, ghi chú lại trong PR nếu dọn dẹp

---

## 3. Backend Rules

### 3.1 Layering (bắt buộc, không có ngoại lệ cho code mới)
- Controller **không chứa business logic**, chỉ: nhận request → map DTO → gọi Service → trả `ApiResponse<T>`
- Mọi business logic mới **phải nằm trong Service**
- Mọi truy vấn/persist dữ liệu mới **phải đi qua Repository**, **không** được thêm
  `IAppDbContext` trực tiếp vào Service mới, kể cả khi code cũ đang làm vậy
- Code cũ đang vi phạm (Service gọi thẳng DbContext) được liệt kê là tech debt:
  khi chạm vào file đó cho bất kỳ lý do gì, ưu tiên refactor về dùng Repository
- Domain layer giữ "framework-light" — không phụ thuộc EF Core/ASP.NET

### 3.2 API Response & Error Handling
- Mọi endpoint trả về `ApiResponse<T>` — **không** còn pattern `return NotFound()/BadRequest()`
  thủ công trong controller. Khi sửa controller cũ có pattern này, refactor luôn về chuẩn chung.
- Business error → throw `AppException` subclass tương ứng, để `GlobalExceptionMiddleware`
  format response. Không tự try/catch rồi return lỗi thủ công trong Service/Controller.
- Exception không xác định → middleware trả 500, không lộ thông tin nội bộ (stack trace)
  ra response ở môi trường Production.

### 3.3 Validation
- **Chuẩn duy nhất: FluentValidation** cho mọi logic validate có điều kiện/phức tạp
  (cross-field, business rule, async check trùng dữ liệu...)
- DataAnnotations chỉ dùng cho constraint đơn giản, tĩnh trên DTO/Entity (`[Required]`,
  `[MaxLength]`) — không dùng để thay thế FluentValidation cho logic nghiệp vụ
- Validator đặt tại `TaskHub.Application/Validators`, naming `*Validator`

### 3.4 DTO & Data Exposure
- Không trả EF entity trực tiếp ra ngoài API — luôn map qua DTO
- Request/Response DTO đặt tại `TaskHub.Application/DTOs`, naming `*Dto`,
  cân nhắc nhóm theo feature: `DTOs/Tasks`, `DTOs/Boards`, `DTOs/Projects`...
  để dễ scale khi thêm module

### 3.5 Authorization & Permission
- Mọi kiểm tra quyền truy cập resource (board, task, project...) phải qua
  `PermissionService` tập trung — **không** rải rác `if (userId != resource.OwnerId)`
  trong controller/service
- Mọi endpoint trả về collection phải filter theo current user / quyền truy cập
  tương ứng, không trả toàn bộ bảng

### 3.6 Database & Migrations
- Enum lưu dạng string trong EF mapping (giữ nguyên convention hiện tại)
- Trước khi tạo migration mới: chạy `dotnet ef migrations list` để chắc chắn
  đang làm việc trên thư mục migrations chính thức, tránh tạo lệch schema
- Đặt tên migration mô tả rõ thay đổi: `AddTimeEntryTable`, không đặt tên chung chung

### 3.7 Pagination & Collection Endpoints
- Mọi endpoint trả danh sách (tasks, boards, notifications, activity logs...)
  phải hỗ trợ `page`, `pageSize`, và trả về theo dạng `PagedResult<T>`
  (tổng số item, tổng số trang) — không trả full list không giới hạn

### 3.8 API Versioning
- Route theo `/api/v1/...`
- Breaking change → tạo version mới (`/api/v2/...`), không sửa trực tiếp version cũ
  làm hỏng client hiện tại

### 3.9 Logging & Observability
- Dùng structured logging (Serilog khuyến nghị) thay vì `Console.WriteLine`/log rời rạc
- Middleware log kèm request id + user id (nếu có) cho mọi request
- Log riêng các sự kiện auth quan trọng: login thất bại, refresh token bị reuse/revoke

### 3.10 Security
- Áp dụng rate limiting cho các endpoint `/auth/login`, `/auth/register`, `/auth/refresh`
  để chống brute-force
- Không commit secrets: JWT signing key, Google client id/secret, SMTP config, connection
  string thật → dùng `dotnet user-secrets` (dev) hoặc biến môi trường (production)
- CORS chỉ whitelist origin của frontend thực tế, không dùng `AllowAnyOrigin` ở production

### 3.11 SignalR
- Hub method chỉ nhận input, gọi Service tương ứng (giống Controller), **không** chứa
  business logic trong Hub
- Đặt tên event rõ ràng theo domain: `TaskUpdated`, `BoardPresenceChanged`, `UserTyping`

### 3.12 Testing (mới, bắt buộc từ nay)
- Thêm test project: `TaskHub.Tests` (xUnit + Moq/NSubstitute + FluentAssertions)
- Mọi Service mới hoặc bị sửa logic quan trọng → cần unit test tối thiểu cho happy path
  + 1-2 edge case
- Flow nhạy cảm (auth, permission, board/task CRUD) cần thêm integration test
  (WebApplicationFactory) trước khi coi là "done"

---

## 4. Frontend Rules

- Toàn bộ gọi API nằm trong `src/api` (dùng `axiosInstance`), **không** gọi Axios
  trực tiếp từ component
- Luôn dùng alias `@/*` cho import nội bộ, không dùng relative path dài (`../../../`)
- Ưu tiên tái sử dụng UI primitive có sẵn (`Button`, `Card`, `Input`, `Modal`, `Select`,
  `Textarea`, `Badge`, `Avatar`, `Dropdown`) trước khi tạo component style mới
- Style bằng Tailwind theme token (`bg-bg-main`, `text-text-main`, `border-border-subtle`,
  `bg-surface-0`, `text-text-muted`...) — không hard-code màu light-only, luôn có dark variant
  để giữ đúng hành vi light/dark theme hiện tại
- Icon dùng `lucide-react`
- Zustand store: đặt tại `src/stores`, naming `useXStore`; giữ store gọn — side effect API
  nên gọi qua `src/api`, không nhét fetch logic trực tiếp trong store action nếu tránh được
  test/tái sử dụng khó khăn
- Component: PascalCase cho component/page, camelCase cho hook/store/API module,
  hook đặt tên `useX`
- Responsive-first: mọi UI mới cần kiểm tra ít nhất ở breakpoint mobile + desktop
- Biến môi trường frontend (`VITE_API_URL`...) không hard-code trong code, đặt trong
  `.env` (không commit `.env` thật, chỉ commit `.env.example`)

---

## 5. Auth Flow (giữ nguyên, ghi lại để agent không đổi sai)

- Access token: JWT, giữ trong memory ở frontend (không persist localStorage)
- Refresh token: HTTP-only cookie, silent refresh khi app mount
  (logic ở `frontend/src/api/axiosInstance.js`)
- Password: BCrypt hash
- Hỗ trợ Google OAuth login song song với email/password

---

## 6. Build & Validate Commands

```bash
# Backend
cd backend && dotnet build TaskHub.sln
dotnet ef migrations list --project TaskHub.Infrastructure   # kiểm tra trước khi thêm migration

# Frontend
cd frontend && npm run lint && npm run build
```

- Không chạy các lệnh build/tạo file khi user yêu cầu rõ "không sửa/tạo file" —
  vì build có thể sinh output ngoài ý muốn
- Test (khi đã có `TaskHub.Tests`): `dotnet test`

---

## 7. Documentation Policy

- `docs/ai/*` và các doc kiến trúc ở root (`DESIGN_SYSTEM_AND_ARCHITECTURE.md`,
  `PROJECT_ROADMAP_V2.md`...) là tài liệu tham khảo, **không phải nguồn sự thật tuyệt đối**
  — luôn đối chiếu với code thực tế trước khi áp dụng, vì hiện đang có phần lệch nhau
- Khi thực hiện thay đổi kiến trúc/API có ảnh hưởng lớn: cập nhật doc liên quan
  trong cùng PR. Nếu không cập nhật kịp, đánh dấu rõ đoạn doc bị ảnh hưởng là
  `>_OUTDATED_` kèm ngày, để lần sau không ai tưởng nhầm là đúng

---

## 8. Quick Do / Don't cho AI Agent

**Do**
- Tuân thủ layering: Controller → Service → Repository → DbContext
- Dùng `ApiResponse<T>` + `AppException` cho mọi response/lỗi
- Dùng FluentValidation cho logic validate
- Dùng PermissionService cho mọi check quyền
- Viết test cho logic quan trọng mới thêm
- `git status` trước khi sửa, giữ thay đổi trong phạm vi task

**Don't**
- Không thêm `IAppDbContext` trực tiếp vào Service mới
- Không trả EF entity thẳng ra API
- Không tự return `NotFound/BadRequest` thủ công trong controller mới
- Không hard-code màu/secret/URL
- Không sửa file untracked/refactor dở dang không liên quan đến task
- Không tạo migration mới mà chưa kiểm tra thư mục migrations hiện hành
