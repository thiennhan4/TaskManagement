# Phase 1 implementation plan — Security and API Contract Reliability

Status: Phase 1 implementation and automated validation PASS, 2026-09-24. The user authorized the final commit; push remains prohibited. Manual runtime checks in section 14 remain required. Sections 1–13 retain the original planning record; sections 14–15 record implementation and commit validation.

Sources: [AGENTS.md](../../AGENTS.md), [full audit](TASKHUB_FULL_AUDIT_AND_ROADMAP.md), [development roadmap](TASKHUB_DEVELOPMENT_ROADMAP.md). AGENTS.md takes precedence. The original planning-only restriction below was superseded by the user's explicit instruction to implement and finish Phase 1, including authorized binary attachment downloads. No commit, push, migration or Phase 2 work is authorized.

Phase 1 contains **16 audit entries: 15 confirmed findings and one likely finding requiring verification**: C01, C03–C10, C17, C24, C28, C32, C46, C47 and L10. Supporting collection/visibility observations below belong to these authorization/contract boundaries and audit section 14; they are not newly invented issue IDs. Phase 2 owns C02 atomic refresh and broader transactional repair; Phase 1 must not be advertised as production-ready while those remain.

## 1. Verified Git state and protected baseline

Read-only checks performed: `git status --short`, `git branch --show-current`, `git log --oneline -10`, `git diff --stat`. Current branch: `feature/project-collaboration-dashboard-publish`. The requested log command returned only two commits:

```text
a716825 feat: publish sanitized TaskHub collaboration dashboard
3441966 Initial commit
```

No branch was changed. No stash, backup ref, secret store, deployed database or live account was inspected. No build/test/lint/package command was run during planning. Scoped AGENTS.md discovery under backend/frontend/docs found no additional instruction files. `rg` is unavailable in this workspace; inspection used PowerShell file reads/searches.

### Existing uncommitted work — not Phase 1 implementation

| Baseline state | Exact paths / scope | Handling during future implementation |
|---|---|---|
| Modified auth host/config | `backend/TaskHub.API/Controllers/authController.cs`, `backend/TaskHub.API/Program.cs`, `backend/TaskHub.API/appsettings.json` | Preserve current JWT/cookie/config work. Controller and Program overlap Phase 1; apply incremental changes. Do not edit appsettings.json or copy its values. |
| Modified auth application | `backend/TaskHub.Application/Repositories/Interfaces/IUserRepository.cs`, `backend/TaskHub.Application/Services/AuthService.cs`, `backend/TaskHub.Application/Services/TokenService.cs` | Preserve repository-based auth and existing behavior fixes. AuthService overlaps; TokenService rotation redesign belongs to Phase 2. |
| Modified auth domain/infrastructure/tests | `backend/TaskHub.Domain/Exceptions/AppException.cs`, `backend/TaskHub.Infrastructure/Repositories/UserRepository.cs`, `backend/TaskHub.Tests/Integration/JwtConfigurationTests.cs` | Reuse existing exceptions/user methods; do not overwrite these baseline changes. |
| Modified frontend | `frontend/src/api/axiosInstance.js` | Preserve current in-memory access token, withCredentials and 401 refresh exclusions, including Google exclusion. No token storage redesign. |
| Untracked auth source | `backend/TaskHub.Application/Repositories/Interfaces/IRefreshTokenRepository.cs`, `backend/TaskHub.Application/Services/JwtConfiguration.cs`, `backend/TaskHub.Infrastructure/Repositories/RefreshTokenRepository.cs` | Existing user work; use as current architecture, never recreate or delete. |
| Untracked tests | `backend/TaskHub.Tests/Integration/AuthEndpointTests.cs`, `backend/TaskHub.Tests/Services/AuthRegressionTests.cs`, `frontend/tests/authRefresh.test.js` | Preserve and extend relevant cases; changing JSON-refresh expectations is Phase 1 work, existing lifecycle coverage is not a new Phase 1 contribution. |
| Untracked documentation | `docs/ai/AUTH_CONFIGURATION.md`, `docs/ai/TASKHUB_FULL_AUDIT_AND_ROADMAP.md`, `docs/ai/TASKHUB_DEVELOPMENT_ROADMAP.md` | Read/reference only in this session. |
| Pre-existing deleted documentation | All 31 deleted files reported under `docs/ai/architecture`, `roadmap`, `rules`, `skills`, `templates` | Unrelated deletion set; do not restore, stage, replace or count it as Phase 1 work. |

Future implementation must recapture this baseline: files can change after this plan. An overlapping dirty file is not permission to revert its current contents. Preserve existing UserRepository.IsAdminAsync, JwtConfiguration validation, ServiceUnavailableException and refresh repository abstraction.

## 2. Current-code issue map

Paths below use these explicit roots: `API/` = `backend/TaskHub.API/`; `Services/`, `DTOs/`, `Validators/`, `Repositories/Interfaces/` = `backend/TaskHub.Application/`; `Repositories/` implementations = `backend/TaskHub.Infrastructure/Repositories/`; `Domain/` = `backend/TaskHub.Domain/`; `FE/` = `frontend/src/`. Classes generally match filenames; exceptions are named explicitly.

| Finding / severity | Current file, class, method / endpoint | Current behavior and risk | Expected target behavior |
|---|---|---|---|
| C07 / P0 | `Services/ProjectService.cs:GetActivityLogsAsync`, `Repositories/ProjectRepository.cs:GetProjectActivityAsync`, `API/Controllers/ProjectController.cs:GetActivity`; `GET /api/projects/{id}/activity` | GetProjectById loads tracked Owner/Members.User; log query explicitly Includes User; service returns `IEnumerable<ProjectActivityLog>` inside `ApiResponse<object>`. PasswordHash is public and not ignored. | Repository scalar projection to allowlisted activity DTO; no EF entity/navigation leaves the service boundary. Explicit response-field security tests through HTTP. |
| C07 / P0, same finding | `BoardService`, `BoardListService`, board/list repositories/controllers; board list/detail/create and project-board endpoints | Return Board/BoardList entities. Project-board lookup first loads a project with users; EF fixup can connect that graph to returned boards. Board detail/list endpoints also expose raw task/project graphs. | Reuse safe BoardResponseDto/BoardListResponseDto/TaskResponseDto and explicit summaries. Typed responses independent of tracking state, bounded versioned read contracts. |
| C03 / P1 | `DTOs/AuthResponseDtos.cs`, `Services/AuthService.cs`, `API/Controllers/authController.cs`; POST register/login/google/refresh | AuthResponseDto.RefreshToken is serialized and also set as cookie. This is confirmed JSON exposure, distinct from speculative navigation loading. | Internal AuthResult carries refresh secret to controller; public AuthResponseDto carries only Token and User; cookie flags retained. |
| C04 / P1 | `API/Program.cs`; AuthController actions | Named AuthRateLimit registered with five shared fixed-window permits but no endpoint/global attachment. | Explicit per-client endpoint policies for four auth actions; bounded independent partitions, common 429 envelope and Retry-After; no shared global five-request bucket. |
| C05 / P1 | `AuthService.RefreshTokenAsync`; TokenService.ValidateRefreshTokenAsync → RefreshTokenRepository.GetValidAsync | Repository validates token flags/expiry and Includes User; refresh never checks User.IsActive before issuing replacements. | Reject inactive user before issuance; revoke existing refresh sessions via existing service method; no new token/cookie on rejection. Atomic consumption remains Phase 2. |
| C01 / P1 | `TimeTrackingService.StartTimerAsync/CreateManualEntryAsync/GetEntriesForTaskAsync`; `TimeTrackingController`; POST `/api/timetracking/start`, `/manual`, GET `/task/{taskId}` | Start/manual check only existence; task history has no caller argument. Authenticated outsiders can access another task's time boundary. | Caller-aware PermissionService authorization, focused time repository, DTO paging and explicit errors. Stop/delete remain entry-owner operations; no new ownership checks scattered in controllers. |
| C06 / P1 | `TaskItemService.InviteMemberToTaskAsync/AcceptTaskInvitationAsync`; POST `/api/tasks/{id}/invite`, `/api/tasks/accept-invite` | Read-style legacy access can assign an existing workspace member; acceptance email check is commented; acceptance silently inserts TeamMember. | Assign-authorized inviter; recipient-bound acceptance; current inviter and recipient eligibility rechecked; never create team membership implicitly. Noneligible recipient must first use explicit project/team membership flow. |
| C46 / P1 | `TaskItemService.UpdateTaskAsync` versus `AssignTaskAsync`; PUT `/api/tasks/{id}`, PATCH `/{id}/assign` | Update allows TaskAction.Update then normalizes/changes assignee; dedicated action requires TaskAction.Assign. | Compare proposed normalized assignee before mutation; if changed, authorize Assign through PermissionService, then validate eligibility. Preserve omitted assignee instead of interpreting a title edit as unassignment. |
| C47 / P1 | `TeamService.AddTeamMemberAsync/ChangeMemberRoleAsync`; `ProjectService.InviteMemberAsync/UpdateMemberRoleAsync/AcceptInvitationAsync`; team member POST/PUT and project invite/PATCH | ManageMembers lets Manager/Admin grant Owner. Project.OwnerId can disagree with Owner membership. Old Owner invitations can grant that role on acceptance. | Explicit centralized role-transition rules. Generic add/invite/role actions cannot grant Owner; owner changes only through an owner-authorized atomic transfer. Reject legacy Owner invites outside that transfer flow; maintain owner identity. |
| C08 / P1 | `CommentController` and `TaskItemController` GET/POST `/api/tasks/{id}/comments`; `ProjectController` and `ProjectMemberController` GET members, POST invite, PATCH role, DELETE member | Route parameter names differ but patterns are equivalent; requests can be ambiguous before business logic runs. | One owner per verb/template. CommentController owns comments; ProjectMemberController owns member actions. Preserve non-colliding acceptance/delete aliases with explicit resource binding. |
| C09 / P1 | `FE/pages/projects/tabs/ProjectTasksBoard.jsx:fetchBoardData` → projectApi.getProjectBoards → ProjectController.GetProjectBoards → BoardService.GetProjectBoardsAsync → BoardRepository.GetBoardsByProjectIdAsync | Query returns boards with no Lists/Tasks Includes. Component chooses one item and reads `boardData.lists || []`; no detail request occurs. Local task/list appends can appear until reload. | One selected-board aggregate endpoint resolves optional boardId and returns safe, ordered active columns/cards. Component consumes that explicit contract, not summary navigation defaults. |
| C10 / P1 | `BoardListService.IsUserBoardOwner/GetListsAsync/CreateListAsync/UpdateListAsync/DeleteListAsync`; `/api/boardlists` | Only Board.OwnerId accepted; denied GET returns empty list, creation null, updates false. Legitimate project/team managers rejected; denial looks like empty success. | BoardAction.View for reads; BoardAction.Update for column changes via PermissionService; missing resource 404, forbidden 403. |
| C17 / P1 | `BoardRepository.GetBoardByIdAsync`, `BoardListRepository.GetListsByBoardIdAsync`, `BoardService.GetBoardAsync` | Included Tasks have no IsDeleted filter. Delete can appear undone after reload. | All Phase 1 board/list/card projections require `!IsDeleted`, including counts and new aggregate pages. Stable Position then Id ordering. |
| C24 / P2, affected boundary only | Board/BoardList/TimeTracking controllers, `TimeTrackingService`, Program, `GlobalExceptionMiddleware`; TaskItemController.AddComment | Manual error actions/null/bool contracts; anonymous time responses; generic time exceptions become 500; model/JWT/rate failures bypass normal exception response. Controller sends a test self-notification after comment creation. | Services throw existing AppException subclasses; typed ApiResponse; configure framework failures once; controller only maps/routes. Comment notification decision moves into CommentService, not duplicated across route owners. |
| C28 / P1 | `Application/Hubs/NotificationHub.cs` joins; `ProjectService.RemoveMemberAsync/UpdateMemberRoleAsync`, `TeamService.RemoveTeamMemberAsync/ChangeMemberRoleAsync` | Joins authorize once. Later changes do not invalidate existing project/team/board/task connections. A removed member can remain a group recipient. | Revalidate effective access after membership change and before protected dispatch; coordinate joins/revocations/sends. Multi-connection tests, including alternate valid membership, required. |
| C32 / P1 | `TaskItemService.UploadAttachmentAsync/GetAttachmentsAsync/DeleteAttachmentAsync`; TaskItemController attachments; Program | Extension/client MIME only; `/uploads/tasks/...` returned as FilePath/FileUrl but no serving route or static middleware. Legacy task-access predicate and delete-by-ID without parent binding. | Private storage + safe metadata; centralized access/content checks, parent-bound operations and authorized streaming design. Do not enable public wwwroot serving. Binary-response rule conflict is explicitly gated below. |
| L10 / P1 likely | `AuthService.GoogleLoginAsync`, `Domain/Entities/AppUser.cs` | Validates signature/audience, then finds account by Email; no explicit EmailVerified check or persisted provider subject. Live unsafe linkage was not reproduced. | Injectable verifier for deterministic claims tests; explicit verified-email/provider identity policy. Do not call this a proven account takeover or pretend subject binding exists. Decide schema only if verified policy needs persistence. |

