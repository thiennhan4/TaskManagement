# Báo cáo Kiểm thử Tính năng Dự án TaskHub

Tài liệu này tổng hợp kết quả kiểm thử các tính năng của dự án **TaskHub** (bao gồm tương tác Frontend ↔ Backend ↔ Database), phân loại các tính năng đã hoàn thiện và chưa hoàn thiện, kèm theo log lỗi chi tiết, nguyên nhân gốc rễ và đề xuất khắc phục.

---

## 1. TỔNG QUAN HỆ THỐNG
* **Backend:** ASP.NET Core Web API 9.0, Entity Framework Core, SQL Server (LocalDB).
* **Frontend:** React, Vite, Tailwind CSS, Zustand, React Flow (cho phần Automation Canvas).
* **Cơ chế liên lạc:** RESTful API & Realtime SignalR.

---

## 2. CÁC TÍNH NĂNG ĐÃ HOÀN THIỆN (Tương tác tốt Frontend ↔ Backend ↔ Database)

Các tính năng dưới đây đã được kiểm thử chạy thành công trọn vẹn từ luồng gửi yêu cầu từ client, xử lý logic tại API/Server và lưu trữ vào database.

| Nhóm tính năng | Chi tiết tính năng đã test thành công | API Endpoint | Trạng thái |
| :--- | :--- | :--- | :--- |
| **Authentication** | Đăng ký tài khoản mới, mã hóa mật khẩu, tạo token.<br>Đăng nhập nhận JWT & Refresh Token.<br>Lấy thông tin tài khoản hiện tại (`me`). | `POST /api/auth/register`<br>`POST /api/auth/login`<br>`GET /api/auth/me` | ✅ Hoạt động tốt |
| **Workspaces / Teams** | Tạo Workspace mới (Team).<br>Lấy danh sách các Workspace của người dùng.<br>Cập nhật thông tin Workspace.<br>Lấy danh sách thành viên trong Workspace. | `POST /api/teams`<br>`GET /api/teams`<br>`PUT /api/teams/{id}`<br>`GET /api/teams/{id}/members` | ✅ Hoạt động tốt |
| **Project Management** | Tạo dự án cá nhân (Personal Project).<br>Lấy danh sách dự án.<br>Lấy chi tiết dự án theo ID.<br>Cập nhật dự án.<br>Lấy danh sách Board thuộc dự án. | `POST /api/projects`<br>`GET /api/projects`<br>`GET /api/projects/{id}`<br>`PATCH /api/projects/{id}`<br>`GET /api/projects/{id}/boards` | ✅ Hoạt động tốt |
| **Boards & Columns** | Tạo Board mới.<br>Lấy chi tiết Board (bao gồm các cột danh sách bên trong).<br>Lấy danh sách các cột (`BoardLists`) theo Board.<br>Tạo, cập nhật và xóa cột (`BoardList`) trong Kanban Board. | `POST /api/boards`<br>`GET /api/boards/{id}`<br>`GET /api/boardlists/board/{id}`<br>`POST/PUT/DELETE /api/boardlists` | ✅ Hoạt động tốt |
| **Task Management** | Tạo Task mới trong cột.<br>Lấy chi tiết Task (kèm logs hoạt động).<br>Cập nhật thông tin Task.<br>Thay đổi trạng thái của Task (`status`).<br>Cập nhật tiến độ hoàn thành Task (`progress`). | `POST /api/tasks/lists/{listId}/tasks`<br>`GET /api/tasks/{id}`<br>`PUT /api/tasks/{id}`<br>`PATCH /api/tasks/{id}/status`<br>`PATCH /api/tasks/{id}/progress` | ✅ Hoạt động tốt |
| **Time Tracking** | Bắt đầu chạy bộ đếm thời gian (`Start Timer`).<br>Lấy thông tin Timer đang chạy.<br>Dừng bộ đếm thời gian và lưu lại nhật ký làm việc.<br>Ghi nhận nhật ký thời gian thủ công (`Manual Entry`).<br>Lấy danh sách nhật ký thời gian cá nhân.<br>Xuất báo cáo thời gian (`Time Report`) phân tích theo task, theo ngày và theo user. | `POST /api/TimeTracking/start`<br>`GET /api/TimeTracking/running`<br>`POST /api/TimeTracking/{id}/stop`<br>`POST /api/TimeTracking/manual`<br>`GET /api/TimeTracking/my-entries`<br>`GET /api/TimeTracking/report` | ✅ Hoạt động tốt |
| **Dashboard & Analytics** | Lấy dữ liệu thống kê tổng hợp số lượng task (Todo, In Progress, Done, Overdue) hiển thị trên màn hình chính.<br>Lấy danh sách các task gần đây và các task sắp đến hạn.<br>Thống kê hiệu suất làm việc (`Velocity`, `Burndown chart`) theo Board. | `GET /api/dashboard/stats`<br>`GET /api/dashboard/recent-tasks`<br>`GET /api/dashboard/upcoming`<br>`GET /api/Analytics/overview` | ✅ Hoạt động tốt |
| **Automations (Rules)** | Tạo quy trình tự động hóa (Ví dụ: *Khi Status thay đổi thành InProgress thì tự động Assign cho tôi*).<br>Kích hoạt/Tạm dừng luật tự động hóa.<br>Lấy lịch sử thực thi của các luật tự động hóa.<br>Lưu vị trí tọa độ các Node và Connection trên Canvas trực quan. | `POST /api/Automation`<br>`POST /api/Automation/{id}/toggle`<br>`GET /api/Automation/board/{id}/history`<br>`PUT /api/Automation/{id}/canvas` | ✅ Hoạt động tốt |

