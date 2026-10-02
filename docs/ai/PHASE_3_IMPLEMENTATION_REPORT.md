# Phase 3 — Bounded Database and API Queries

2026-09-29. **PHASE 3 IMPLEMENTATION STATUS: PASS. MANUAL QA STATUS: NOT VERIFIED.** Implementation and automated exit criteria pass; the specific mobile/desktop checks in the final closeout remain **MANUAL VERIFICATION REQUIRED** because no browser is available. This supersedes the earlier combined PARTIAL classification without claiming browser verification passed. Stop for human review. No commit, push, staging, Phase 4 work, application database update, or new migration was performed in this continuation or closeout.

## Recovery and preservation

Branch: `feature/project-collaboration-dashboard-publish`; HEAD: `7302a02`. AGENTS.md is authoritative. The audit, development roadmap, Phase 2 report, working/index diffs, new Phase 3 sources, and `.phase3-review` artifacts were inspected before continuing. No Phase 3 report existed at recovery.

The original interrupted implementation already contained bounded reads, projections, SQL aggregates, bulk updates, collation-aware email lookup, and frontend paging. The later `.phase3-review/recovery-20260929` attempt completed attachment response/loading/error handling, team headers and roster paging, paging consumers, parsing/lint blockers, route ownership, and regression coverage. Its recorded backend result was 197/197; its separate Phase 3 SQL run was 4/4. The earlier failing team ownership-race test had already been corrected to accept the established business-validation response while retaining the owner invariant. These changes were preserved rather than repeated.

This continuation first ran the attachment/paging frontend tests (12/12). Two new regressions then reproduced remaining defects: legacy collection aliases omitted pagination metadata, and SQL analytics changed burndown boundaries. Both failed before fixes in `targeted-before.trx`. A connection interruption occurred during the fix call. After reconnecting, AGENTS.md and Git state were read again: every patch was present, no follow-up TRX existed, and no test process was running. Only the unfinished test command was resumed.

The inherited index contains 69 staged Phase 2 paths and is byte-for-byte unchanged according to `git ls-files --stage`. Existing documentation deletions, unrelated frontend refactors, untracked skills, recovery artifacts, and both migration directories were retained. Session hashes and status snapshots are under `.phase3-review/continuation-20260929/`; older evidence was not overwritten.

## Work completed in this continuation

- Added pagination headers to the remaining legacy board, board-list, comment, project-board and project-member collection aliases. Nested legacy board-list responses now include `TaskPage`; the existing array fields remain. CORS exposes `X-Pagination` and `Deprecation`.
- Restored analytics snapshot totals, rolling time-of-day burndown cutoffs, the established maximum 30-day burndown horizon, latest-current-Done completion semantics, and per-task nonnegative completion durations before averaging. Aggregation stays in SQL; board/scope/date filtering and distinct daily completion velocity remain coordinated.
- Added bounded server search to Kanban queries and both board consumers. Search applies before card totals and paging, so matches beyond the initially loaded cards are discoverable. Search changes restart pages; reorder remains disabled while partial, filtered, or loading.
- Protected My Tasks and standalone board requests from late results after filter/navigation changes. Clearing My Tasks search resets page one. Calendar ignores stale success, error, and loading updates after a date-window change.
- Added HTTP regressions for legacy metadata and search beyond the initial card page, SQL regressions for analytics semantics and invitation lookup under case-sensitive/case-insensitive column collations, and frontend regressions for server search and stale responses.

## Finding verification

| Scope | Current implementation and evidence |
| --- | --- |
| C18 | Task comment counts exclude deleted comments; attachment counts and project board/member counts use explicit database projections. Mutation response counts are read explicitly too. SQL HTTP tests cover populated counts and unloaded child collections. |
| C20 | Authorized server paging for My Tasks, projects/workspace projects, boards/columns/cards, team/project members, comments, attachments/activity and time history. Unique ID tie breakers, bounds, independent totals, and date-window validation are covered. |
| C21 | Dashboard conditional counts, SQL velocity buckets with distinct task semantics, SQL union activity paging, and SQL analytics with shared board/scope criteria. Current-state overview totals are snapshots; event/time series use the requested date interval. |
| C22 | Task detail loads the header/authorization relationships, without lifetime comment/attachment/activity Includes. Child tabs use bounded endpoints. |
| C23 | Task/board search uses bounded Contains without LOWER(column). Email equality uses the actual column collation: direct equality for case-insensitive columns; the existing lowercase compatibility predicate remains for case-sensitive/unknown schemas. SQL tests verify trimmed mixed-case user/invitation inputs and project-scoped invitation lookup. |
| C19/C24 performance | Notification read-all and refresh-token revoke-all execute one user-scoped SQL UPDATE each, with zero tracked entities. Nonrelational test fallback remains. Existing notification delivery and Phase 2 token-rotation/rollback semantics are preserved. |
| C26 performance | Scoped bulk notification reads retain existing unread-count/events. Notification UI ownership/realtime architecture remains later-phase scope. |
| Permission queries | Project reads no longer load all members/users; board reads no longer load all lists/cards. Active collection routes use scoped visibility predicates rather than per-private-project authorization loops. PermissionService remains authoritative; no cross-request permission cache was added. |
| L01 | Preserved before-baseline and captured after evidence for 10/100/1,000 active-card boards. Query counts, tracked entities, returned card counts and process allocation deltas are distinguished below. |
| L07 | SQL history projections fetch timestamps; live durations are calculated only for the bounded returned page. SQL tests execute running-history and stopped-entry report paths. |