### Important adjacent observations, not added issue counts

- `ProjectRepository.GetAccessibleProjectsAsync` accepts a ProjectMember row without first restricting that branch to Team projects; PermissionService rejects a non-owner Personal project even with such a row. Seed this legacy/inconsistent-data case in collection/detail parity tests.
- Task collection predicates have owner/assignee/board-owner shortcuts before project scope; removed users can retain those scalar IDs. Verify and align affected read predicates with current project permission, rather than treating assignment as a continuing grant to any project.
- `GetWorkspaceProjectsAsync` filters Private by explicit project membership while PermissionService currently permits workspace-member View regardless of Visibility. The plan preserves centralized access semantics and makes collection rules agree; it does not silently redefine “Private” to a new authorization product model.
- `TaskCard.jsx` reads `commentCount`, while TaskResponseDto and board count events use `commentsCount`. This is a concrete C09/C07 consumer contract correction, not a separate feature.

## 3. User/entity serialization trace

Program configures `ReferenceHandler.IgnoreCycles`, string enums and omission of nulls. IgnoreCycles prevents recursive loops; it does not hide PasswordHash, role/status metadata or populated user navigation collections. JWT authentication does not automatically load an AppUser into every request's DbContext. No lazy-loading behavior was established, so unloaded navigation data is not asserted to leak.

| Endpoint / response now | Actual loading and serialization path | Exposure classification | Target DTO and tests |
|---|---|---|---|
| GET `/api/projects/{id}/activity`: `ApiResponse<object>` containing `IEnumerable<ProjectActivityLog>` | ProjectService first calls GetProjectByIdAsync → project Owner/Members.User tracked; repository log query Includes User → raw logs returned | **Direct, code-confirmed path to User.PasswordHash**, plus email/role/IsActive/timestamps. Project/User relationship fixup may add broader graph. Metadata is an unreviewed JSON string and must not be blindly exported. | ProjectActivityResponseDto + compact UserSummaryDto; HTTP recursive allowlist and synthetic secret sentinels; tracked/untracked graph cases; actor distinct from owner. |
| GET `/api/projects/{id}/boards`: object containing `IEnumerable<Board>` | BoardService loads Project via GetByIdAsync with Owner and Members.User, authorizes, then queries tracked Boards | **Reachable loaded graph through fixup**: Board.Project.Owner / Project.Members.User and potentially Board.Owner. Not just a hypothetical field on an unloaded entity. | BoardSummaryDto only; full nested JSON field tests with project users and deliberately populated navigations. |
| GET `/api/boards/{id}`: object containing Board | Includes Project + Lists.Tasks; permission calls read scalar admin or memberships without loading users on normal current paths | Raw Project/Task graph definitely returned; **PasswordHash exposure is conditional, not demonstrated by this fresh request alone**. Returning raw tasks also emits internal soft-delete/scheduling fields. | BoardResponseDto with projected active BoardListResponseDto/TaskResponseDto; tests with fresh context and pretracked users to prove shape never depends on EF state. |
| GET `/api/boards`: object containing owner-scoped Boards | `Where(OwnerId == userId)` with no user Include; no preceding service auth graph load | Direct entity response confirmed; **no loaded user/hash proven on ordinary fresh request**. Board navigation shape still unsafe as a contract. Also verify project access after ownership/membership changes. | BoardSummaryDto collection; non-sensitive allowlist and collection scope tests. |
| POST `/api/boards`: object containing newly saved Board | Entity created from Name/Color/OwnerId; no user query in this service | No loaded user/hash proven; raw entity contract is unnecessary and vulnerable to later fixup changes. | BoardResponseDto/summary allowlist with empty lists, no navigation. Test creation JSON. |
| GET `/api/boardlists/board/{boardId}`: object containing BoardList entities | IsUserBoardOwner loads board/project/lists/tasks; list query Includes Tasks; shared context fixes Board/List/Task graph | Raw board/project/task graph confirmed; **user/hash conditional** because this path does not itself Include users. Deleted task exposure is definite. | Paged BoardListResponseDto canonical read, safe legacy adapter; deleted-row and tracked-user response tests. |
| POST `/api/boardlists`: object containing saved BoardList | Same owner-check graph load before adding list; response returns tracked entity | Raw Board navigation may be populated; **user/hash conditional**. | BoardListResponseDto, no Board entity, empty Tasks on new column; create response test. |
| POST `/api/auth/register`, `/login`, `/google`, `/refresh`: `ApiResponse<AuthResponseDto>` | Service explicitly copies generated refresh string into DTO; controller sets cookie then returns same DTO | **Confirmed RefreshToken JSON exposure**. User is already UserResponseDto and does not include PasswordHash. | Split internal AuthResult/public AuthResponseDto; assert JSON absence of refreshToken while Set-Cookie supplies valid cookie. |

Potential secret fields behind a populated `AppUser.RefreshTokens` navigation are RefreshToken.Token, IDs, expiry/revocation fields and User back-reference. Project/board reads do **not** explicitly Include RefreshTokens; do not report raw refresh values as definitely exposed by them. Instead preload synthetic tokens in a security regression fixture and prove DTO allowlisting prevents exposure under any tracking state.

`Id`, project/list/task IDs and owner/assignee IDs are deliberate client identifiers needed for links/actions, not secrets merely because they are GUIDs. UserSummaryDto is display-only and omits account role/active state/created timestamps/email unless the consuming contract specifically needs email. Existing member DTOs intentionally include email for authorized rosters; self-profile UserResponseDto intentionally includes Role/IsActive/CreatedAt. Do not remove those indiscriminately.

### Reviewed paths already using DTOs (do not invent entity leaks)

- `/api/auth/me` and `/api/settings/profile`: UserResponseDto. Auth user mapping does not copy PasswordHash/navigation collections.
- Project CRUD/archive/restore/conversion: ProjectResponseDto, even where controller uses `ApiResponse<object>`; project members: ProjectMemberDto.
- Team reads/mutations: TeamResponseDto, TeamDetailResponseDto, TeamMemberResponseDto; their loaded users are mapped, not returned.
- Task list/detail/create/update/status/assign/move: TaskResponseDto or TaskDetailResponseDto. Comments, attachments and activity are mapped to CommentResponseDto/AttachmentResponseDto/ActivityLogResponseDto. Their authorization or storage-path problems are separate from User serialization.
- Time responses: TimeEntryDto/TimeReportDto projections; Include(User) does not by itself expose AppUser when Select/MapToDto limits the response.
- Dashboard and `/api/v1/projects/{id}/activity`: DashboardActivityDto inside PagedResult. That active project activity tab is distinct from the unsafe legacy endpoint. Notification endpoints use NotificationDto.
- Hub presence sends only UserId/FullName/AvatarUrl; comment/task/project events use scalar payloads/DTOs. Their main Phase 1 defect is continuing recipient authorization, not raw AppUser serialization.

Controller return inspection covered all 13 controller files. No additional direct AppUser response action was found. Regression tests still scan every Phase 1 response recursively, including anonymous/event payloads. A `[JsonIgnore]` on PasswordHash alone is not the proposed fix: it would leave the rest of the entity graph contract uncontrolled.

## 4. DTO and API contract design

### Reuse before adding

`BoardResponseDto` and `BoardListResponseDto` exist but a source-wide reference search found no production use outside their definitions. They already compose TaskResponseDto and avoid user entities. Wire them into legacy detail/create responses; add ProjectId to BoardResponseDto because BoardDetailPage uses it to redirect project boards. Keep their safe array shapes for compatibility adapters. Do not return Domain Board merely to retain that property.

| DTO decision | Conceptual shape / boundary |
|---|---|
| **Split AuthResponseDto** | Public: `Token`, `User: UserResponseDto`. New internal `AuthResult` in Application Models: `Token`, `RefreshToken`, `User: UserResponseDto`. IAuthService returns AuthResult; controller sets cookie and maps public response. Internal model never passed to Ok/Created/SignalR. |
| **Reuse UserResponseDto** | Existing Id, Email, FullName, AvatarUrl, Role, IsActive, CreatedAt for authenticated self-profile only. No new duplicate self-user DTO. |
| **Create UserSummaryDto** | Id, FullName, AvatarUrl. Used by legacy activity actor/user compatibility shape; no PasswordHash, tokens, global role, IsActive or user navigation. Existing flattened task/comment/member fields remain flattened. |
| **Create ProjectActivityResponseDto** | Id, ProjectId, UserId, Action, Description, CreatedAt, `User: UserSummaryDto`. Preserve safe legacy names; exclude Project entity and unreviewed Metadata. Legacy endpoint returns bounded safe array; canonical paged activity stays the existing DashboardActivityDto contract. |
| **Expand/reuse BoardResponseDto** | Id, Name, Color, OwnerId, **ProjectId**, CreatedAt, UpdatedAt, `Lists: List<BoardListResponseDto>`. Only explicit safe fields. For legacy detail/create adapters; not a second source of entity graphs. |
| **Reuse BoardListResponseDto** | Id, Name, Position, BoardId, Color, CreatedAt, UpdatedAt, `Tasks: List<TaskResponseDto>`. Projection excludes deleted tasks and orders Position/Id. New column returns Tasks=[] explicitly. |
| **Create BoardSummaryDto** | Id, Name, Color, OwnerId, ProjectId, CreatedAt, UpdatedAt. No Lists field that can be mistaken for loaded content. Used by project/owner board collections and new Kanban header. |
| **Create ProjectKanbanResponseDto** | `Board: BoardSummaryDto?`, `Lists: PagedResult<KanbanColumnDto>`, `CanManageColumns`, `CanCreateTasks`. Empty project: Board=null, empty page. No duplicated collection of all board details. |
| **Create KanbanColumnDto** | Same scalar column metadata as BoardListResponseDto, but `Tasks: PagedResult<TaskResponseDto>`. This separate shape is necessary for explicit nested pagination while preserving existing array contracts; reuse a shared projection, not independent permission logic. |
| **Reuse TaskResponseDto** | Existing title/description/status/priority/label/position/progress/dates, list/board IDs/names, owner/assignee IDs/display names, TeamId/WorkspaceId, timestamps, CommentsCount/AttachmentsCount/IsOverdue. New Kanban projections calculate counts explicitly. Do not map missing navigations to business zero. |
| **Reuse PagedResult<T> and ApiResponse<T>** | Existing `Items, Page, PageSize, TotalItems, TotalPages`; existing `Success, Message, Data, Errors`. No competing pagination/envelope DTO. New page validators enforce positive page and pageSize limits. |
| **Reuse CommentResponseDto, ProjectMemberDto, TeamMemberResponseDto, TimeEntryDto** | Already safe. Canonical affected collection reads wrap them in PagedResult; preserve legacy scalar field names and use adapters only at legacy routes. |
| **Tighten AttachmentResponseDto** | Id, FileName, FileSize, trusted ContentType, UploadedByUserName, UploadedAt, FileUrl pointing to authorized download, CanDelete. No physical storage path. For legacy compatibility, FilePath may temporarily equal the protected API URL, never a storage key/path; canonical contract removes FilePath. |
| **Add query/transfer request DTOs** | KanbanQueryDto: optional BoardId/ListId, Page/PageSize, TaskPage/TaskPageSize. TransferOwnershipDto: TargetUserId. FluentValidation enforces IDs, positive bounds and valid transition inputs; PermissionService handles actual authority. |

### Versioning and compatibility

1. Existing unversioned routes retain supported safe scalar/array shape through DTOs where practical. Removing user navigation/hash/refresh secrets is an immediate security correction, not a deprecated field kept for compatibility.
2. New canonical collection/aggregate routes use `/api/v1/...` and paged contracts. Do not break the already-existing `/api/v1/projects/{id}/activity` DashboardActivityDto response. If implementation discovers a collision with an established v1 shape, use v2; no automatic controller-wide route alias that creates collisions.
3. Legacy collection adapters accept validated page/pageSize and delegate to the same scoped paged service, emitting only that page's safe array for older consumers. Document these transitional aliases and their paging limitation. No newly implemented canonical list returns an unbounded array. Full repository-wide collection migration remains Phase 3.
4. New scoped reads: `/api/v1/boards`, `/api/v1/projects/{id}/boards`, `/api/v1/boardlists/board/{boardId}`, `/api/v1/tasks/{taskId}/comments`, `/api/v1/projects/{projectId}/members`, `/api/v1/timetracking/task/{taskId}`. Existing activity already has its paged v1 endpoint. Update active callers listed below; do not change unrelated dashboard metrics.
5. Defaults proposed for bounded new reads: page=1; ordinary pageSize=20, maximum=100. Kanban: pageSize=20 columns (maximum 100), taskPageSize=50 per column (maximum 100). These are contract limits for review, not performance measurements. Return accurate metadata; do not silently label a partial board as complete.