---

## 3. CÁC TÍNH NĂNG CHƯA HOÀN THIỆN / BỊ LỖI (Incomplete or Broken)

Dưới đây là các tính năng bị lỗi hoặc mới chỉ được triển khai một phần (chạy được dưới 50% hoặc lỗi hoàn toàn).

### 3.1. Hệ thống Bình luận (Comment System) — ❌ BỊ LỖI HOÀN TOÀN (HTTP 500)
* **Mô tả:** Tính năng thêm bình luận vào Task bị crash hệ thống ngay khi gọi API.
* **Nguyên nhân:** Xung đột định tuyến (Routing Conflict) trong ASP.NET Core. Cả hai Controller `TaskItemController` và `CommentController` đều đăng ký cùng một route `POST /api/tasks/{taskId}/comments` và `GET /api/tasks/{taskId}/comments`, dẫn đến lỗi `AmbiguousMatchException`.

### 3.2. Xóa Dự án (Delete Project) — ❌ BỊ LỖI (HTTP 500)
* **Mô tả:** Người dùng là Owner của Project không thể xóa dự án của mình khi dự án đó đã có Kanban Board đi kèm.
* **Nguyên nhân:** Lỗi vi phạm khóa ngoại SQL Server. Khi tạo Project, hệ thống tự động tạo một "Main Board" liên kết với Project đó. Khi thực hiện xóa Project, database ném ra lỗi xung đột ràng buộc tham chiếu (`FK_Boards_Projects_ProjectId`) do cấu hình quan hệ giữa Project và Board là `DeleteBehavior.NoAction` (không cho phép xóa cascade).

### 3.3. Tạo dự án bị trùng Slug giữa các User khác nhau — ❌ BỊ LỖI (HTTP 500)
* **Mô tả:** Nếu User A đã tạo một dự án có slug là `personal-project`, khi User B tạo một dự án cũng có slug là `personal-project` thì hệ thống sẽ báo lỗi hệ thống 500 thay vì xử lý mượt mà.
* **Nguyên nhân:** Ràng buộc cơ sở dữ liệu và logic nghiệp vụ không khớp nhau. 
  * Cơ sở dữ liệu cấu hình index unique toàn cục trên cột `Slug` của bảng `Projects` (`IX_Projects_Slug`).
  * Trong khi đó, file `ProjectService.cs` chỉ kiểm tra trùng lặp slug trong phạm vi dự án cá nhân của chính user đó hoặc trong cùng một workspace (`p.Slug == slug && (p.OwnerId == userId || p.WorkspaceId == workspaceId)`). Do đó, kiểm tra nghiệp vụ của User B vượt qua, nhưng lệnh ghi database ném ra lỗi trùng khóa trùng lặp (`SqlException` 2601).

### 3.4. Đăng nhập qua Google (Google Login) — 🔄 CHỈ HOÀN THIỆN 1/2
* **Mô tả:** Endpoint API `POST /api/auth/google` đã được viết ở backend, và giao diện nút đăng nhập Google cũng có ở frontend. Tuy nhiên, tính năng chưa hoạt động được do chưa có cấu hình Client Secret thực tế và Client ID thực tế trên môi trường sản xuất.

---

## 4. CHI TIẾT LOG LỖI & PHÂN TÍCH NGUYÊN NHÂN GỐC RỄ