## API contracts and frontend adoption

Canonical collection bodies use `ApiResponse<PagedResult<T>>` with `items`, `page`, `pageSize`, `totalItems`, `totalPages`. Page defaults are 1/20; invalid page sizes outside 1–100 are rejected with the common error envelope rather than silently clamped. Page is bounded to 1–1,000,000. Card pages default to 50 with the same 100 maximum. Sorts add unique ID tie breakers.

| API | Bounds / semantics |
| --- | --- |
| `/api/v1/tasks`, `/tasks/my-tasks`, `/tasks/summary` | Task filter/search/sort plus independent accessible-task summary; totals do not equal the current page length. |
| `/api/v1/tasks/calendar` | Explicit start/end, ordered interval at most 62 days, optional project/board scope, paged matches. |
| `/api/v1/projects`, `/projects/{id}`, `/teams`, `/teams/{id}` | Project server search/status/archive/workspace filters; safe header DTOs with explicit counts/current role. |
| `/api/v1/projects/{id}/boards`, `/boards`, `/boardlists/board/{id}`, `/projects/{id}/kanban` | Paged boards/columns/cards; `searchKeyword` max 200 characters applies before card count/page. Standalone `/api/boards/{id}` also accepts the bounded Kanban query. |
| `/api/v1/projects/{id}/members`, `/teams/{id}/members`, `/tasks/{id}/comments` | Authorized roster/comment pages; stable ties. |
| `/api/v1/tasks/{id}/attachments`, `/tasks/{id}/activity-logs` | Separate bounded child tabs. Attachment metadata omits storage keys; authenticated download remains separate. |
| `/api/v1/timetracking/task/{id}`, `/my-entries`, `/report` | User history/report defaults to the last 30 days, validates an ordered interval at most 366 days. Reports aggregate stopped entries, with independent totals and explicit task-group page metadata (`totalTaskGroups`, `taskGroupPage`, `taskGroupPageSize`, `taskGroupTotalPages`). Daily groups are date-bounded and user groups are current-user scoped. |
| Dashboard / analytics | Dashboard activity uses SQL UNION ALL then page ordering, with bounded action lookups. Analytics validates 1–365 days. Re-completion is distinct per task per velocity bucket; heatmap retains event counts. |

Legacy unversioned array aliases are transitional: bounded items plus `Deprecation` and `X-Pagination` headers, with additive nested metadata where needed. Supported frontend consumers use canonical bodies. No pre-existing versioned collection body was replaced with an incompatible shape. Existing task-detail child array fields remain empty; their separately paged endpoints are the supported read path.

MyTasksPage uses server filters and independent summary counts. Calendar appends pages and shows loaded/total counts. BoardDetailPage and ProjectTasksBoard expose partial data, load more columns/cards, search on the server, and block reorder of incomplete/filtered data. TaskAttachments, TaskActivity and TaskComments consume page metadata and distinguish errors from empty results. Projects/teams and their member screens expose page controls; time tracking exposes task-history pages. Existing selector helpers deliberately traverse explicit page metadata for complete choices; they do not silently stop at page one. Column selectors fetch column choices without loading every card. Unused legacy service/repository methods were not broadly removed as part of Phase 3.

## Validation

Final command results are recorded in the generated evidence section below. Targeted backend checks passed 13/13; targeted frontend checks passed 16/16 before the full runs. The initial sandboxed .NET command could not read the existing NuGet configuration; the approved outside-sandbox rerun worked. SQL fixtures create and delete only generated `TaskHub_Phase2_Test_*` LocalDB databases, never the application's configured database. The collation regression changes only its disposable test table.

Frontend full lint and production build passed. The build retains the existing >500 kB chunk warning. Vitest retains a warning from a Phase 2 Button mock forwarding `leftIcon` to a DOM element; tests pass and production Button rendering is not implicated.

Browser setup returned `No browser is available`; discovery returned `[]`, including the final closeout availability check. Mobile/desktop live browser interaction is **NOT VERIFIED**. The [browser skill](C:/Users/ADMIN/.codex/plugins/cache/openai-bundled/browser/26.908.70816/skills/control-in-app-browser/SKILL.md) and its troubleshooting instructions require reporting an unavailable browser rather than substituting an unrelated control workaround. Automated DOM interaction tests do not prove live browser behavior at either viewport. The user's final closeout instruction classifies this outstanding manual verification separately from implementation PASS; the narrow functional checklist is recorded below.

The source-pattern secret scan examined 410 source/config/test files, including new untracked code. Its two candidates are the English/Vietnamese localization labels for `common.password`; no credential was found and no secret value was printed. This is a working-tree pattern scan, not a Git-history or external secret-manager audit.

## Performance evidence and index decision