## 5. Kanban end-to-end trace and selected fix

Current chain:

```text
ProjectDetailPage (id + ?boardId)
  → ProjectTasksBoard.fetchBoardData
  → projectApi.getProjectBoards(projectId)
  → GET /api/projects/{id}/boards
  → ProjectController.GetProjectBoards
  → BoardService.GetProjectBoardsAsync
  → ProjectRepository.GetByIdAsync (authorization graph)
  → BoardRepository.GetBoardsByProjectIdAsync (boards only)
  → raw Board[] → component picks requested ID or first board
  → boardData.lists || [] → empty columns after fresh request
```

`ProjectRepository.EnsureDefaultBoardStructureAsync` really creates Main Board, To Do, In Progress and Done. This is a **read-contract mismatch**, not proof of failed persistence. Live create handlers append cards/columns locally, which cannot reconstruct them after a full reload. The standalone BoardDetailPage does fetch `/boards/{id}`, but redirects project boards back into the broken project flow when it sees projectId.

### Selected approach: one project-selected aggregate request

Add `GET /api/v1/projects/{projectId}/kanban?boardId={optional}` in ProjectController, delegated to a new `IBoardService.GetProjectKanbanAsync` method. Keep board-summary endpoints summaries. Do not load every board's lists/tasks, and do not make ProjectTasksBoard issue summary + detail requests when the server can select the board directly.

Service sequence:

1. Load project authorization facts, throw NotFound if absent, and authorize ProjectAction.View before returning any board/card data.
2. If boardId is supplied, resolve it **within that project**. Invalid/cross-project/missing board returns 404 after project authorization; never silently fall back to another board. If omitted, choose the first by CreatedAt descending, then Id (matching existing newest-board fallback deterministically). An empty project returns a valid empty aggregate, not a fabricated Main Board.
3. Authorize BoardAction.View using the resolved board/project. Derive CanManageColumns from BoardAction.Update and CanCreateTasks from the same centralized task/board action policy; no role reconstruction in controller/repository/UI.
4. Repository uses no-tracking scalar projections: page columns by Position/Id; page active tasks per returned column by Position/Id; project TaskResponseDto names/counts. No Include(User) entity graph is serialized. Keep read queries sequential on a scoped DbContext.
5. New aggregate accepts validated column page/pageSize and taskPage/taskPageSize. An optional listId requests only that column's task page after checking its board ownership. This lets a column's Load more use the same endpoint without refetching every board or duplicating an initial metadata request.
6. Return ProjectKanbanResponseDto with complete paging metadata. Legacy `/boards/{id}` continues using safe BoardResponseDto. The canonical endpoint owns the richer selected/paged view; both share projection helpers and permission calls.

Frontend sequence:

1. Add `projectApi.getProjectKanban(projectId, params)` using axiosInstance and `@/` imports; replace getProjectBoards in ProjectTasksBoard's initial load.
2. Set board from `data.board`, lists from `data.lists.items`; adapt each column's `tasks.items` into the rendering list while retaining page metadata separately. Do not fetch metadata again. Render explicit no-board, zero-card, denied, not-found and load-failed states; clear the old board on project switch and guard late responses.
3. Add column/card Load more controls when metadata says more exists. Until a column is fully loaded, disable drag reorder for its partial list with an explanation; do not send position mutations based on an incomplete list. Phase 2 still owns full ordering/move invariants.
4. Use returned capabilities for column/task controls. Fix TaskCard's `commentCount` read to `commentsCount`; preserve existing TaskResponseDto field naming and string enums.
5. Include ProjectId in standalone board DTO so BoardDetailPage redirect continues to target the same board. Test invalid requested board without silent fallback.
6. Prove create → navigate away → full browser reload retrieves stored columns/cards from HTTP with hub disabled. Also prove soft-delete → reload does not resurrect a task. No reliance on localStorage, old Zustand state or live events.

This deliberately refines the roadmap's proposed “summaries then details” sequence based on the user's request to avoid redundant calls. It preserves its selected-board and safe DTO intent. Performance rework beyond bounded projections/paging is not imported from Phase 3.

## 6. Authorization contract and collection parity

### Current centralized policy to preserve or explicitly tighten

| Resource/action | Personal / standalone | Team/project roles | Required Phase 1 correction |
|---|---|---|---|
| Project View | Personal owner; current global Admin override retained | Project owner/Owner/Admin/Member/Guest or workspace membership can View | Collection predicates must not grant Personal access through stale ProjectMember rows. “Public” does not mean anonymous. |
| Board View | Standalone Board.OwnerId; project board delegates to project View | Same project View | Check project-backed owned boards against project permission, not only historical Board.OwnerId. |
| Column changes | Standalone board owner; Personal project owner | BoardAction.Update → ProjectAction.Update: owner/Admin or team Owner/Manager | Replace IsUserBoardOwner; Guest/Member cannot manage columns. Explicit Guest restriction must apply consistently if project Guest also has a workspace role; record and test precedence centrally. |
| Task View | Personal project owner; standalone task owner/assignee under existing policy | Task's actual List.Board.Project governs, then legacy TeamId fallback only when no project exists | Always load project authorization relation; `GetByIdWithDetailsAsync` currently omits `.ThenInclude(Board.Project)`, so detail checks must use GetTaskForAuthorizationAsync or an equivalent safe relation query. |
| Task Update / ChangeStatus | Existing owner/eligible standalone assignee policy | Project Member allowed; explicit project Guest modifications denied | Do not turn new authorization into a blanket owner-only UI. |
| Assignment / task invite | Personal owner normalization; invitations prohibited without collaboration scope | TaskAction.Assign → ProjectAction.Update; eligible recipient separately checked | Guard both general PUT and dedicated assignment/invitation, including clearing assignment. Omitted field must not count as explicit clear. |
| Time history/start/manual | Task View for read; Task Update for recording time, through PermissionService | Guest can read permitted time history but cannot start/manual-write | No reliance on task existence. Stopping/deleting one's entry does not grant new task access; returned title/details must obey current View. |
| Project members/invites | Personal invites remain invalid | View for roster; ManageMembers for ordinary invitations/transitions | Owner grants require distinct owner-transfer authority, not ManageMembers. Acceptance revalidates invite role and inviter's current authority. |
| Team roster/roles | Not applicable | View for roster; Owner/Manager for ordinary member management | Manager cannot add/promote self/others to Owner or alter an Owner under ordinary management. Owner self-removal remains guarded. |
| Attachments | View for list/download; Update for upload; delete permission centralized | Same task policy plus uploader/task-owner/global-admin delete rule only while task still accessible | Parent task ID must match stored attachment TaskId. Removed uploader cannot bypass task permission. |
| Hub groups/events | Same View permission as corresponding HTTP resource | Effective permission recomputed, not just “was once a member” | If team access is removed but direct project membership still grants View, project access remains valid; never evict/reject solely by label without checking policy. |

### Implementation boundaries

- Extend IPermissionService/PermissionService with role-transition/ownership-transfer and attachment/time ownership authorization helpers where existing enums cannot express the rule. Do not copy role comparisons into controller, repository, frontend or a second permission service. Capability calculation belongs to the same policy implementation.
- Express collection scope as reusable authorization criteria evaluated by repositories; PermissionService owns the semantics. Shared query criteria must be framework-light in Application/Domain contracts, with EF translation inside repositories. Tests compare collection inclusion with detail authorization for each fixture, including removed users and stale owner/assignee/Personal membership records.
- Preserve the current rule that Team project access can derive from workspace membership regardless of Visibility; align GetWorkspaceProjectsAsync to the centralized View rule. If product review instead requires private-team isolation, record it as a policy decision before changing all read/hub paths together. Do not implement one interpretation in lists and another in detail.
- C46 minimal payload fix: distinguish absent AssignedToId from explicit null (request presence flag/custom binding or a dedicated update request representation). Absent preserves assignment; explicit changed value/null invokes Assign permission. Add only this required preservation bridge now; broader StartDate/Progress/status semantics remain C15/C16 in Phase 2.
- C06 no-schema invitation policy: task invitations may assign only recipients already eligible for the task's project/workspace when accepted. An outsider must complete explicit membership invitation first. Recheck inviter's current Assign permission and recipient email/eligibility at acceptance; do not add TeamMember as a side effect or create a new task-membership model.
- C47 generic add/invite/role endpoints reject Owner as a target grant, including old pending Owner invites. Add explicit owner-authorized project/team transfer commands using existing owner/member fields in one repository transaction. Project transfer updates Project.OwnerId, old Owner membership and new Owner membership together; team transfer promotes eligible target before demoting actor in that transaction. Preserve last-owner protection. Full L06 concurrent owner-invariant hardening belongs to Phase 2; no foundation release before that test passes.
- Refactor the touched TimeTrackingService completely behind a focused repository. Move touched TaskItem/Team direct context queries to their repositories/user repository, including helpers needed by those methods. Do not expand to Settings/Audit/Notification service architecture cleanup (Phase 2). Never add new Service→DbContext access.

### C28 recipient enforcement design

The existing static BoardPresence map tracks board connections only; it is not enough to revoke task/project/team groups. Add a single-host connection/resource registry service, with user→connections and connection→joined resources, invoked by hub connect/join/leave/disconnect. Store only IDs/display-safe presence, never tokens or DbContext instances.

Add a protected realtime dispatcher service. Task/column/comment/project activity sends and hub typing/presence use this dispatcher rather than trusting a historical SignalR group membership. Resolve candidates from the registry; re-evaluate View via RealtimeAccessService/PermissionService with fresh repository state, then send only to authorized connections. Membership mutations invalidate affected resources and remove denied group/presence memberships before reporting completion.

Coordinate join/register, membership mutation/invalidation and dispatch using ordered per-user/resource synchronization; all protected send sites must participate. A check-then-send without coordination still has a revocation race. Do not hold a singleton DbContext; scoped permission evaluation uses fresh state. Define the acceptance boundary as **no protected send initiated after revocation completes**; already transmitted frames cannot be recalled. Tests must race join/send/remove and verify all tabs, including nested task/board groups. This is a single-process baseline; reconnect replay, distributed presence, durable notification delivery and broad memory tuning remain Phase 9.

## 7. Critical error contracts and route ownership

### Current affected inconsistencies

| Location | Current behavior | Exact change |
|---|---|---|
| BoardController.GetBoard/UpdateBoard | Manual NotFound with “missing or access denied”; service null/bool | Service throws NotFound for absent and Forbidden for denied. Controller returns typed success DTO only. |
| BoardController.DeleteBoard | Manual BadRequest on false | NotFound/Forbidden from service, success ApiResponse with documented empty data. |
| BoardListController create/update/delete | BadRequest/NotFound from null/false; denied GET yields 200 empty | Resolve resource, authorize and throw in service; typed DTO / success envelope. |
| TimeTrackingController all actions | Anonymous `{success,data}` or `{success,message}` | ApiResponse<TimeEntryDto>, ApiResponse<PagedResult<TimeEntryDto>>, ApiResponse<TimeReportDto> or explicit success envelope. |
| TimeTrackingService | KeyNotFoundException, InvalidOperationException, ArgumentException | Missing task/entry → 404; running/stopped state conflict → 409; invalid dates → FluentValidation 400 (or documented business validation 422), never accidental 500. |
| `[ApiController]` model validation / malformed JSON | Default framework response; no InvalidModelStateResponseFactory | Configure one ApiResponse error formatter with safe errors list; test malformed GUID/JSON, missing fields and FluentValidation failures. |
| JWT challenge/forbid / rate limiter | Program has only OnMessageReceived; rate status 429 but no body | Shared safe formatter in challenge/forbidden/OnRejected; preserve WWW-Authenticate and Retry-After where applicable. Correct middleware order if partition uses authenticated identity. |
| GetCurrentUserId helpers | Guid.Parse / Guid.Empty fallbacks vary | Validate required subject once at auth boundary or shared accessor; invalid authenticated identity produces controlled 401, never 500/Guid.Empty data access. |
| GlobalExceptionMiddleware | AppException mapped correctly; unknown errors generic 500 | Reuse it; do not expose exception text/stack for unknown errors. Keep error casing/envelope consistent with MVC; no per-controller try/catch. |
| TaskItemController.AddComment | Calls CommentService then creates test self-notification using comment content | Move any retained notification decision into CommentService; ensure exactly one dispatch with the chosen single controller action. Do not introduce broad delivery retry work here. |

No manual StatusCode/Forbid action was found in the targeted controllers beyond these patterns; do not invent occurrences. NotificationController's bare Unauthorized/NoContent is real but outside the selected HTTP response cleanup unless implementation must touch that controller; its broad inbox repair remains Phase 4/9.

Target JSON envelope (null omission may follow configured serializer):