### Lỗi 1: Xung đột API Bình luận (Comments)
* **Log lỗi tại API:**
```text
Microsoft.AspNetCore.Routing.Matching.AmbiguousMatchException: The request matched multiple endpoints. Matches: 

TaskHub.API.Controllers.TaskItemController.AddComment (TaskHub.API)
TaskHub.API.Controllers.CommentController.CreateComment (TaskHub.API)
   at Microsoft.AspNetCore.Routing.Matching.DefaultEndpointSelector.ReportAmbiguity(Span`1 candidateState)
   at Microsoft.AspNetCore.Routing.Matching.DefaultEndpointSelector.ProcessFinalCandidates(HttpContext httpContext, Span`1 candidateState)
   ...
```
* **Phân tích code:**
  * **TaskItemController.cs**:
    ```csharp
    [Route("api/tasks")]
    public class TaskItemController : ControllerBase {
        [HttpPost("{id}/comments")]
        public async Task<IActionResult> AddComment(Guid id, [FromBody] CreateCommentDto dto...)
    }
    ```
  * **CommentController.cs**:
    ```csharp
    [Route("api/tasks")]
    public class CommentController : ControllerBase {
        [HttpPost("{taskId}/comments")]
        public async Task<IActionResult> CreateComment(Guid taskId, [FromBody] CreateCommentDto dto...)
    }
    ```
  * **Khắc phục:** Cần xóa bỏ các endpoint bình luận thừa trong `TaskItemController` và chỉ giữ lại duy nhất một nơi quản lý là `CommentController`.

---

### Lỗi 2: Lỗi vi phạm ràng buộc khóa ngoại khi xóa dự án (Delete Project)
* **Log lỗi tại API:**
```text
fail: Microsoft.EntityFrameworkCore.Update[10000]
      An exception occurred in the database while saving changes for context type 'TaskHub.Infrastructure.Data.AppDbContext'.
      Microsoft.EntityFrameworkCore.DbUpdateException: An error occurred while saving the entity changes.
       ---> Microsoft.Data.SqlClient.SqlException (0x80131904): The DELETE statement conflicted with the REFERENCE constraint "FK_Boards_Projects_ProjectId". The conflict occurred in database "TaskHubDb", table "dbo.Boards", column 'ProjectId'.
```
* **Phân tích code:**
  * Trong **AppDbContext.cs**:
    ```csharp
    modelBuilder.Entity<Board>()
        .HasOne(b => b.Project)
        .WithMany(p => p.Boards)
        .HasForeignKey(b => b.ProjectId)
        .OnDelete(DeleteBehavior.NoAction); // <--- Lỗi ở đây
    ```
  * Ràng buộc này chỉ định khi xóa dự án thì không được tự động xóa Board (`NoAction`).
  * **Khắc phục:** Cần đổi thành `DeleteBehavior.Cascade` (Xóa dự án sẽ tự động xóa sạch các Board liên quan) hoặc trong `ProjectService.DeleteProjectAsync` phải chủ động truy vấn và xóa hết các Board liên kết trước khi thực hiện xóa Project.

---

### Lỗi 3: Lỗi trùng lặp Slug dự án toàn cục (Duplicate Project Slug)
* **Log lỗi tại API:**
```text
fail: Microsoft.EntityFrameworkCore.Update[10000]
      An exception occurred in the database while saving changes for context type 'TaskHub.Infrastructure.Data.AppDbContext'.
      Microsoft.EntityFrameworkCore.DbUpdateException: An error occurred while saving the entity changes.
       ---> Microsoft.Data.SqlClient.SqlException (0x80131904): Cannot insert duplicate key row in object 'dbo.Projects' with unique index 'IX_Projects_Slug'. The duplicate key value is (personal-project).
```
* **Phân tích code:**
  * Trong **AppDbContext.cs**:
    ```csharp
    modelBuilder.Entity<Project>()
        .HasIndex(p => p.Slug)
        .IsUnique(); // <--- Đánh dấu unique toàn cục
    ```
  * Cấu hình trên ép buộc cột `Slug` phải là duy nhất trên toàn hệ thống (tất cả các user). Điều này không hợp lý vì các user khác nhau hoàn toàn có quyền đặt tên dự án cá nhân giống nhau (ví dụ: `my-project`).
  * **Khắc phục:** Cấu hình lại index unique là sự kết hợp của `Slug` + `OwnerId` (đối với dự án cá nhân) hoặc `Slug` + `WorkspaceId` (đối với dự án đội nhóm), thay vì chỉ unique đơn lẻ cột `Slug`.

---

## 5. CÁC KIẾN NGHỊ VÀ ĐỀ XUẤT HÀNH ĐỘNG

1. **Xử lý Xung đột Routing bình luận:**
   * Hãy xóa bỏ hoàn toàn các Action liên quan đến Comment trong `TaskItemController`. Mọi thao tác lấy danh sách bình luận, thêm và xóa bình luận nên được giao cho `CommentController` xử lý thống nhất.
2. **Cấu hình Cascade Delete cho Dự án & Board:**
   * Thay đổi cấu hình quan hệ Board-Project trong `AppDbContext.cs` từ `DeleteBehavior.NoAction` sang `DeleteBehavior.Cascade` để cơ sở dữ liệu tự động dọn dẹp các Board và Column liên kết khi xóa một dự án.
3. **Sửa đổi Index Unique của Project Slug:**
   * Thay vì `.HasIndex(p => p.Slug).IsUnique()`, hãy sửa đổi cấu hình Entity Framework để tạo một index phức hợp:
     ```csharp
     modelBuilder.Entity<Project>()
         .HasIndex(p => new { p.Slug, p.OwnerId, p.WorkspaceId })
         .IsUnique();
     ```