Original baseline: `.phase3-review/phase3-before.trx`. Latest after evidence: the `Phase3QueryEvidenceTests` result in `continuation-20260929/backend-final.trx`, extracted below. These are repository measurements, not full HTTP request totals.

| Active seeded cards | Before board queries / tracked entities | Before detail queries / tracked entities | Before column queries / tracked | Before personal stats queries |
| --- | --- | --- | --- | --- |
| 10 | 5 / 35 | 1 / 8 | 4 / 0 | 6 |
| 100 | 5 / 305 | 1 / 8 | 4 / 0 | 6 |
| 1,000 | 5 / 3,005 | 1 / 8 | 4 / 0 | 6 |

The legacy allocation probe deliberately reconstructs the committed graph. Its returned-card count includes the fixture's one soft-deleted card; the active count is the size label. The bounded probe includes board header, task detail and one column page; returned active cards are 10/50/50. `GC.GetTotalAllocatedBytes` deltas are process-wide and include EF compilation/concurrent/background work. They are samples, not isolated endpoint allocation benchmarks; no percentage improvement or latency claim is made. Tracked entities are not SQL rows. Full ORM result-row fan-out is not instrumented; only the six explicit plan probes count returned SQL rows.

No new Phase 3 index or migration is justified by the current fixture evidence. Actual executed STATISTICS XML plans were inspected, not just generated SQL. Existing indexes include Tasks(ListId), Lists(BoardId), Comments(TaskId), TaskActivityLogs(TaskId), ProjectActivityLogs(ProjectId), Projects(OwnerId,Slug)/(WorkspaceId,Slug), Notifications(UserId,CreatedAt)/IsRead, and TimeEntries(TaskId,StartTime)/(UserId,StartTime) plus active-timer uniqueness. Slug is bounded to 100 Unicode characters. The 1,000-card fixture often uses a clustered scan/sort; comments/activity use the existing FK seek plus sort. No missing-index group was reported. Empty notification/time probes and a one-project fixture do not establish production index benefit.

The exact probe SQL is in `Phase3QueryEvidenceTests.InspectIndexesAndPlan`: ordered active cards by ListId, comments by TaskId, activity by TaskId, active projects by OwnerId, unread notifications by UserId, and time sums by TaskId within a user/date scope. These simplified probes are not claimed to be full ORM dashboard plans. A future candidate such as Tasks(ListId,IsDeleted,Position,Id) or activity(TaskId,CreatedAt,Id) needs representative read/write plans and logical-read measurements; it adds index storage and mutation maintenance and may duplicate existing FK access. No such index was created. The inherited Phase 2 migration and snapshot are unchanged.

## Remaining work and review boundaries

Complete mobile and desktop browser smoke checks once a browser connection is available: My Tasks filtering/page changes; full-board server search and incremental cards; child tabs; team/project paging; calendar load-more; time-history navigation; error/403 states. No known failing automated test or unresolved implementation blocker remains after the recorded final gates. Production-size execution plans, isolated allocation traces and deployment database history remain operational follow-ups, not measured claims in this report.

The report's inventories distinguish changes from this continuation from the inherited Phase 3 implementation and unrelated dirty state. Paths with overlapping staged Phase 2/frontend-refactor hunks are not presented as entirely Phase 3-owned. The exact current `git status --short` is included in the generated section and saved separately. Nothing is staged for review by this continuation.

<!-- GENERATED_EVIDENCE -->

## Final automated results

| Gate | Result |
| --- | --- |
| Backend build | PASS — 0 warnings, 0 errors |
| Full backend tests | PASS — 201/201, 0 failed, 0 skipped |
| Phase 3 SQL Server tests | PASS — 6/6, executed within the full suite on disposable LocalDB |
| Vitest | PASS — 25/25 tests across 5 files |
| Node auth-refresh tests | PASS — 8/8 |
| Frontend lint | PASS — full eslint run, exit 0 |
| Frontend production build | PASS — existing large-chunk warning |
| git diff --check / --cached --check | PASS |
| Secret scan | PASS — 410 files; 2 reviewed localization-label false positives |
| Index preservation | PASS — all 69 staged paths unchanged |
| Mobile/desktop browser checks | NOT VERIFIED — no browser connection available |

Commands (backend commands run from backend, frontend commands from frontend):

```text
dotnet build TaskHub.sln -tl:off
dotnet test TaskHub.sln -m:1 -nodeReuse:false -tl:off --logger "trx;LogFileName=backend-final.trx" --results-directory D:/web/TaskHub/.phase3-review/continuation-20260929
npx.cmd vitest run --reporter=default --reporter=json --outputFile=../.phase3-review/continuation-20260929/vitest.json
node --test tests/authRefresh.test.js
npm.cmd run lint
npm.cmd run build
git diff --check
git diff --cached --check
node .phase3-review/continuation-20260929/verify.cjs
```

SQL cases executed:

- TaskHub.Tests.Integration.Phase3SqlReadTests.Analytics_PreservesRollingCutoffsLatestCompletionAndThirtyDayBurndown — Passed
- TaskHub.Tests.Integration.Phase3RecoveryTests.CollectionsOnSql_ArePagedAuthorizedAndHaveExplicitCounts — Passed
- TaskHub.Tests.Integration.Phase3RecoveryTests.BulkReadAndRevoke_AreSingleScopedUpdatesWithoutTracking — Passed
- TaskHub.Tests.Integration.Phase3SqlReadTests.EmailEquality_UsesColumnCollationAndPreservesCaseInsensitiveInvitationMatching — Passed
- TaskHub.Tests.Integration.Phase3SqlReadTests.AggregatesAndDeepActivityExecuteOnSqlServer — Passed
- TaskHub.Tests.Integration.Phase3QueryEvidenceTests.QueryEvidence_10_100_1000_Cards — Passed

## Measured after state

```text
ALLOCATION size=10 shape=legacyGraph processBytes=47132248 returnedCards=11 tracked=35 queries=5
EVIDENCE size=10 boardQueries=1 boardTracked=2
EVIDENCE size=10 detailQueries=1 detailTracked=5
EVIDENCE size=10 columnsQueries=4 columnsTracked=0
ALLOCATION size=10 shape=boundedHeaderDetailColumns processBytes=69898928 returnedCards=10 sqlRows=not-instrumented
EVIDENCE size=10 statsQueries=2
ALLOCATION size=100 shape=legacyGraph processBytes=8376392 returnedCards=101 tracked=305 queries=5
EVIDENCE size=100 boardQueries=1 boardTracked=2
EVIDENCE size=100 detailQueries=1 detailTracked=5
EVIDENCE size=100 columnsQueries=4 columnsTracked=0
ALLOCATION size=100 shape=boundedHeaderDetailColumns processBytes=5255928 returnedCards=50 sqlRows=not-instrumented
EVIDENCE size=100 statsQueries=2
ALLOCATION size=1000 shape=legacyGraph processBytes=144953448 returnedCards=1001 tracked=3005 queries=5
EVIDENCE size=1000 boardQueries=1 boardTracked=2
EVIDENCE size=1000 detailQueries=1 detailTracked=5
EVIDENCE size=1000 columnsQueries=4 columnsTracked=0
ALLOCATION size=1000 shape=boundedHeaderDetailColumns processBytes=37683112 returnedCards=50 sqlRows=not-instrumented
EVIDENCE size=1000 statsQueries=2
ACTUAL_PLAN probe=cards returnedSqlRows=50 operators=Top,Filter,Sort,Clustered Index Scan indexes=[PK_Tasks] missingIndexGroups=0
ACTUAL_PLAN probe=comments returnedSqlRows=1 operators=Sort,Nested Loops,Index Seek,Clustered Index Seek indexes=[IX_Comments_TaskId],[PK_Comments] missingIndexGroups=0
ACTUAL_PLAN probe=activity returnedSqlRows=1 operators=Sort,Nested Loops,Index Seek,Clustered Index Seek indexes=[IX_TaskActivityLogs_TaskId],[PK_TaskActivityLogs] missingIndexGroups=0
ACTUAL_PLAN probe=projects returnedSqlRows=1 operators=Sort,Clustered Index Scan indexes=[PK_Projects] missingIndexGroups=0
ACTUAL_PLAN probe=notifications returnedSqlRows=0 operators=Sort,Clustered Index Scan indexes=[PK_Notifications] missingIndexGroups=0
ACTUAL_PLAN probe=time returnedSqlRows=0 operators=Stream Aggregate,Nested Loops,Index Scan,Clustered Index Seek indexes=[IX_TimeEntries_TaskId_StartTime],[PK_TimeEntries] missingIndexGroups=0
```

The smallest bounded allocation sample exceeds the legacy sample in this concurrent full-suite run. These process-wide numbers do not establish an isolated memory saving. Query counts and bounded returned card counts are the reproducible structural evidence. Full existing-index output is saved in [query-evidence.txt](../../.phase3-review/continuation-20260929/query-evidence.txt).

## Exact files changed in this continuation

23 source/test/document files changed relative to the continuation's on-disk baseline (including work completed immediately before the connection interruption):

```text
backend/TaskHub.API/Controllers/BoardController.cs
backend/TaskHub.API/Controllers/BoardListController.cs
backend/TaskHub.API/Controllers/CommentController.cs
backend/TaskHub.API/Controllers/ProjectController.cs
backend/TaskHub.API/Controllers/ProjectMemberController.cs
backend/TaskHub.API/Program.cs
backend/TaskHub.Application/DTOs/BoardListDtos.cs
backend/TaskHub.Application/DTOs/ProjectKanbanDtos.cs
backend/TaskHub.Application/Services/BoardService.cs
backend/TaskHub.Application/Services/Interfaces/IBoardService.cs
backend/TaskHub.Application/Validators/Phase1QueryValidators.cs
backend/TaskHub.Infrastructure/Repositories/BoardReadRepository.cs
backend/TaskHub.Infrastructure/Repositories/DashboardRepository.Analytics.cs
backend/TaskHub.Tests/Integration/Phase3ReadTests.cs
backend/TaskHub.Tests/Integration/Phase3SqlReadTests.cs
docs/ai/PHASE_3_IMPLEMENTATION_REPORT.md
frontend/src/api/boardApi.js
frontend/src/pages/boards/BoardDetailPage.jsx
frontend/src/pages/projects/tabs/ProjectTasksBoard.jsx
frontend/src/pages/tasks/MyTasksPage.jsx
frontend/src/stores/useCalendarStore.js
frontend/tests/phase1Contracts.test.jsx
frontend/tests/phase3Paging.test.jsx
```