```text
success: false
message: safe actionable message
data: null or omitted
errors: safe field/business errors or omitted
```

Status rules: 401 unauthenticated/invalid identity/token; 403 authenticated denied/inactive account; 404 absent or mismatched child resource after parent authorization; 400 malformed/field validation; 409 state/transition conflict; 422 supported business-validation cases; 429 rate limit; 503 unavailable Google configuration; 500 generic unexpected failure. Do not conflate an authorized empty page with 403/404.

### Exact route ownership

- CommentController owns GET/POST `/api/tasks/{taskId}/comments` and canonical paged GET `/api/v1/tasks/{taskId}/comments`. Remove duplicate GET/POST actions from TaskItemController. Preserve both existing delete URL forms as uniquely routed CommentController actions; parent-qualified delete verifies comment.TaskId.
- ProjectMemberController owns GET roster, POST invite, PATCH role, DELETE member under `/api/projects/{projectId}/members` plus canonical paged v1 roster. Remove those duplicate actions from ProjectController.
- Keep the currently consumed `/api/projects/{projectId}/members/accept-invite` and `/api/projects/accept-invite` aliases, each with one action. The scoped alias must verify token.ProjectId equals route projectId; both call one service operation.
- ProjectController retains project CRUD/legacy activity/board summaries, existing conversion and the new selected Kanban action. Do not blanket-add a v1 route prefix: DashboardController already owns v1 project activity.
- Test endpoint pattern equivalence after normalizing parameter names; service-only tests cannot detect ambiguous routing.

## 8. Attachment and Google decisions that need explicit review

### Private attachment streaming versus the response rule

C32 does not demonstrate that uploads are currently public: Program has neither UseStaticFiles nor a download route. It demonstrates broken retrieval and unsafe permission/content boundaries. Planned download is `GET /api/v1/tasks/{taskId}/attachments/{attachmentId}/download`, resolving a repository-owned storage key after parent/task authorization, streaming with safe Content-Disposition and trusted content type. Storage adapter canonicalizes/contains paths under a configured private root, rejects traversal and client-selected physical paths, validates allowed file signatures/text types, and preserves the existing 10 MB limit. File/DB durability cleanup remains Phase 2.

There is an explicit contract conflict to resolve before enabling this download: [AGENTS.md §3.2](../../AGENTS.md) says **“Mọi endpoint trả về ApiResponse<T>”** (every endpoint returns ApiResponse<T>), while the roadmap calls for streaming file bytes. A binary success cannot also be a JSON ApiResponse. Recommended review decision: allow a narrow authenticated binary-success exception, with metadata and every pre-stream error still using ApiResponse. This plan does not silently grant that exception. Without approval, implement the security/metadata checks and keep download explicitly unavailable; **full C32/Phase 1 exit stays open**, not falsely complete. Do not work around the rule with base64 JSON, bearer tokens in URLs or public files.

Because access tokens are in memory, a normal `<a href=fileUrl>` cannot authenticate the download. The eventual TaskAttachments consumer must request a blob through `src/api`/axiosInstance, create a short-lived object URL and revoke it; do not persist access tokens. Do not expose a file system path as FileUrl.

### Google identity verification and schema

Extract signature/audience verification behind `IGoogleIdentityVerifier` so tests supply verified claim results without calling live Google. The production implementation still uses GoogleJsonWebSignature; mocks are only in tests. Validate nonempty subject/email and EmailVerified before any email match or account creation. Test invalid audience/signature through verifier behavior and unverified/mismatched identity through AuthService.

Current schema has no Google subject/provider binding. An explicit verified-email linking policy must be reviewed; checking EmailVerified alone must not be described as persisted identity binding. If durable subject-bound linking is required to pass L10, propose a separate schema diff and migration review; do not invent columns in this phase plan. Until policy/tests establish safe supported behavior, keep the affected Google linking behavior unavailable with a controlled error, and do not mark L10 resolved merely because the token signature is valid. Email/password flow remains available. No live takeover or complete provider-safety guarantee is claimed by this read-only review.

## 9. Implementation sequence — vertical slices with tests

These are future implementation steps, not actions performed now. Keep each slice buildable; do not change a public service return type in isolation from its implementations/callers/tests. No commits are automatic.

1. **Freeze baseline and resolve review gates.** Re-read instructions/status; record current tests/lint failures in an authorized implementation session. Confirm binary download exception, Google identity policy and selected role/visibility semantics. Create fixtures for owner, outsider, project Guest/Member/Admin, team Member/Manager/Owner, global Admin and stale removed memberships. No real secrets/data.
2. **C07 safe legacy projections.** Add UserSummaryDto/ProjectActivityResponseDto/BoardSummaryDto; wire existing board/list DTOs (including ProjectId), repository projections, service interfaces and controllers together. Add recursive HTTP allowlist tests for every response in section 3; keep legacy safe shapes and existing v1 activity separate.
3. **C03–C05 auth contracts.** Split internal/public auth results and update IAuthService/AuthService/controller and existing tests in one slice. Add inactive refresh/revocation, attach partitioned auth policies and framework error formatting. Tests read refresh from Set-Cookie, never JSON. Preserve existing JWT configuration work; do not implement C02 rotation transaction here.
4. **L10 verification seam.** Add verifier and deterministic claims tests; implement the approved no-schema policy or record binding migration gate. Do not silently auto-link accounts under an unreviewed assumption.
5. **Central permission/collection contracts.** Implement policy-owned role/transfer and permission query criteria, authorization-relation loading, collection/detail parity, C10 columns and C46 presence-aware assignment guard. Add safe owner transfer commands and relevant relational tests. Generic Owner grants/old Owner invites are denied before mutation.
6. **C01/C06/C32 resource boundaries.** Add focused time/collaboration persistence interfaces; move touched direct queries behind repositories. Enforce caller/resource/recipient/parent-ID checks; add bounded time history and corrected error types. Implement attachment private storage/download only after its review gate. No implicit membership grant.
7. **C08 single route owners.** Consolidate comment/member actions with interface/controller/client updates and endpoint-routing tests in the same slice. Move controller notification decision into service; retain parent-bound non-colliding aliases.
8. **C09/C17 single-request Kanban.** Add paged aggregate query/DTO/controller method plus projectApi/ProjectTasksBoard parsing/loading/paging/capabilities in one slice. Reuse safe projection rules, active-task filter and stable order. Fix TaskCard count field and preserve standalone redirect. Add cold-load, requested-board, delete/reload and bounds regressions.
9. **C28 ongoing event authorization.** Add registry/dispatcher, route all protected event sends through it, and integrate membership invalidation. Test with real connected clients and raced joins/revocations; do not count hub method mocks as proof of transport security.
10. **Complete consumer/error alignment.** Migrate active paged comments/member/time readers, blob attachment caller and affected auth/permission error messages. Do not show empty-success on a denied load; prevent old project data during a failed/superseded request. Keep wider UI redesign, reconnect recovery and state-store consolidation out of scope.
11. **Acceptance and review.** Run section 10 tests, builds, changed-file lint, diff check and security scan; compare final diff to baseline, document API changes and migrations decision. Stop for human review. Do not proceed to Phase 2, commit, push or merge without instruction.

## 10. Exact regression test plan

Use existing xUnit/NSubstitute/FluentAssertions/WebApplicationFactory stack. Existing EF InMemory tests remain useful but cannot prove FK/transaction behavior or SQL projection translation. Seed a disposable SQL database for the new relational transfer/projection cases; no production connection or migration execution is needed during this planning session.

### Unit tests

| Existing/new test area | Required cases and assertions |
|---|---|
| BoardServiceTests; new BoardListServiceTests | Authorize before returning data; owner/admin/manager can manage columns; Guest/Member denied; 404 missing vs 403 denied; explicit Guest-plus-workspace-role precedence; DTO results rather than entity equivalence. |
| PermissionServiceTests | Personal non-owner with stale membership denied; team/project role table; Assign stronger than Update; owner transition grants only through transfer; attachment/time ownership remains centralized. |
| TaskAssignmentBusinessRulesTests | PUT same assignee succeeds with Update; changed/cleared assignee invokes Assign and denial leaves task untouched; absent AssignedToId preserves current assignment; Personal assignment normalizes owner; ineligible recipient denied. |
| New TimeTrackingAuthorizationTests | Read calls View, writes call Update, outsider/Guest-write denied; stop/delete only owned entry; missing task/entry and invalid range are controlled; no SaveChanges side effect when denied. |
| New TaskInvitationSecurityTests | Wrong recipient, expired/accepted token, deleted task, revoked inviter and ineligible recipient denied; no new TeamMember; existing eligible member assignment authorized; old Owner-style project invite cannot bypass transition rules. |
| ProjectBusinessRulesTests; new TeamRoleSecurityTests | Manager/Admin generic Owner promotion/add/invite denied; owner transfer happy path and invalid target; owner identity consistent; no removal/demotion of protected owner through ordinary manager flow. |
| AuthRegressionTests; new GoogleIdentityPolicyTests | Inactive refresh issues no JWT/refresh token and revokes sessions; verified/unverified/missing-subject/email cases; existing-account policy and missing-config error; no client-visible refresh field. |
| New AttachmentSecurityTests | View/Update/delete policy; parent-ID mismatch; unsupported/spoofed content, empty/oversized file; path containment; no client-provided path; valid authorized stream if approved. |
| New RealtimeRecipientTests | Connect/join/leave/disconnect registry; all connections for revoked user; effective alternative project membership; denied recipients excluded by dispatcher; no retained token/DbContext. |

### Repository and HTTP/transport integration tests

| Area | Exact acceptance assertions |
|---|---|
| New SensitiveResponseEndpointTests | Seed synthetic PasswordHash/refresh sentinels and owner/member/actor graphs. GET legacy activity/project boards/boards/detail/lists plus POST board/list never contains passwordHash, refreshTokens, token entity fields, raw User/Project/Board navigations or sentinel values. Also assert allowed scalar schema and IDs remain. Use fresh requests and a test-only tracking fixture; do not dump token bodies on failure. |
| Existing AuthEndpointTests | Register/login/google success/refresh JSON has Token + safe User only. Cookie supplies refresh value and remains HttpOnly/Secure/SameSite=Strict/Path=/. Existing refresh/logout/password lifecycle remains green after helper parses Set-Cookie; inactive refresh fails and no replacement cookie is emitted. |
| New AuthRateLimitEndpointTests | Each auth endpoint actually rejects excess calls with 429 + envelope/Retry-After; a different partition retains capacity; no secrets in keys/log diagnostics; normal lifecycle suite gets isolated/reset limiter state rather than globally disabling limiter tests. |
| New Phase1RouteAndErrorTests | One normalized method/template owner for comments and member routes; actual GET/POST/PATCH/DELETE reaches intended action once; both acceptance aliases and comment delete aliases work with matching parents; invalid child-parent pair denied. 401/403/404/400/409/422/429/503/500 use documented JSON status/contract; unknown 500 contains no internal exception. |
| New ProjectKanbanEndpointTests | New Personal project has default persisted columns; reload/new request returns them and stored tasks. Requested board must belong to project; absent requested ID does not fall back. Empty project vs forbidden distinguished; Position/Id ties stable; deleted cards excluded; field/count/capability and nested paging contract correct. |
| ProjectBoardRepositoryTests / new BoardProjectionRepositoryTests | SQL translation for nested projections/paged cards and count expressions; no raw entity result dependence on EF fixup; projectId preserved; page boundaries accurate; no tasks from another board/project. |
| TaskCollectionAuthorizationTests plus new Phase1AuthorizationEndpointTests | Outsider cannot view project/board/list/Kanban/time/attachment; Personal membership corruption and removed assignee/old board owner do not leak collection rows. Team membership/project role combinations agree across list/detail/time/hub. Global Admin behavior explicitly tested, not inferred. |
| New OwnershipTransferRepositoryTests | Disposable SQL rollback test leaves owner/member rows unchanged on injected failure; successful transfer updates all relevant existing fields together. Do not claim to close Phase 2 L06 race without its later concurrency suite. |
| New AttachmentEndpointTests | Authorized authenticated byte retrieval only if rule exception approved; unauthenticated/outsider/mismatched IDs receive envelopes; safe headers and no public `/uploads` fallback; stored files isolated in test temp storage and cleaned up by fixture. |
| New RealtimeRevocationIntegrationTests | Two real client connections join board/task/project/team, lose membership/role, then subsequent protected event sends exclude them. Race a join with removal and verify no post-completion send; still-authorized direct project member remains eligible. Mocked Groups.Remove calls alone do not pass this gate. |

### Frontend tests

There is currently no mounted UI test runner or test script in package.json; `frontend/tests/authRefresh.test.js` contains the existing Node interceptor tests. Phase 1 implementation needs a small mounted React test setup (Vitest + Testing Library + jsdom, installed only in the future authorized implementation session), plus real-browser manual acceptance. This is necessary infrastructure for this phase's regression tests, not postponed wholesale to Phase 11. Keep the existing Node suite separately runnable.

Proposed frontend test files:

- `frontend/tests/projectKanban.test.jsx`: mount ProjectTasksBoard with one aggregate response; assert one initial aggregate request and no summary/detail duplicate; requested board query forwarded; columns/tasks render; unmount/remount reload; empty versus 403/404/error; late project A response ignored; paging metadata/load-more/partial-reorder behavior.
- `frontend/tests/taskCardContract.test.jsx`: `commentsCount`, string status/priority and assignee name render from TaskResponseDto without navigation objects.
- `frontend/tests/phase1ApiConsumers.test.jsx`: canonical comments/member/time page unpacking, forbidden errors visible, attachment blob download/revocation (if enabled), profile/token consumers work without refreshToken JSON.
- Extend `frontend/tests/authRefresh.test.js` only if the interceptor is changed; otherwise rerun unchanged. Test 403/429 do not enter refresh loops; retain current successful single-flight/retry coverage.

### Manual verification

1. Use synthetic owner/outsider/team role accounts. Inspect response field names (not real credentials) for every section 3 route; verify 401/403 is distinct from a successful empty page.
2. Create a Personal project, confirm Main Board/default columns, create cards, navigate away and hard reload with SignalR disabled. Repeat with Team project Member/Admin and explicit boardId. Delete a card and hard reload; it stays absent.
3. Verify permitted column CRUD and denied Guest/Member column mutation; removed user cannot retrieve prior board/task/time/file even when retaining its ID.
4. Sign in, silent-refresh, reject inactive refresh, logout; observe only cookie storage for refresh. Rate rejection produces useful feedback and another test client partition still works.
5. Verify comment/member requests no longer return ambiguous-routing 500, parent-bound aliases and recipient mismatch rejection. Attempt generic Owner promotion as Manager/Admin and verify denial.
6. Keep two tabs connected, revoke effective membership, trigger protected board/task/project events and verify no post-revocation delivery. Test legitimate remaining project access separately.
7. At mobile and desktop widths, verify Kanban reload/paging/errors and retained task actions; no redesign/visual compliance claim beyond tested Phase 1 flows.

### Commands for implementation acceptance (not run in this planning session)

```text
backend:  dotnet build TaskHub.sln
backend:  dotnet test TaskHub.sln
frontend: node --test tests/authRefresh.test.js
frontend: npm.cmd run test:phase1      # add this targeted script with the UI harness
frontend: npm.cmd run build
frontend: npm.cmd exec -- eslint <exact changed JS/JSX/config files>
root:     git diff --check
```

Also run full frontend lint to record inherited versus new diagnostics; the audit's 53 errors/6 warnings are historical evidence, not a current measurement. Changed files must pass; no blanket disabling rules. Run dependency/config/secret security scans using approved project tooling with sanitized output; never print real tokens, cookies or configuration values. If the environment lacks an approved secret scanner, report that gate explicitly rather than claiming a pass. No phase completion while required regression/build/security checks are failing or unrun.

## 11. Migration impact

**Default: no EF migration for the defined core Phase 1 repairs.** Safe DTOs, JSON/auth policy changes, PermissionService rules, rate/error configuration, repository projections, paged Kanban, same-schema ownership transfer, time permissions, recipient-bound invitations, private storage abstraction and single-host realtime registry require no new EF fields/tables.

**Conditional L10 branch: to be verified.** Persisted provider/subject identity would require a separately designed schema/migration because current AppUser has no such field. This is not automatically required by fixing sensitive serialization or Kanban. Record the linking policy/verification result before deciding; never mark durable subject binding complete without storage.

Do not add rowversion, token-family/hash fields, outbox tables, active-timer index, scoped slug indexes, new task-member table or migration-history cleanup to Phase 1. Those belong to later roadmap phases or an explicitly reviewed scope change. If Google identity changes require a migration, list migrations using the correct Infrastructure/startup context and review both directories first; planning does not run or create one.

## 12. Expected implementation file scope

This is a bounded file inventory for a future implementation session, not a claim that these files were changed now. Exact new filenames below are proposed. Tests stay in the existing TaskHub.Tests project. Preserve all current dirty content in overlapping files.

### Files expected to modify — backend production

| Directory | Files | Purpose |
|---|---|---|
| `backend/TaskHub.API/Controllers/` | `authController.cs`, `BoardController.cs`, `BoardListController.cs`, `ProjectController.cs`, `ProjectMemberController.cs`, `CommentController.cs`, `TaskItemController.cs`, `TimeTrackingController.cs`, `TeamController.cs` | Typed responses/unique routes, aggregate and transfer actions, current-user/parent-bound contracts; download gated |
| `backend/TaskHub.API/` | `Program.cs` | DI, partitioned rate policies, model/JWT/rate error contracts; retain existing config hardening |
| `backend/TaskHub.API/Middleware/` | `GlobalExceptionMiddleware.cs` | Shared response formatting only if needed for consistent framework/error output; keep unknown-error redaction |
| `backend/TaskHub.Application/DTOs/` | `AuthResponseDtos.cs`, `BoardDtos.cs`, `BoardListDtos.cs`, `TaskFeatureDtos.cs`, `AppDtos.cs`, `ProjectMemberDtos.cs`, `TeamDtos.cs` | Public auth split, board/projectId, attachment contract, assignment field presence and role/transfer inputs; reuse existing task/page DTOs |
| `backend/TaskHub.Application/Services/` | `AuthService.cs`, `BoardService.cs`, `BoardListService.cs`, `ProjectService.cs`, `TaskItemService.cs`, `TeamService.cs`, `TimeTrackingService.cs`, `CommentService.cs`, `PermissionService.cs`, `RealtimeAccessService.cs` | All scoped response/permission/auth/resource/event changes |
| `backend/TaskHub.Application/Services/Interfaces/` | `IAuthService.cs`, `IBoardService.cs`, `IBoardListService.cs`, `IProjectService.cs`, `ITaskItemService.cs`, `ITeamService.cs`, `ITimeTrackingService.cs`, `ICommentService.cs`, `IPermissionService.cs`, `IRealtimeAccessService.cs` | DTO/read/query/transfer/policy/service signatures |
| `backend/TaskHub.Application/Repositories/Interfaces/` | `IBoardRepository.cs`, `IBoardListRepository.cs`, `IProjectRepository.cs`, `ITaskItemRepository.cs`, `ITeamRepository.cs`, `ICommentRepository.cs` | Projected reads, scoped criteria, parent-bound queries, atomic same-schema transfer commands |
| `backend/TaskHub.Infrastructure/Repositories/` | `BoardRepository.cs`, `BoardListRepository.cs`, `ProjectRepository.cs`, `TaskItemRepository.cs`, `TeamRepository.cs`, `CommentRepository.cs` | Matching EF queries/commands; no entity serialization contracts |
| `backend/TaskHub.Application/Hubs/` | `NotificationHub.cs` | Registry lifecycle and permission-aware protected dispatch |

No change to UserRepository/IUserRepository is expected if existing GetById/GetByEmail/IsAdmin methods suffice. Add only demonstrated missing methods without discarding baseline changes. TokenService/IRefreshTokenRepository/RefreshTokenRepository are consumed unchanged for C05 revocation; C02 redesign is excluded. ApiResponse.cs/PagedResult definitions need no replacement.

### Files expected to add — backend production

- `backend/TaskHub.Application/Models/AuthResult.cs`: internal auth result, never a public response.
- `backend/TaskHub.Application/DTOs/UserSummaryDto.cs`, `ProjectActivityResponseDto.cs`, `ProjectKanbanDtos.cs`: minimal actor/activity and aggregate/query DTOs; BoardSummaryDto can reside in existing BoardDtos.cs.
- `backend/TaskHub.Application/Validators/Phase1QueryValidators.cs`, `TimeTrackingValidators.cs`, `MembershipValidators.cs`: bounds/date/role-input validation; resource authority remains PermissionService.
- `backend/TaskHub.Application/Repositories/Interfaces/ITimeTrackingRepository.cs` and `backend/TaskHub.Infrastructure/Repositories/TimeTrackingRepository.cs`: time persistence/query boundary.
- `backend/TaskHub.Application/Repositories/Interfaces/ITaskCollaborationRepository.cs` and `backend/TaskHub.Infrastructure/Repositories/TaskCollaborationRepository.cs`: attachment/invitation queries and recipient acceptance persistence using existing tables. Existing task relation/personal-board helper persistence can go into ITaskItemRepository rather than another generic repository.
- `backend/TaskHub.Application/Services/Interfaces/IGoogleIdentityVerifier.cs` and `backend/TaskHub.Infrastructure/Identity/GoogleIdentityVerifier.cs`: production verification seam for L10 tests; implementation model only carries validated claims.
- `backend/TaskHub.Application/Services/Interfaces/IRealtimeConnectionRegistry.cs`, `IProtectedRealtimeDispatcher.cs`, and corresponding `backend/TaskHub.Application/Services/RealtimeConnectionRegistry.cs`, `ProtectedRealtimeDispatcher.cs`: single-host ID registry and recipient enforcement. No singleton EF context.
- `backend/TaskHub.Application/Services/Interfaces/IAttachmentStorage.cs` and `backend/TaskHub.Infrastructure/Storage/LocalAttachmentStorage.cs`: private path/content/stream boundary, subject to binary-success decision.

Shared permission criteria/formatter types may be placed beside existing PermissionService/API middleware; do not introduce a second RBAC policy engine. If new support files are needed, name them in the implementation diff review instead of expanding unrelated architecture.

### Files expected to modify — frontend

| Path under `frontend/` | Required reason |
|---|---|
| `src/api/projectApi.js` | Single aggregate Kanban method, paged board summaries/legacy activity mapping where still used |
| `src/pages/projects/tabs/ProjectTasksBoard.jsx` | Aggregate parsing, page state, server capabilities and explicit load/denial states |
| `src/pages/boards/BoardDetailPage.jsx` | Verify safe DTO projectId redirect and read error contract; change only required adapter/error behavior |
| `src/components/tasks/TaskCard.jsx` | Correct commentsCount field |
| `src/components/tasks/TaskFormModal.jsx` | Preserve assignment / omit unchanged field so C46 does not turn ordinary edits into unauthorized unassignment |
| `src/api/boardApi.js`, `src/api/listApi.js` | Canonical paged read adapter while retaining mutation DTO conventions |
| `src/pages/dashboard/DashboardPage.jsx` | Adapt its board-summary collection consumption only; no dashboard query rewrite |
| `src/api/projectMemberApi.js`, `src/pages/projects/ProjectDetailPage.jsx` | Paged roster and parent-bound acceptance/transfer contracts; capability presentation |
| `src/api/commentApi.js`, `src/components/tasks/TaskComments.jsx` | Canonical paged comments and explicit load errors; existing delete URL remains supported |
| `src/api/timeTrackingApi.js`, `src/stores/useTimeTrackingStore.js`, `src/components/timetracking/TimeTrackingWidget.jsx` | Page metadata, denied/error display; do not sum a page and label it a lifetime total (label page subtotal or supply authorized aggregate) |
| `src/stores/useProjectStore.js` | Existing legacy getProjectActivity consumer: adapt to safe/paged data without reintroducing raw User dependency |
| `src/api/taskApi.js`, `src/services/taskService.js`, `src/components/tasks/TaskAttachments.jsx` | If approved download is enabled, authenticated blob request + object URL cleanup; preserve task route aliases; fix missing useRef only if this component is changed |
| `src/pages/auth/LoginPage.jsx`, `src/pages/auth/RegisterPage.jsx` | Show ApiResponse message for 403/429/503; no auth UI redesign or redirect work from Phase 5 |
| `src/pages/projects/AcceptProjectInvite.jsx`, `src/pages/tasks/AcceptTaskInvite.jsx` | Surface scoped/recipient/eligibility rejection without success claims |
| `src/pages/teams/TeamDetailPage.jsx`, `src/components/projects/InviteMemberModal.jsx` | Ordinary role choices exclude Owner grants and explain explicit transfer; preserve server enforcement |
| `package.json`, `package-lock.json`, new `vitest.config.js` | Future targeted UI-test infrastructure only; no installation during planning |

AuthContext and authApi already consume only token/user; they require regression verification, not a mandatory source change for the JSON refresh removal. axiosInstance is already dirty and should remain unchanged unless a Phase 1 test demonstrates a required error-handling change. NotificationContext lifecycle/reconnect refactor remains Phase 4/9, not assumed necessary for server recipient enforcement.

### Tests expected to update/add

Update existing files under `backend/TaskHub.Tests/`:

- `Integration/AuthEndpointTests.cs` (untracked baseline), `Services/AuthRegressionTests.cs` (untracked baseline).
- `Services/BoardServiceTests.cs`, `PermissionServiceTests.cs`, `ProjectBusinessRulesTests.cs`, `TaskAssignmentBusinessRulesTests.cs`, `RealtimeAuthorizationTests.cs`.
- `Repositories/ProjectBoardRepositoryTests.cs`, `TaskCollectionAuthorizationTests.cs`, `TaskItemRepositoryTests.cs` as signatures/authorization relations change.