Local review artifacts were also added under `.phase3-review/continuation-20260929/`: verification/report scripts, baseline hashes/index/status, targeted-before/after and final backend TRX, backend summary and query evidence, Vitest JSON, source inventories, final status, and redacted secret-scan findings. Build/test output directories remain ignored.

The [Phase 3 inventory](../../.phase3-review/continuation-20260929/phase3-changed-files.json) compares current content against the original Phase 3 hashes. Previously untracked paths without an original hash are not attributed to Phase 3 merely because they are absent from that hash map. The [continuation inventory](../../.phase3-review/continuation-20260929/session-changed-files.json) records exactly this session's edits.

## Remaining unrelated working-tree state

All 31 inherited documentation deletions remain. The 69 staged Phase 2 paths, Phase 2 report/auth configuration, unrelated frontend refactors and context modules, untracked audit/roadmap/context notes, `.agents/`, `skills-lock.json`, and older review artifacts remain. Overlapping source files retain their earlier hunks; neither the index nor untracked unrelated files were reset or deleted. See [inherited untouched status](../../.phase3-review/continuation-20260929/inherited-untouched-status.txt) for exact paths. The report and this session's 22 implementation/test file edits account for all source hash changes since the continuation baseline.

## git status --short

```text
 M backend/TaskHub.API/Controllers/BoardController.cs
 M backend/TaskHub.API/Controllers/BoardListController.cs
 M backend/TaskHub.API/Controllers/CommentController.cs
M  backend/TaskHub.API/Controllers/NotificationController.cs
MM backend/TaskHub.API/Controllers/ProjectController.cs
MM backend/TaskHub.API/Controllers/ProjectMemberController.cs
 M backend/TaskHub.API/Controllers/TaskItemController.cs
 M backend/TaskHub.API/Controllers/TeamController.cs
MM backend/TaskHub.API/Controllers/TimeTrackingController.cs
MM backend/TaskHub.API/Program.cs
A  backend/TaskHub.API/Workers/AttachmentCleanupWorker.cs
M  backend/TaskHub.Application/DTOs/AppDtos.cs
 M backend/TaskHub.Application/DTOs/BoardDtos.cs
 M backend/TaskHub.Application/DTOs/BoardListDtos.cs
 M backend/TaskHub.Application/DTOs/ProjectDtos.cs
 M backend/TaskHub.Application/DTOs/ProjectKanbanDtos.cs
 M backend/TaskHub.Application/DTOs/TaskFeatureDtos.cs
 M backend/TaskHub.Application/DTOs/TeamDtos.cs
 M backend/TaskHub.Application/DTOs/TimeTrackingDtos.cs
A  backend/TaskHub.Application/Repositories/Interfaces/IAuditRepository.cs
 M backend/TaskHub.Application/Repositories/Interfaces/ICollaborationReadRepository.cs
 M backend/TaskHub.Application/Repositories/Interfaces/IDashboardRepository.cs
A  backend/TaskHub.Application/Repositories/Interfaces/IMutationRunner.cs
A  backend/TaskHub.Application/Repositories/Interfaces/INotificationRepository.cs
A  backend/TaskHub.Application/Repositories/Interfaces/IOutboxRepository.cs
MM backend/TaskHub.Application/Repositories/Interfaces/IProjectRepository.cs
M  backend/TaskHub.Application/Repositories/Interfaces/IRefreshTokenRepository.cs
MM backend/TaskHub.Application/Repositories/Interfaces/ITaskItemRepository.cs
MM backend/TaskHub.Application/Repositories/Interfaces/ITimeTrackingRepository.cs
 M backend/TaskHub.Application/Services/AnalyticsService.cs
A  backend/TaskHub.Application/Services/AttachmentCleanupService.cs
M  backend/TaskHub.Application/Services/AuditService.cs
M  backend/TaskHub.Application/Services/AuthService.cs
 M backend/TaskHub.Application/Services/BoardService.cs
 M backend/TaskHub.Application/Services/CollaborationReadService.cs
M  backend/TaskHub.Application/Services/CommentService.cs
A  backend/TaskHub.Application/Services/DurableDelivery.cs
M  backend/TaskHub.Application/Services/EmailService.cs
M  backend/TaskHub.Application/Services/INotificationService.cs
 M backend/TaskHub.Application/Services/Interfaces/IBoardService.cs
 M backend/TaskHub.Application/Services/Interfaces/ICollaborationReadService.cs
M  backend/TaskHub.Application/Services/Interfaces/IProjectService.cs
 M backend/TaskHub.Application/Services/Interfaces/ITaskItemService.cs
MM backend/TaskHub.Application/Services/Interfaces/ITimeTrackingService.cs
M  backend/TaskHub.Application/Services/Interfaces/ITokenService.cs
M  backend/TaskHub.Application/Services/NotificationService.cs
MM backend/TaskHub.Application/Services/ProjectService.cs
M  backend/TaskHub.Application/Services/SettingsService.cs
MM backend/TaskHub.Application/Services/TaskItemService.cs
M  backend/TaskHub.Application/Services/TeamService.cs
MM backend/TaskHub.Application/Services/TimeTrackingService.cs
M  backend/TaskHub.Application/Services/TokenService.cs
 M backend/TaskHub.Application/Validators/Phase1QueryValidators.cs
A  backend/TaskHub.Application/Validators/Phase2MutationValidators.cs
M  backend/TaskHub.Application/Validators/ProjectValidators.cs
M  backend/TaskHub.Application/Validators/TaskValidators.cs
A  backend/TaskHub.Domain/Entities/OutboxMessage.cs
M  backend/TaskHub.Infrastructure/Data/AppDbContext.cs
A  backend/TaskHub.Infrastructure/Migrations/20260925043942_AddMutationOutboxAndScopedUniqueness.Designer.cs
A  backend/TaskHub.Infrastructure/Migrations/20260925043942_AddMutationOutboxAndScopedUniqueness.cs
M  backend/TaskHub.Infrastructure/Migrations/AppDbContextModelSnapshot.cs
A  backend/TaskHub.Infrastructure/Repositories/AuditRepository.cs
 M backend/TaskHub.Infrastructure/Repositories/BoardReadRepository.cs
 M backend/TaskHub.Infrastructure/Repositories/BoardRepository.cs
 M backend/TaskHub.Infrastructure/Repositories/CollaborationReadRepository.cs
 M backend/TaskHub.Infrastructure/Repositories/DashboardRepository.cs
A  backend/TaskHub.Infrastructure/Repositories/MutationRunner.cs
AM backend/TaskHub.Infrastructure/Repositories/NotificationRepository.cs
A  backend/TaskHub.Infrastructure/Repositories/OutboxRepository.cs
MM backend/TaskHub.Infrastructure/Repositories/ProjectRepository.cs
MM backend/TaskHub.Infrastructure/Repositories/RefreshTokenRepository.cs
MM backend/TaskHub.Infrastructure/Repositories/TaskItemRepository.cs
MM backend/TaskHub.Infrastructure/Repositories/TimeTrackingRepository.cs
 M backend/TaskHub.Infrastructure/Repositories/UserRepository.cs
M  backend/TaskHub.Infrastructure/Storage/LocalAttachmentStorage.cs
A  backend/TaskHub.Tests/Fixtures/Phase2SqlFactory.cs
A  backend/TaskHub.Tests/Integration/Phase2MigrationTests.cs
A  backend/TaskHub.Tests/Integration/Phase2RecoveryTests.cs
A  backend/TaskHub.Tests/Integration/Phase2SqlMutationTests.cs
AM backend/TaskHub.Tests/Integration/Phase2TaskMutationTests.cs
 M backend/TaskHub.Tests/Services/AnalyticsServiceTests.cs
M  backend/TaskHub.Tests/Services/ProjectBusinessRulesTests.cs
M  backend/TaskHub.Tests/Services/RealtimeAuthorizationTests.cs
M  backend/TaskHub.Tests/Services/TaskAssignmentBusinessRulesTests.cs
M  docs/ai/AUTH_CONFIGURATION.md
AM docs/ai/PHASE_2_IMPLEMENTATION_REPORT.md
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
 M frontend/src/api/attachmentApi.js
M  frontend/src/api/authApi.js
M  frontend/src/api/axiosInstance.js
 M frontend/src/api/boardApi.js
 M frontend/src/api/calendarApi.js
 M frontend/src/api/commentApi.js
 M frontend/src/api/listApi.js
 M frontend/src/api/projectApi.js
 M frontend/src/api/projectMemberApi.js
MM frontend/src/api/taskApi.js
 M frontend/src/api/teamApi.js
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
 M frontend/src/components/tasks/TaskComments.jsx
 M frontend/src/components/tasks/TaskDetailDrawer.jsx
 M frontend/src/components/tasks/TaskDetailModal.jsx
 M frontend/src/components/tasks/TaskFilters.jsx
M  frontend/src/components/tasks/TaskFormModal.jsx
 M frontend/src/components/tasks/TaskKanbanBoard.jsx
 M frontend/src/components/tasks/TaskList.jsx
 M frontend/src/components/tasks/TaskListHeader.jsx
 M frontend/src/components/tasks/TaskModal.jsx
 M frontend/src/components/tasks/TaskTable.jsx
 M frontend/src/components/theme/ThemeToggle.jsx
 M frontend/src/context/AuthContext.jsx
 M frontend/src/context/NotificationContext.jsx
 M frontend/src/context/TaskContext.jsx
 M frontend/src/context/ThemeContext.jsx
 M frontend/src/pages/auth/LoginPage.jsx
 M frontend/src/pages/auth/RegisterPage.jsx
 M frontend/src/pages/boards/BoardDetailPage.jsx
 M frontend/src/pages/calendar/CalendarPage.jsx
 M frontend/src/pages/dashboard/DashboardPage.jsx
 M frontend/src/pages/notifications/NotificationsPage.jsx
 M frontend/src/pages/profile/ProfilePage.jsx
 M frontend/src/pages/projects/AcceptProjectInvite.jsx
 M frontend/src/pages/projects/ProjectDetailPage.jsx
 M frontend/src/pages/projects/ProjectsPage.jsx
MM frontend/src/pages/projects/tabs/ProjectSettingsTab.jsx
MM frontend/src/pages/projects/tabs/ProjectTasksBoard.jsx
 M frontend/src/pages/tasks/AcceptTaskInvite.jsx
MM frontend/src/pages/tasks/MyTasksPage.jsx
 M frontend/src/pages/teams/TeamDetailPage.jsx
 M frontend/src/pages/teams/TeamsPage.jsx
 M frontend/src/routes/PrivateRoute.jsx
 M frontend/src/services/taskService.js
 M frontend/src/stores/useCalendarStore.js
 M frontend/src/stores/useProjectStore.js
 M frontend/src/stores/useTimeTrackingStore.js
M  frontend/tests/authRefresh.test.js
 M frontend/tests/pagedCollection.test.jsx
 M frontend/tests/phase1Contracts.test.jsx
A  frontend/tests/phase2Mutations.test.jsx
AM frontend/tests/phase2ProjectSettings.test.jsx
?? .agents/
?? .phase2-review/
?? .phase3-review/
?? backend/TaskHub.API/Controllers/BoundedReadController.cs
?? backend/TaskHub.API/Controllers/LegacyPaging.cs
?? backend/TaskHub.Application/DTOs/AttachmentLinks.cs
?? backend/TaskHub.Application/DTOs/BoundedQueryDtos.cs
?? backend/TaskHub.Application/Validators/BoundedQueryValidators.cs
?? backend/TaskHub.Infrastructure/Repositories/CollaborationReadRepository.Collections.cs
?? backend/TaskHub.Infrastructure/Repositories/DashboardRepository.Analytics.cs
?? backend/TaskHub.Infrastructure/Repositories/EmailComparison.cs
?? backend/TaskHub.Infrastructure/Repositories/ReadProjection.cs
?? backend/TaskHub.Infrastructure/Repositories/TaskItemRepository.Reads.cs
?? backend/TaskHub.Infrastructure/Repositories/TimeTrackingRepository.Reads.cs
?? backend/TaskHub.Tests/Integration/Phase3QueryEvidenceTests.cs
?? backend/TaskHub.Tests/Integration/Phase3ReadTests.cs
?? backend/TaskHub.Tests/Integration/Phase3RecoveryTests.cs
?? backend/TaskHub.Tests/Integration/Phase3SqlReadTests.cs
?? docs/ai/.phase1-task-context-check.txt
?? docs/ai/PHASE_3_IMPLEMENTATION_REPORT.md
?? docs/ai/TASKHUB_DEVELOPMENT_ROADMAP.md
?? docs/ai/TASKHUB_FULL_AUDIT_AND_ROADMAP.md
?? frontend/src/components/ui/PageControls.jsx
?? frontend/src/context/authState.js
?? frontend/src/context/taskState.js
?? frontend/src/context/themeState.js
?? frontend/tests/phase3Paging.test.jsx
?? skills-lock.json
```