Add the nine unit suites, nine HTTP/transport suites and two repository suites named in section 10 (existing suites are expanded rather than duplicated):

- Services: `BoardListServiceTests.cs`, `TimeTrackingAuthorizationTests.cs`, `TaskInvitationSecurityTests.cs`, `TeamRoleSecurityTests.cs`, `GoogleIdentityPolicyTests.cs`, `AttachmentSecurityTests.cs`, `RealtimeRecipientTests.cs` (seven new files; other unit areas extend existing files).
- Integration: `SensitiveResponseEndpointTests.cs`, `AuthRateLimitEndpointTests.cs`, `Phase1RouteAndErrorTests.cs`, `ProjectKanbanEndpointTests.cs`, `Phase1AuthorizationEndpointTests.cs`, `AttachmentEndpointTests.cs`, `RealtimeRevocationIntegrationTests.cs` (seven new files; auth lifecycle extends existing file).
- Repositories: `BoardProjectionRepositoryTests.cs`, `OwnershipTransferRepositoryTests.cs` (two new files).
- Test support: `Fixtures/Phase1ApiFactory.cs`, `Fixtures/Phase1SqlFixture.cs`, `Fixtures/Phase1SecurityData.cs` for isolated test identities, SQL and redacted synthetic data. If a .NET SignalR client package is necessary for real hub integration, change `TaskHub.Tests.csproj` only in the authorized implementation session; do not substitute mocked delivery for integration evidence.
- Frontend: three new test files named in section 10 plus `frontend/tests/setup.js`; retain existing authRefresh.test.js. Browser manual steps are explicit until a browser runner is added; do not label Node or jsdom as a real browser.

### Files that must not be touched by this Phase 1 implementation without a reviewed scope change

- `backend/TaskHub.API/appsettings.json`, any `.env`, user-secrets or live credentials; current baseline config is not an output artifact.
- `backend/TaskHub.Infrastructure/Data/Migrations/**`, `backend/TaskHub.Infrastructure/Migrations/**`, `AppDbContextModelSnapshot.cs` and `Data/AppDbContext.cs` schema mapping by default. L10 schema decision requires separate review, not automatic permission.
- Existing `JwtConfiguration.cs`, `JwtConfigurationTests.cs`, TokenService/refresh repository work unless a demonstrated Phase 1 integration need requires a narrowly reviewed extension; no atomic rotation implementation here.
- DashboardRepository/AnalyticsService query optimizations, calendar/runtime/store redesign, ProfilePage Settings persistence, templates/search/automation/bulk/trash and unrelated design-system/theme files.
- Pre-existing deleted docs, legacy refactor scripts, component generators and unused scaffolds; do not execute cleanup helpers.
- `.git` refs/config, backup refs, stash objects; no reset/restore/stash/commit/push/merge as part of planning or automatic phase completion.
- The audit/roadmap files during this planning session. Future API documentation changes should be explicitly included in the implementation review; this session's sole write is PHASE_1_IMPLEMENTATION_PLAN.md.

## 13. Phase 1 exit criteria and review record

All must pass before marking Phase 1 complete:

1. Every section 3 affected JSON response contains only approved DTO fields; recursive HTTP regressions prove absence of PasswordHash, refresh-token data, raw user navigation and storage paths, including populated tracking graphs. Public auth JSON has no refresh secret; safe self-profile/member fields remain intentionally available.
2. Affected controllers/services no longer return Board, BoardList, ProjectActivityLog, AppUser or other EF entities as responses. No `ApiResponse<object>` hides an entity. Protected binary file success is enabled only with the explicit response-rule decision; its metadata/errors remain DTO/envelope based.
3. PermissionService governs resource/actions; outsider/Personal/team/Guest/Manager/Admin/owner cases and collection/detail parity pass. Assignment changes cannot bypass Assign; invitations bind recipient and cannot grant implicit workspace/Owner privileges.
4. Unique route/HTTP tests pass for comments and project members, including parent-bound aliases. Framework/model/JWT/rate errors and business errors carry expected status/envelope; no unexpected exception detail is exposed.
5. One initial Kanban aggregate call returns required ordered columns/cards, respects requested board/project, excludes deleted tasks and describes pagination explicitly. Full reload with realtime disabled reproduces persisted data. Frontend uses matching DTO names and does not mistake load denial/failure for empty content.
6. C28 tests prove no new protected send after revocation completes across all connected tabs; remaining legitimate access is handled correctly. This does not claim Phase 9 reconnect/replay completeness.
7. Rate controls and inactive-refresh tests pass. L10 has a recorded verified policy and test outcome; any unresolved provider binding requirement is reported as a blocker, not silently deferred while Phase 1 is called complete.
8. C32 permission/content/storage/download acceptance passes under the approved response contract. Without the binary exception decision, secure interim unavailability is allowed as partial work but does not satisfy full Phase 1 completion.
9. Backend build/tests, frontend build, targeted frontend regressions, changed-file lint, `git diff --check` and applicable security scans pass. Existing full-lint debt is separately reported, no new diagnostics introduced; no fabricated test/build result.
10. No secrets, unexplained migration or unrelated edits introduced. Final diff is reconciled against the dirty baseline. Human review receives exact files/contracts/tests, unresolved gates and Phase 2 release blockers. No commit until approval, no automatic push/merge.

Planning completion record: all 16 Phase 1 audit entries are mapped to current classes/routes, response paths are traced with confirmed versus conditional exposure separated, one-request Kanban contract is selected, DTO reuse and versioning are explicit, permission/error/test/file scope is specified, and default no-migration plus conditional Google/schema and binary-contract decisions are disclosed. No application source was modified in preparing this plan.

## 14. Implemented Phase 1 review — 2026-09-24

**PHASE 1 STATUS: PASS (automated gate).** No remaining Phase 1 code finding is known from the completed checks. This does not certify production deployment or the manual browser checks below.

Branch: `feature/project-collaboration-dashboard-publish`. Existing auth remediation, unfinished frontend refactors, GSAP skill installation, and 31 unrelated documentation deletions were preserved. Nothing was staged, committed, pushed, reset, restored, cleaned or migrated. Backup refs and stashes were not touched.

### Finding results

| Finding | Implemented behavior and evidence |
|---|---|
| C01 | PermissionService authorizes timer start/manual writes and paged task history. Stop/delete retain entry-owner checks and current resource access. HTTP allowed/denied tests pass. |
| C03 | AuthResult stays internal; register/login/Google/refresh JSON exposes only token/user. HTTP tests check cookie flags and absence of the actual refresh secret. |
| C04 | All four auth actions have effective IP/action partitions, normalized path casing/trailing slash, controlled 429 and Retry-After. HTTP tests verify separate clients and actions. |
| C05 | Inactive refresh revokes sessions, returns 403, and sets no replacement cookie. |
| C06 | Inviter Assign permission, recipient email binding, active/current eligibility and inviter recheck; no implicit team membership. Valid/wrong/removed/inactive/expired/invalid cases pass. |
| C07 | Explicit board, column, task, activity and auth DTOs replace raw entity responses. Recursive HTTP checks exclude sensitive fields and private storage paths. |
| C08 | One verb/template owner; route descriptor uniqueness plus real comment/member HTTP operations pass. Parent-bound delete/accept adapters remain intentional. |
| C09 | One selected-board Kanban aggregate returns metadata, paged columns/cards and capabilities. Frontend remount, stale-response, paging and forbidden-state tests pass. |
| C10 | Column reads use BoardAction.View; mutations use Update. Manager allowed, member write/outsider denied, missing resource distinguished. |
| C17 | Repository projections and legacy DTO mappings exclude deleted tasks, count active comments and order Position then Id. Reload and paging tests pass. |
| C24 | Touched endpoints use ApiResponse/AppException, including framework auth/validation/rate errors. Membership enums use FluentValidation. Binary download success is the explicitly authorized exception. |
| C28 | Protected groups recheck current access for sends; both typing events use the protected dispatcher. Real SignalR clients over TestServer long polling prove two connections lose delivery after membership removal, alternate access remains valid, and reconnect join is denied. |
| C32 | Private storage, validated extension/content, 10 MB limit, safe metadata, parent-bound authorized streaming and deletion. HTTP tests exercise bytes, headers, unauthorized access, two existing parents, invalid/empty/oversized uploads and deletion. |
| C46 | Omitted assignment preserved; changes require Assign and eligible active recipients, including legacy team tasks. Title edits cannot clear assignment. |
| C47 | Generic Owner grants and legacy Owner invites denied; explicit owner-authorized transfer preserves Project.OwnerId and one owner membership sequentially. Undefined roles rejected. |
| L10 | Verified subject/email required; automatic email identity limited to Google-authoritative Gmail or matching hosted domain. External-email implicit binding denied. No ClientSecret or persisted provider subject needed for this restricted policy; see AUTH_CONFIGURATION.md. |

### Contracts added or completed

- `GET /api/v1/projects/{id}/kanban`: selected safe board, paged ordered columns/cards, explicit capabilities.
- `GET /api/v1/boards`, `GET /api/v1/projects/{id}/boards`: paged BoardSummaryDto.
- `GET /api/v1/boardlists/board/{boardId}`: paged KanbanColumnDto with independently paged tasks.
- `GET /api/v1/tasks/{taskId}/comments`, `GET /api/v1/projects/{projectId}/members`: safe paged DTOs.
- Unversioned collection adapters retain safe arrays for one validated page. Active frontend adapters follow canonical page metadata; the initial Kanban read remains one aggregate request.
- Existing canonical project activity remains unchanged; legacy activity remains a bounded safe DTO array.
- `POST /api/v1/projects/{id}/transfer-ownership` and `POST /api/v1/teams/{id}/transfer-ownership`: explicit transfer.
- `GET /api/v1/tasks/{id}/attachments/{attachmentId}/download`: authorized File stream, attachment disposition, no-store, nosniff. Metadata FileUrl (and transitional FilePath alias) is an API URL, never a storage key. Client downloads through bearer-authenticated Axios and a temporary blob URL.
- Existing auth, task assignment/invitation, time-entry, column mutation and member mutation routes are secured without introducing duplicate route owners.
- DTOs in the complete working tree: public AuthResponseDto/internal AuthResult; BoardSummaryDto, BoardResponseDto, BoardListResponseDto, ProjectKanbanResponseDto, KanbanColumnDto, PageQueryDto/KanbanQueryDto, ProjectActivityResponseDto/UserSummaryDto, TransferOwnershipDto; existing TaskResponseDto, comment/member/time/attachment DTOs reused. AttachmentDownload is an internal streaming result.

### Recovery and validation evidence

The requested initial Debug attachment/authorization filter passed 16/16. No local TaskHub.API process remained at recovery, so none was killed and Debug was retained. The earlier session's MSB3027/MSB3021 output lock is no longer present. Targeted expanded security tests passed 30/30; realtime/Google policy tests passed 7/7; contract/security tests passed 17/17. These overlapping runs are not added together.

The frontend test harness initially returned a mock function from beforeEach, which Vitest treated as cleanup; corrected the hook to return nothing. Changed-file lint caught two redundant catch blocks after removing Axios error-object logging; removed them. One attachment EOF whitespace issue and one xUnit assertion-style warning were fixed. None remains failing.

| Command/check | Final result |
|---|---|
| `dotnet build TaskHub.sln` (Debug) | PASS, 0 warnings, 0 errors |
| `dotnet test TaskHub.sln` (Debug) | PASS, 160/160, 0 skipped |
| `npx vitest run` | PASS, 8/8 across two frontend contract files |
| `node --test tests/authRefresh.test.js` | PASS, 5/5 |
| `npm run build` | PASS; existing bundle-size warning (>500 kB) remains |
| Changed Phase 1 frontend source/test/config ESLint | PASS, 0 diagnostics |
| Full `npm run lint` | 7 pre-existing errors, 0 warnings; baseline was 8 errors |
| `git diff --check` | PASS after EOF correction |
| Tracked plus untracked source pattern scan | PASS after review: 376 files, 13 candidates were random test-value expressions and four localization labels; no credential material found; values never printed |
| Migration/source safety | No migration or schema change; protected baseline deletions retained; no unrelated source edits from this completion work |

Full-lint debt left untouched: Navbar.jsx:93 undefined idx and :135 unused idx; CreateTaskModal.jsx:35 parse error; TaskActivity.jsx:23 parse error; TaskDetailModal.jsx:28 parse error; TaskKanbanBoard.jsx:42 unused err; TaskContext.jsx:54 set-state-in-effect. TaskAttachments' pre-existing parse error was corrected because that component required the private-download contract update.

### Manual verification required and deferred work

- Real Google sign-in with the configured OAuth client/origin.
- Browser cookie behavior over the deployment's HTTPS/origin configuration. Automated tests verify Set-Cookie headers, not a browser cookie jar.
- Physical save/open attachment UX from the running application. Component/API tests verify the authenticated blob and streamed content; they do not operate a native save dialog.
- Live WebSocket/multi-host SignalR behavior. Actual multi-connection delivery is automated using TestServer long polling; the protected registry is currently single-host.
- SQL Server runtime/query execution remains a deployment check: HTTP tests use EF InMemory. No live database was modified.
- Old attachment records containing legacy filesystem keys deliberately do not fall back to public serving; any existing deployment data relocation needs an explicit operational plan.
- Phase 2+: C02 atomic refresh consumption, transaction/concurrency enforcement for owner transfer/invitation/ordering, wider reliability/performance work, distributed realtime coordination, repository-wide collection migration, and frontend lint/bundle debt. None was started here.

### Exact completion-work file manifest

This manifest identifies incremental completion edits by comparison with the preserved starting-tree hashes; it is not a claim that every dirty file below was authored in this session. The pre-existing skill-lock change belongs to the separate requested skill installation and is excluded from Phase 1.

- `backend/TaskHub.API/Controllers/BoardController.cs`
- `backend/TaskHub.API/Controllers/BoardListController.cs`
- `backend/TaskHub.API/Controllers/CollaborationReadController.cs`
- `backend/TaskHub.API/Controllers/CommentController.cs`
- `backend/TaskHub.API/Controllers/ProjectController.cs`
- `backend/TaskHub.API/Controllers/ProjectMemberController.cs`
- `backend/TaskHub.API/Controllers/TaskItemController.cs`
- `backend/TaskHub.API/Program.cs`
- `backend/TaskHub.Application/Hubs/NotificationHub.cs`
- `backend/TaskHub.Application/Models/AttachmentDownload.cs`
- `backend/TaskHub.Application/Repositories/Interfaces/IBoardReadRepository.cs`
- `backend/TaskHub.Application/Repositories/Interfaces/ICollaborationReadRepository.cs`
- `backend/TaskHub.Application/Services/CollaborationReadService.cs`
- `backend/TaskHub.Application/Services/Interfaces/ICollaborationReadService.cs`
- `backend/TaskHub.Application/Services/Interfaces/ITaskItemService.cs`
- `backend/TaskHub.Application/Services/TaskItemService.cs`
- `backend/TaskHub.Application/Validators/MembershipValidators.cs`
- `backend/TaskHub.Infrastructure/Repositories/BoardReadRepository.cs`
- `backend/TaskHub.Infrastructure/Repositories/CollaborationReadRepository.cs`
- `backend/TaskHub.Infrastructure/Repositories/ProjectRepository.cs`
- `backend/TaskHub.Tests/Fixtures/Phase1ApiFactory.cs`
- `backend/TaskHub.Tests/Integration/AttachmentEndpointTests.cs`
- `backend/TaskHub.Tests/Integration/AuthEndpointTests.cs`
- `backend/TaskHub.Tests/Integration/Phase1AuthorizationEndpointTests.cs`
- `backend/TaskHub.Tests/Integration/Phase1ContractEndpointTests.cs`
- `backend/TaskHub.Tests/Integration/RealtimeRevocationIntegrationTests.cs`
- `backend/TaskHub.Tests/Services/GoogleIdentityPolicyTests.cs`
- `docs/ai/AUTH_CONFIGURATION.md`
- `docs/ai/PHASE_1_IMPLEMENTATION_PLAN.md`
- `frontend/src/api/attachmentApi.js`
- `frontend/src/api/boardApi.js`
- `frontend/src/api/commentApi.js`
- `frontend/src/api/listApi.js`
- `frontend/src/api/pagedCollection.js`
- `frontend/src/api/projectApi.js`
- `frontend/src/api/projectMemberApi.js`
- `frontend/src/components/tasks/TaskAttachments.jsx`
- `frontend/src/components/timetracking/TimeTrackingWidget.jsx`
- `frontend/src/pages/projects/tabs/ProjectTasksBoard.jsx`
- `frontend/src/services/taskService.js`
- `frontend/src/stores/useTimeTrackingStore.js`
- `frontend/tests/authRefresh.test.js`
- `frontend/tests/pagedCollection.test.jsx`
- `frontend/tests/phase1Contracts.test.jsx`
- `frontend/vitest.config.js`

### Final git status --short

All other dirty files shown here are preserved prior Phase 1/auth/user work or unrelated documentation deletions. No files are staged.

```text
 M backend/TaskHub.API/Controllers/BoardController.cs
 M backend/TaskHub.API/Controllers/BoardListController.cs
 M backend/TaskHub.API/Controllers/CommentController.cs
 M backend/TaskHub.API/Controllers/ProjectController.cs
 M backend/TaskHub.API/Controllers/ProjectMemberController.cs
 M backend/TaskHub.API/Controllers/TaskItemController.cs
 M backend/TaskHub.API/Controllers/TeamController.cs
 M backend/TaskHub.API/Controllers/TimeTrackingController.cs
 M backend/TaskHub.API/Controllers/authController.cs
 M backend/TaskHub.API/Program.cs
 M backend/TaskHub.API/appsettings.json
 M backend/TaskHub.Application/DTOs/AppDtos.cs
 M backend/TaskHub.Application/DTOs/AuthResponseDtos.cs
 M backend/TaskHub.Application/DTOs/BoardDtos.cs
 M backend/TaskHub.Application/Hubs/NotificationHub.cs
 M backend/TaskHub.Application/Repositories/Interfaces/IProjectRepository.cs
 M backend/TaskHub.Application/Repositories/Interfaces/ITeamRepository.cs
 M backend/TaskHub.Application/Repositories/Interfaces/IUserRepository.cs
 M backend/TaskHub.Application/Services/AuthService.cs
 M backend/TaskHub.Application/Services/BoardListService.cs
 M backend/TaskHub.Application/Services/BoardService.cs
 M backend/TaskHub.Application/Services/CommentService.cs
 M backend/TaskHub.Application/Services/Interfaces/IAuthService.cs
 M backend/TaskHub.Application/Services/Interfaces/IBoardListService.cs
 M backend/TaskHub.Application/Services/Interfaces/IBoardService.cs
 M backend/TaskHub.Application/Services/Interfaces/ICommentService.cs
 M backend/TaskHub.Application/Services/Interfaces/IPermissionService.cs
 M backend/TaskHub.Application/Services/Interfaces/IProjectService.cs
 M backend/TaskHub.Application/Services/Interfaces/ITaskItemService.cs
 M backend/TaskHub.Application/Services/Interfaces/ITeamService.cs
 M backend/TaskHub.Application/Services/Interfaces/ITimeTrackingService.cs
 M backend/TaskHub.Application/Services/PermissionService.cs
 M backend/TaskHub.Application/Services/ProjectService.cs
 M backend/TaskHub.Application/Services/TaskItemService.cs
 M backend/TaskHub.Application/Services/TeamService.cs
 M backend/TaskHub.Application/Services/TimeTrackingService.cs
 M backend/TaskHub.Application/Services/TokenService.cs
 M backend/TaskHub.Domain/Exceptions/AppException.cs
 M backend/TaskHub.Infrastructure/Repositories/BoardListRepository.cs
 M backend/TaskHub.Infrastructure/Repositories/BoardRepository.cs
 M backend/TaskHub.Infrastructure/Repositories/ProjectRepository.cs
 M backend/TaskHub.Infrastructure/Repositories/TaskItemRepository.cs
 M backend/TaskHub.Infrastructure/Repositories/TeamRepository.cs
 M backend/TaskHub.Infrastructure/Repositories/UserRepository.cs
 M backend/TaskHub.Tests/Integration/DashboardAnalyticsEndpointTests.cs
 M backend/TaskHub.Tests/Integration/JwtConfigurationTests.cs
 M backend/TaskHub.Tests/Services/BoardServiceTests.cs
 M backend/TaskHub.Tests/Services/ProjectBusinessRulesTests.cs
 M backend/TaskHub.Tests/Services/RealtimeAuthorizationTests.cs
 M backend/TaskHub.Tests/Services/TaskAssignmentBusinessRulesTests.cs
 M backend/TaskHub.Tests/TaskHub.Tests.csproj
 D docs/ai/architecture/backendplan.md
 D docs/ai/architecture/database_schema.md
 D docs/ai/architecture/frontendplan.md
 D docs/ai/roadmap/backendplan1.md
 D docs/ai/roadmap/milestones.md
 D docs/ai/roadmap/phase2.md
 D "docs/ai/rules/Logging Rules.md"
 D "docs/ai/rules/Security Rules.md"
 D "docs/ai/rules/Validation Rules.md"
 D docs/ai/rules/api_rules.md
 D docs/ai/rules/auth_rules.md
 D docs/ai/rules/backend_rules.md
 D docs/ai/rules/database_rules.md
 D docs/ai/rules/exception_rules.md
 D docs/ai/rules/frontend_rule.md
 D "docs/ai/rules/naming Convention_rules.md"
 D docs/ai/rules/repose_rule.md
 D docs/ai/rules/testing_rules.md
 D docs/ai/skills/skill_Auth.md
 D docs/ai/skills/skill_Error.md
 D docs/ai/skills/skill_Logging.md
 D docs/ai/skills/skill_Migration.md
 D docs/ai/skills/skill_Naming.md
 D docs/ai/skills/skill_Repository.md
 D docs/ai/skills/skill_Response.md
 D docs/ai/skills/skill_Security.md
 D docs/ai/skills/skill_ServicePattern.md
 D docs/ai/skills/skill_Testing.md
 D docs/ai/skills/skill_Validation.md
 D docs/ai/templates/ruletemplate.txt
 D docs/ai/templates/skilltemplate.txt
 M frontend/package-lock.json
 M frontend/package.json
 M frontend/src/api/axiosInstance.js
 M frontend/src/api/boardApi.js
 M frontend/src/api/commentApi.js
 M frontend/src/api/listApi.js
 M frontend/src/api/projectApi.js
 M frontend/src/api/projectMemberApi.js
 M frontend/src/api/timeTrackingApi.js
 M frontend/src/components/landing/WorkflowSection.jsx
 M frontend/src/components/layout/Navbar.jsx
 M frontend/src/components/layout/Topbar.jsx
 M frontend/src/components/projects/CreateProjectModal.jsx
 M frontend/src/components/projects/EditProjectModal.jsx
 M frontend/src/components/tasks/CalendarTaskModal.jsx
 M frontend/src/components/tasks/CreateTaskModal.jsx
 M frontend/src/components/tasks/EmptyState.jsx
 M frontend/src/components/tasks/StatusWorkflow.jsx
 M frontend/src/components/tasks/TaskActivity.jsx
 M frontend/src/components/tasks/TaskAttachments.jsx
 M frontend/src/components/tasks/TaskCard.jsx
 M frontend/src/components/tasks/TaskComments.jsx
 M frontend/src/components/tasks/TaskDetailDrawer.jsx
 M frontend/src/components/tasks/TaskDetailModal.jsx
 M frontend/src/components/tasks/TaskFilters.jsx
 M frontend/src/components/tasks/TaskFormModal.jsx
 M frontend/src/components/tasks/TaskKanbanBoard.jsx
 M frontend/src/components/tasks/TaskList.jsx
 M frontend/src/components/tasks/TaskListHeader.jsx
 M frontend/src/components/tasks/TaskModal.jsx
 M frontend/src/components/tasks/TaskTable.jsx
 M frontend/src/components/theme/ThemeToggle.jsx
 M frontend/src/components/timetracking/TimeTrackingWidget.jsx
 M frontend/src/context/AuthContext.jsx
 M frontend/src/context/NotificationContext.jsx
 M frontend/src/context/TaskContext.jsx
 M frontend/src/context/ThemeContext.jsx
 M frontend/src/pages/auth/LoginPage.jsx
 M frontend/src/pages/auth/RegisterPage.jsx
 M frontend/src/pages/calendar/CalendarPage.jsx
 M frontend/src/pages/dashboard/DashboardPage.jsx
 M frontend/src/pages/notifications/NotificationsPage.jsx
 M frontend/src/pages/profile/ProfilePage.jsx
 M frontend/src/pages/projects/AcceptProjectInvite.jsx
 M frontend/src/pages/projects/ProjectDetailPage.jsx
 M frontend/src/pages/projects/ProjectsPage.jsx
 M frontend/src/pages/projects/tabs/ProjectSettingsTab.jsx
 M frontend/src/pages/projects/tabs/ProjectTasksBoard.jsx
 M frontend/src/pages/tasks/AcceptTaskInvite.jsx
 M frontend/src/pages/tasks/MyTasksPage.jsx
 M frontend/src/pages/teams/TeamDetailPage.jsx
 M frontend/src/pages/teams/TeamsPage.jsx
 M frontend/src/routes/PrivateRoute.jsx
 M frontend/src/services/taskService.js
 M frontend/src/stores/useCalendarStore.js
 M frontend/src/stores/useTimeTrackingStore.js
?? .agents/
?? backend/TaskHub.API/Controllers/CollaborationReadController.cs
?? backend/TaskHub.Application/DTOs/ProjectKanbanDtos.cs
?? backend/TaskHub.Application/Models/
?? backend/TaskHub.Application/Repositories/Interfaces/IBoardReadRepository.cs
?? backend/TaskHub.Application/Repositories/Interfaces/ICollaborationReadRepository.cs
?? backend/TaskHub.Application/Repositories/Interfaces/IRefreshTokenRepository.cs
?? backend/TaskHub.Application/Repositories/Interfaces/ITaskCollaborationRepository.cs
?? backend/TaskHub.Application/Repositories/Interfaces/ITimeTrackingRepository.cs
?? backend/TaskHub.Application/Services/BoardMapping.cs
?? backend/TaskHub.Application/Services/CollaborationReadService.cs
?? backend/TaskHub.Application/Services/Interfaces/IAttachmentStorage.cs
?? backend/TaskHub.Application/Services/Interfaces/ICollaborationReadService.cs
?? backend/TaskHub.Application/Services/Interfaces/IGoogleIdentityVerifier.cs
?? backend/TaskHub.Application/Services/Interfaces/IProtectedHubContext.cs
?? backend/TaskHub.Application/Services/JwtConfiguration.cs
?? backend/TaskHub.Application/Services/ProtectedHubContext.cs
?? backend/TaskHub.Application/Services/ResourceVisibility.cs
?? backend/TaskHub.Application/Validators/MembershipValidators.cs
?? backend/TaskHub.Application/Validators/Phase1QueryValidators.cs
?? backend/TaskHub.Application/Validators/TimeTrackingValidators.cs
?? backend/TaskHub.Infrastructure/Identity/
?? backend/TaskHub.Infrastructure/Repositories/BoardReadRepository.cs
?? backend/TaskHub.Infrastructure/Repositories/CollaborationReadRepository.cs
?? backend/TaskHub.Infrastructure/Repositories/RefreshTokenRepository.cs
?? backend/TaskHub.Infrastructure/Repositories/TaskCollaborationRepository.cs
?? backend/TaskHub.Infrastructure/Repositories/TimeTrackingRepository.cs
?? backend/TaskHub.Infrastructure/Storage/
?? backend/TaskHub.Tests/Fixtures/
?? backend/TaskHub.Tests/Integration/AttachmentEndpointTests.cs
?? backend/TaskHub.Tests/Integration/AuthEndpointTests.cs
?? backend/TaskHub.Tests/Integration/Phase1AuthorizationEndpointTests.cs
?? backend/TaskHub.Tests/Integration/Phase1ContractEndpointTests.cs
?? backend/TaskHub.Tests/Integration/Phase1SecurityEndpointTests.cs
?? backend/TaskHub.Tests/Integration/RealtimeRevocationIntegrationTests.cs
?? backend/TaskHub.Tests/Services/AuthRegressionTests.cs
?? backend/TaskHub.Tests/Services/GoogleIdentityPolicyTests.cs
?? docs/ai/.phase1-task-context-check.txt
?? docs/ai/AUTH_CONFIGURATION.md
?? docs/ai/PHASE_1_IMPLEMENTATION_PLAN.md
?? docs/ai/TASKHUB_DEVELOPMENT_ROADMAP.md
?? docs/ai/TASKHUB_FULL_AUDIT_AND_ROADMAP.md
?? frontend/src/api/attachmentApi.js
?? frontend/src/api/pagedCollection.js
?? frontend/src/context/authState.js
?? frontend/src/context/taskState.js
?? frontend/src/context/themeState.js
?? frontend/tests/
?? frontend/vitest.config.js
?? skills-lock.json
```