## Final Phase 3 closeout — 2026-09-29

**IMPLEMENTATION STATUS: PASS. AUTOMATED VALIDATION: PASS. MANUAL QA STATUS: NOT VERIFIED.**

The roadmap's Phase 3 implementation and automated exit criteria are satisfied by the current code and preserved evidence. No actual remaining Phase 3 functional defect was identified in this closeout review. Browser checks remain **MANUAL VERIFICATION REQUIRED**; they are unresolved checks, not known failures or recorded passes. Phase 3 can close as implementation PASS with this explicit manual QA obligation, subject to human review.

### Exit-criteria review

| Roadmap requirement | Closeout result and supporting evidence |
| --- | --- |
| Bounded high-growth collections, deterministic paging, My Tasks server filters and independent totals | PASS — shared bounded query validators, ID tie breakers, `ReadProjection.Page`, `Phase3ReadTests`, and frontend paging regressions. Canonical page metadata and legacy compatibility are documented above. |
| Calendar/report range limits | PASS — calendar maximum 62 days; time reports maximum 366 days; analytics maximum 365 days. Ordered-range validation and SQL/frontend cases are preserved. |
| Explicit DB counts and bounded task children | PASS — task comment/attachment and project board/member projections; detail no longer includes lifetime sibling collections; separate paged comments, attachments and activity. SQL count/authorization cases pass. |
| Partial standalone/project boards | PASS for implementation/automated coverage — column/card metadata, load-more controls, server search before paging and restrictions on incomplete-data reorder are present. Live paging and duplicate-card checks remain in the manual checklist. |
| Dashboard, velocity, activity and analytics | PASS — conditional SQL statistics, distinct-task velocity buckets, bounded union activity paging and consistent board/scope criteria. Snapshot totals, event-series date bounds and completion/burndown semantics have regression coverage. |
| Time aggregation and L07 | PASS — stopped-entry reports aggregate in SQL; running history computes live duration only for the bounded page. Disposable SQL Server tests execute both paths. |
| Set-based updates | PASS — user-scoped notification read-all and token revoke-all SQL updates; existing authorization/events and Phase 2 refresh security behavior are preserved. |
| Email equality and permissions | PASS — trimmed normalized input and column-collation-aware equality; compatibility behavior on case-sensitive schemas is tested. Touched collection paths use scoped predicates/lightweight permission reads. |
| Performance evidence and index decision | PASS — original baseline, after query/tracking/returned-card measurements, qualified process allocation samples and actual plan probes are retained. Full ORM row fan-out was not measured and no percentage improvement is claimed. No new index is justified by this evidence. |

### Automated results remain applicable

No source, test or configuration file changed during this closeout. The current differences from the saved continuation baseline match its recorded 23-file change inventory exactly, with no additional or missing changed paths. No backend/frontend source or test file is newer than its respective saved validation result: backend `2026-09-29T12:25:15.964Z`, Vitest `2026-09-29T11:57:19.208Z`. These are artifact modification timestamps, not newly executed test times.

The existing results are retained: backend build with zero warnings/errors; backend tests 201/201; six Phase 3 SQL tests on disposable LocalDB; Vitest 25/25; auth tests 8/8; frontend lint/build; whitespace checks; and the 410-file source-pattern secret scan all PASS. Full test/build suites were not rerun for this documentation-only closeout. The documented chunk/mock warnings and secret-scan scope remain unchanged.

### Outstanding browser checklist

Supported browser discovery returned `[]`; no live browser interaction or screenshot was performed. Proposed representative viewports are desktop **1280 × 800** and mobile **390 × 844**. These dimensions describe pending checks, not executed sessions. Use authorized test data spanning multiple pages, including more than 50 cards in a column, and inspect request parameters/page metadata where specified.

| Functional check — MANUAL VERIFICATION REQUIRED | Desktop | Mobile |
| --- | --- | --- |
| My Tasks: next/previous pages, server search/status/priority filters, independent summary and matching totals; changing or clearing filters on a later page resets page one. | NOT VERIFIED | NOT VERIFIED |
| Calendar: navigation requests the bounded visible date interval; load-more exposes all available pages with loaded/total counts; failed/403 responses remain distinguishable from empty or complete results. | NOT VERIFIED | NOT VERIFIED |
| Standalone board: partial columns/cards are identified; load-more reaches later columns/cards without losing existing items; server search can find initially unloaded cards; incomplete/filtered reorder is disabled. | NOT VERIFIED | NOT VERIFIED |
| Project Kanban: load further columns and cards; each task ID appears once across ordinary successive pages and repeated load-more clicks; project/board/search changes reset the appropriate pages. | NOT VERIFIED | NOT VERIFIED |
| Comments: traverse child pages and confirm totals/order; adding/deleting a comment leaves a valid page; loading, empty and access-error states remain distinct. | NOT VERIFIED | NOT VERIFIED |
| Attachments: traverse metadata pages and confirm totals; authenticated download works from a later page; deletion updates the page; loading, empty and access-error states remain distinct. | NOT VERIFIED | NOT VERIFIED |
| Task activity: traverse pages with stable ordering and page metadata; failed/403 responses are visible and do not imply an empty history. | NOT VERIFIED | NOT VERIFIED |
| Project/team collections and rosters: reach later pages, retain server filters/counts and expose errors without silently showing only the first page. | NOT VERIFIED | NOT VERIFIED |
| Time tracking: navigate task-history pages and confirm displayed totals/page controls and error handling. | NOT VERIFIED | NOT VERIFIED |