## 15. Final commit review

The user authorized one commit on 2026-09-24 with message `feat: complete security and api contract hardening`. No push is authorized. The earlier no-commit statements record the implementation-stage restriction.

An isolated export of the staged index (excluding unrelated local changes) passed Debug build with 0 warnings/errors, 160 backend tests, 8 frontend contract tests, 5 auth tests, frontend build, and changed-file lint. The existing bundle-size warning remains. Staged whitespace checks passed. A redacted scan of all 111 staged files found only nine generated test-value expression candidates; no credential material was found. No migration or documentation deletion is included.

Included files: 88 backend, 21 frontend, 2 documentation. Unrelated frontend context/refactor work, GSAP skills/lockfile, broad audit/roadmap drafts and all 31 prior documentation deletions are excluded.

### Exact staged file set

- `backend/TaskHub.API/Controllers/BoardController.cs`
- `backend/TaskHub.API/Controllers/BoardListController.cs`
- `backend/TaskHub.API/Controllers/CollaborationReadController.cs`
- `backend/TaskHub.API/Controllers/CommentController.cs`
- `backend/TaskHub.API/Controllers/ProjectController.cs`
- `backend/TaskHub.API/Controllers/ProjectMemberController.cs`
- `backend/TaskHub.API/Controllers/TaskItemController.cs`
- `backend/TaskHub.API/Controllers/TeamController.cs`
- `backend/TaskHub.API/Controllers/TimeTrackingController.cs`
- `backend/TaskHub.API/Controllers/authController.cs`
- `backend/TaskHub.API/Program.cs`
- `backend/TaskHub.API/appsettings.json`
- `backend/TaskHub.Application/DTOs/AppDtos.cs`
- `backend/TaskHub.Application/DTOs/AuthResponseDtos.cs`
- `backend/TaskHub.Application/DTOs/BoardDtos.cs`
- `backend/TaskHub.Application/DTOs/ProjectKanbanDtos.cs`
- `backend/TaskHub.Application/Hubs/NotificationHub.cs`
- `backend/TaskHub.Application/Models/AttachmentDownload.cs`
- `backend/TaskHub.Application/Models/AuthResult.cs`
- `backend/TaskHub.Application/Repositories/Interfaces/IBoardReadRepository.cs`
- `backend/TaskHub.Application/Repositories/Interfaces/ICollaborationReadRepository.cs`
- `backend/TaskHub.Application/Repositories/Interfaces/IProjectRepository.cs`
- `backend/TaskHub.Application/Repositories/Interfaces/IRefreshTokenRepository.cs`
- `backend/TaskHub.Application/Repositories/Interfaces/ITaskCollaborationRepository.cs`
- `backend/TaskHub.Application/Repositories/Interfaces/ITeamRepository.cs`
- `backend/TaskHub.Application/Repositories/Interfaces/ITimeTrackingRepository.cs`
- `backend/TaskHub.Application/Repositories/Interfaces/IUserRepository.cs`
- `backend/TaskHub.Application/Services/AuthService.cs`
- `backend/TaskHub.Application/Services/BoardListService.cs`
- `backend/TaskHub.Application/Services/BoardMapping.cs`
- `backend/TaskHub.Application/Services/BoardService.cs`
- `backend/TaskHub.Application/Services/CollaborationReadService.cs`
- `backend/TaskHub.Application/Services/CommentService.cs`
- `backend/TaskHub.Application/Services/Interfaces/IAttachmentStorage.cs`
- `backend/TaskHub.Application/Services/Interfaces/IAuthService.cs`
- `backend/TaskHub.Application/Services/Interfaces/IBoardListService.cs`
- `backend/TaskHub.Application/Services/Interfaces/IBoardService.cs`
- `backend/TaskHub.Application/Services/Interfaces/ICollaborationReadService.cs`
- `backend/TaskHub.Application/Services/Interfaces/ICommentService.cs`
- `backend/TaskHub.Application/Services/Interfaces/IGoogleIdentityVerifier.cs`
- `backend/TaskHub.Application/Services/Interfaces/IPermissionService.cs`
- `backend/TaskHub.Application/Services/Interfaces/IProjectService.cs`
- `backend/TaskHub.Application/Services/Interfaces/IProtectedHubContext.cs`
- `backend/TaskHub.Application/Services/Interfaces/ITaskItemService.cs`
- `backend/TaskHub.Application/Services/Interfaces/ITeamService.cs`
- `backend/TaskHub.Application/Services/Interfaces/ITimeTrackingService.cs`
- `backend/TaskHub.Application/Services/JwtConfiguration.cs`
- `backend/TaskHub.Application/Services/PermissionService.cs`
- `backend/TaskHub.Application/Services/ProjectService.cs`
- `backend/TaskHub.Application/Services/ProtectedHubContext.cs`
- `backend/TaskHub.Application/Services/ResourceVisibility.cs`
- `backend/TaskHub.Application/Services/TaskItemService.cs`
- `backend/TaskHub.Application/Services/TeamService.cs`
- `backend/TaskHub.Application/Services/TimeTrackingService.cs`
- `backend/TaskHub.Application/Services/TokenService.cs`
- `backend/TaskHub.Application/Validators/MembershipValidators.cs`
- `backend/TaskHub.Application/Validators/Phase1QueryValidators.cs`
- `backend/TaskHub.Application/Validators/TimeTrackingValidators.cs`
- `backend/TaskHub.Domain/Exceptions/AppException.cs`
- `backend/TaskHub.Infrastructure/Identity/GoogleIdentityVerifier.cs`
- `backend/TaskHub.Infrastructure/Repositories/BoardListRepository.cs`
- `backend/TaskHub.Infrastructure/Repositories/BoardReadRepository.cs`
- `backend/TaskHub.Infrastructure/Repositories/BoardRepository.cs`
- `backend/TaskHub.Infrastructure/Repositories/CollaborationReadRepository.cs`
- `backend/TaskHub.Infrastructure/Repositories/ProjectRepository.cs`
- `backend/TaskHub.Infrastructure/Repositories/RefreshTokenRepository.cs`
- `backend/TaskHub.Infrastructure/Repositories/TaskCollaborationRepository.cs`
- `backend/TaskHub.Infrastructure/Repositories/TaskItemRepository.cs`
- `backend/TaskHub.Infrastructure/Repositories/TeamRepository.cs`
- `backend/TaskHub.Infrastructure/Repositories/TimeTrackingRepository.cs`
- `backend/TaskHub.Infrastructure/Repositories/UserRepository.cs`
- `backend/TaskHub.Infrastructure/Storage/LocalAttachmentStorage.cs`
- `backend/TaskHub.Tests/Fixtures/Phase1ApiFactory.cs`
- `backend/TaskHub.Tests/Integration/AttachmentEndpointTests.cs`
- `backend/TaskHub.Tests/Integration/AuthEndpointTests.cs`
- `backend/TaskHub.Tests/Integration/DashboardAnalyticsEndpointTests.cs`
- `backend/TaskHub.Tests/Integration/JwtConfigurationTests.cs`
- `backend/TaskHub.Tests/Integration/Phase1AuthorizationEndpointTests.cs`
- `backend/TaskHub.Tests/Integration/Phase1ContractEndpointTests.cs`
- `backend/TaskHub.Tests/Integration/Phase1SecurityEndpointTests.cs`
- `backend/TaskHub.Tests/Integration/RealtimeRevocationIntegrationTests.cs`
- `backend/TaskHub.Tests/Services/AuthRegressionTests.cs`
- `backend/TaskHub.Tests/Services/BoardServiceTests.cs`
- `backend/TaskHub.Tests/Services/GoogleIdentityPolicyTests.cs`
- `backend/TaskHub.Tests/Services/ProjectBusinessRulesTests.cs`
- `backend/TaskHub.Tests/Services/RealtimeAuthorizationTests.cs`
- `backend/TaskHub.Tests/Services/TaskAssignmentBusinessRulesTests.cs`
- `backend/TaskHub.Tests/TaskHub.Tests.csproj`
- `docs/ai/AUTH_CONFIGURATION.md`
- `docs/ai/PHASE_1_IMPLEMENTATION_PLAN.md`
- `frontend/package-lock.json`
- `frontend/package.json`
- `frontend/src/api/attachmentApi.js`
- `frontend/src/api/axiosInstance.js`
- `frontend/src/api/boardApi.js`
- `frontend/src/api/commentApi.js`
- `frontend/src/api/listApi.js`
- `frontend/src/api/pagedCollection.js`
- `frontend/src/api/projectApi.js`
- `frontend/src/api/projectMemberApi.js`
- `frontend/src/api/timeTrackingApi.js`
- `frontend/src/components/tasks/TaskAttachments.jsx`
- `frontend/src/components/tasks/TaskCard.jsx`
- `frontend/src/components/timetracking/TimeTrackingWidget.jsx`
- `frontend/src/pages/projects/tabs/ProjectTasksBoard.jsx`
- `frontend/src/services/taskService.js`
- `frontend/src/stores/useTimeTrackingStore.js`
- `frontend/tests/authRefresh.test.js`
- `frontend/tests/pagedCollection.test.jsx`
- `frontend/tests/phase1Contracts.test.jsx`
- `frontend/vitest.config.js`