All listed flows are applicable; none is marked NOT APPLICABLE merely because the browser is unavailable. No color, responsive redesign, animation, accessibility-overhaul or GSAP review was attempted. No cosmetic changes were made.

### Review boundary and exact working-tree state

- Phase 4 may be the next implementation phase after human acceptance of this closeout and explicit authorization for that work. It was not started here; manual QA remains outstanding until the checklist is actually executed.
- No Phase 3 index or migration was created. The evidence-based decision above remains unchanged, as do the inherited Phase 2 migration and snapshot.
- **Exact file changed during this closeout:** `docs/ai/PHASE_3_IMPLEMENTATION_REPORT.md` only. The 23-file inventory above describes the preceding implementation continuation, not new closeout code edits.
- Branch remains `feature/project-collaboration-dashboard-publish`; HEAD remains `7302a02`. The 69-path Phase 2 index matches the saved index byte-for-byte. Existing unrelated frontend work, documentation deletions, untracked files, skills and recovery artifacts were preserved.
- `git status --short` remains exactly the 209-line snapshot in the preceding section and in [final-status.txt](../../.phase3-review/continuation-20260929/final-status.txt). This report was already untracked, so its closeout edit does not add a status entry. No staging, commit, push, history/ref/stash operation or application database update occurred. Stop for human review.
