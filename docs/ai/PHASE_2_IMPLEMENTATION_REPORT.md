# Phase 2 implementation report

Status: Phase 2 PASS for the implementation and scoped validation, 2026-09-25. Ready for human review. No commit, push, merge or Phase 3 work. The full working tree still contains unrelated frontend lint failures, detailed below; this is not a claim that the entire working tree is ready to commit.

Baseline: branch feature/project-collaboration-dashboard-publish, HEAD 7302a02. Backend build: 0 warnings/errors; tests: 160 passed, 0 failed/skipped. Preserved 31 documentation deletions, 41 frontend modifications and 15 untracked files. File hashes captured before implementation.

## Finding checklist and decisions

| Finding | Files / operations | Baseline → target | Required evidence | Schema impact |
|---|---|---|---|---|
| C02 / L03 | AuthService, TokenService, RefreshTokenRepository; frontend refresh entry | Separate validation/revoke/insert → conditional consumption plus replacement in one transaction; one in-tab refresh entry | Concurrent SQL refresh, replay/inactive/rollback; client bootstrap | No token schema change needed |
| C11 | ProjectService, ProjectRepository, AppDbContext | Global SQL slug vs scoped service → workspace slug and personal-owner slug unique indexes | SQL duplicate scope / different scope | Replace global index with two filtered indexes |
| C12 / C32 | ProjectRepository.DeleteAsync, attachment service/storage | FK failure / early physical delete → transactional descendants plus durable private-file cleanup | Relational delete and failure injection | Outbox cleanup intent |
| C13 / C46 / C47 / L06 | Project, TaskItem, Comment, Team services | Multiple saves → serializable business transaction with after-commit delivery | Rollback at intermediate saves; concurrent transfer/member changes | No owner/version column required for serialized invariant checks |
| C14 | TaskItemService.MoveTaskAsync / repository | List/position only → same-board deterministic sibling order, explicit status independent of columns, optional stale timestamp guard | Same/cross board, order, conflict, rollback | None |
| C15 / C16 / C38 | AppDtos, task validators, task service, MyTasksPage | Defaults overwrite omitted fields / past-due rejection → presence-aware edit; merged date validation; dedicated status | Title-only/null/enum/date/overdue tests | None |
| C19 / C24 | Audit, Settings, Notification repositories/services; project/time interfaces | Direct context / missing cancellation → focused persistence boundaries, propagated cancellation | Architecture check and regression suite | None |
| L05 | TimeTrackingRepository, model | Check/insert race → unique UserId where EndTime IS NULL | Concurrent SQL starts | Filtered unique index |
| L09 | Both migration directories and ProjectType | Historical int → numeric string → semantic enum names | Disposable SQL migration/data rehearsal | Guarded numeric-string repair if rehearsal confirms |

## Transaction and delivery policy

Service mutation boundaries run through IMutationRunner, implemented next to repositories. Nested operations share the scoped transaction. SQL uses Serializable isolation; deadlock victims and uniqueness/concurrency violations return controlled 409 conflicts, without automatic replay of external work. Required entity, membership, activity, audit, notification and outbox saves commit or roll back together. Delivery intent is durable before commit; network/realtime work runs after commit. General delivery retries/claims remain Phase 9; pending intent must never be described as delivered.

Refresh uses existing unique Token plus IsRevoked/ExpiresAt and a conditional SQL update. Only the winner inserts a replacement. Reuse rejects the consumed token without revoking the successful sibling session; token-family revocation/hashing would change compatibility/retention and is not required for single consumption. No family/hash migration is planned.

## Migration safety decision (before creation)

`dotnet ef migrations list --project TaskHub.Infrastructure --startup-project TaskHub.API` succeeded; 17 migrations shown as applied in the configured development history. Current snapshot is under Migrations; Data/Migrations also contains attributed historical migrations and is retained. No application database update is authorized or performed.

Planned minimal migration: active timer filtered unique index (L05), durable outbox table (C13/C32), two scoped slug indexes (C11). Existing global slug uniqueness implies the weaker scoped constraints are compatible. Existing duplicate running timers must block migration for operator reconciliation, never be silently deleted. Downgrade to global slugs must reject duplicates across scopes; dropping outbox loses pending work and requires draining/exporting first. ProjectType repair will be based on disposable rehearsal; unknown representations must block, not be guessed. SQL runtime tests use only a generated TaskHub_Phase2_Test_* LocalDB database.

L09 rehearsal confirmed on disposable SQL: the full 17-migration chain applies, numeric string 0 materializes as Personal but a SQL predicate for Personal returns zero rows. The new migration therefore normalizes 0/1 to Personal/Team only after rejecting unknown values and workspace/type disagreement. Slugs longer than 100 and duplicate active timers also block before any schema mutation. Down blocks cross-scope duplicate slugs and pending outbox work. Old migrations remain byte-for-byte unchanged.

## Interrupted-session recovery and finding status

Recovered the existing branch and dirty tree instead of starting over. The recovery baseline built with zero warnings/errors and passed 177 tests, including the existing SQL tests, with no skips. No baseline backend failure needed classification A/B/C/D. The previous auth tests were read and checked against the implementation before any frontend rerun. The recovered report and existing migration were preserved.

`PARTIAL` below includes missing verification as well as an implementation gap. No finding was NOT STARTED or BLOCKED at recovery. All SQL results below were established with generated, disposable `TaskHub_Phase2_Test_*` LocalDB databases. Application and production databases were not updated.

| Finding | Recovered | Final | Implementation and evidence | Schema / missing work |
|---|---|---|---|---|
| C02 / L03 | C02 COMPLETE; L03 PARTIAL | COMPLETE | Conditional SQL consume and insert, one winner/one 401 without cookie; insert failure restores original token. In-tab sharing and simulated two-tab Web Lock tests pass. | No token schema change. Real browser smoke test remains a deployment check, not claimed here. |
| C11 | PARTIAL | COMPLETE | Workspace or personal-owner scoped SQL uniqueness, matching service checks; personal slug lookup now also includes owner. SQL tests cover both scopes and duplicate rejection. | Existing scoped-index migration retained. Nothing further required for Phase 2. |
| C12 | PARTIAL | COMPLETE | SQL permanent deletion removes board/list/task descendants; archive/restore retains data; late delete failure restores descendants and cleanup intents. UI keeps state on failure and disables duplicate actions. | Existing outbox migration; cleanup is eventual. |
| C13 / C46 transaction completion | PARTIAL | COMPLETE | Serializable runner covers project/default board/member/notification/activity, invitation acceptance, status/log, comment/activity, assignment and notification reads. Fault injection verifies rollback; callback test proves no delivery before commit or on rollback. | Existing outbox table. No additional schema. |
| C14 | PARTIAL | COMPLETE | Same-board authorization, deterministic position/ID order, optional expected timestamp conflict, independent status. Late outbox failure restores source/destination order and timestamps. | No extra columns. Legacy clients may omit the optional stale-move guard. |
| C15 / C16 | PARTIAL | COMPLETE | Presence-aware DTOs preserve omitted assignment/date/progress/status/priority; explicit null clears nullable fields. Form now omits unchanged values including the original deadline timestamp. Dedicated overdue status updates pass. | None. |
| C19 | COMPLETE | COMPLETE | Audit/Notification use focused repositories; Settings uses existing user/token persistence; TaskItem/Team/TimeTracking use their feature repositories. No audited service references IAppDbContext or EF Core. | None. |
| Remaining C24 | PARTIAL | COMPLETE | Project/time/notification cancellation interfaces and persistence, nested project/outbox calls and notification HTTP actions now propagate cancellation. Notification write responses use ApiResponse and identity failures use AppException. | None; legacy notification read actions now return HTTP 200 with the success envelope instead of 204. Current frontend accepts it. |
| Remaining C32 | PARTIAL | COMPLETE | Upload rollback removes the written file, failed writes create no metadata/activity; failed delete transaction keeps file/metadata; post-commit file-delete failure retains cleanup intent and retry succeeds. Local storage cleans a write that fails before returning its key; bad legacy cleanup keys do not block other items in the batch. | Existing durable FileCleanup records. Abrupt process termination between file creation and metadata commit is not an atomic filesystem/SQL transaction; see retention limitations below. |
| C38 server validation | COMPLETE | COMPLETE | FluentValidation rejects invalid task/project enums and merged date ordering; title-only edit and overdue completion tests pass. | None; broad UI enum consolidation remains outside Phase 2. |
| C47 / L06 | PARTIAL | COMPLETE | Explicit owner-only transfers serialized with role/removal operations; ordinary owner grants/demotions/removals forbidden. Concurrent project transfers retain exactly one matching OwnerId/member; team transfer/removal and two-owner role/removal cases retain owners. | No owner/version column required. |
| L05 | PARTIAL | COMPLETE | Concurrent API starts produce one success and one conflict. Direct SQL persistence also rejects a duplicate running row but accepts stopped history. | Existing filtered unique UserId index, EndTime IS NULL. |
| L09 | PARTIAL | COMPLETE | Full historical chain rehearsed; `0` and `1` rows fail semantic predicates before repair and match afterward. Unknown values block the migration; no applied-history entry is left behind. | Existing guarded numeric-string repair retained; no invented migration. |

## Failure classification and remaining boundaries

- A/B: recovered Phase 2 lint failures were unnecessary rethrow-only catch blocks; removed without swallowing backend errors. Found and completed owner-scoped slug lookup, unchanged deadline preservation, cancellation propagation and storage write cleanup. Added the missing SQL and frontend regression evidence.
- C: broad working-tree lint still has seven errors across six unrelated, pre-existing refactor files: `Navbar.jsx` (undefined/unused `idx`), `CreateTaskModal.jsx`, `TaskActivity.jsx`, `TaskDetailModal.jsx` (unfinished useCallback syntax), `TaskKanbanBoard.jsx` (unused catch variable), `TaskContext.jsx` (setState-in-effect). They do not block the current production import graph/build or scoped Phase 2 tests. They were not changed in this continuation.
- D: one final test invocation was denied access to the user NuGet.Config by the sandbox. It succeeded after the required escalation; no code workaround or configuration change was made.
- Archive is reversible retention, not deletion or a new write-lock policy. Permanent delete removes relational project descendants and queues private-file deletion; historical notification/audit/outbox records are not advertised as a general account-data erasure operation.
- File cleanup retries from its durable outbox on the existing worker interval. A legacy invalid key remains pending for operator reconciliation. Filesystem/SQL cannot share a physical transaction: hard process/host failure during the upload gap, or simultaneous loss of storage and DB during compensation, is not covered by the ordinary exception compensation guarantee. No claim of crash-proof upload journaling is made.
- General email/realtime outbox retry/claims remain the roadmap's Phase 9 work. Delivery failure leaves pending intent; it neither rolls back committed business state nor reports failed delivery as completed. No Phase 9 worker was added.
- SQL runtime verification is complete locally. Production history/data and real browser cross-tab smoke testing remain NOT VERIFIED, by design; neither was used to claim production deployment readiness.

## Final requested report

1. **Recovered work:** preserved existing mutation runner, outbox/cleanup foundation, repository extraction, token rotation, validators, presence-aware DTOs, task ordering, scoped schema, migration, SQL tests and auth regressions. Recovery baseline: 177 tests PASS.
2. **Completed findings:** C02, C11–C16, C19, remaining C24/C32, C38 server validation, C46/C47 transaction completion; L03/L05/L06/L09 within Phase 2.
3. **Remaining findings:** no open Phase 2 implementation blocker. Unrelated lint/refactor state and later-phase delivery work remain as explicitly described above.
4. **Atomic refresh:** SQL race PASS; exactly one successor, losing request 401 with no cookie; replacement insert rollback PASS. Winning session is not revoked by replay.
5. **Transactions:** rollback injection PASS for project, invitation, task status, comment, move, notification read and project/attachment deletion. After-commit network failure preserves committed state and pending intent.
6. **Direct context debt:** all six targeted services are free of direct IAppDbContext/EF references. Settings reuses useful existing repositories.
7. **Slug:** scope is workspace for team projects, owner for personal projects. Creation/update validation, SQL constraints and slug lookup agree. Both scopes tested on SQL.
8. **Delete/archive:** archive/restore retains descendants and attachments; permanent delete cascades relational children and queues file cleanup. SQL success and rollback cases PASS.
9. **Task mutation:** omitted values preserved, explicit null honored, title-only deadline timestamp preserved, overdue status allowed, enum/date rejection, authorized same-board deterministic moves, stale conflict and failed-move rollback PASS.
10. **Ownership:** SQL project transfer race, team transfer/removal race, two-owner normal-role/removal denial PASS. OwnerId and project Owner membership remain consistent.
11. **Active timer:** SQL concurrent-start and direct constraint tests PASS; one running timer per user, stopped history permitted.
12. **Attachment consistency:** exception compensation, metadata/delete rollback, pending cleanup and successful retry PASS. Legacy invalid-key batch isolation verified; crash gap documented above.
13. **ProjectType:** Personal=0, Team=1; current string conversion, Team sentinel/default retained. Both migration directories inspected and retained. Numeric-string 0/1 repair and unknown-value guard rehearsed using disposable data.
14. **Migrations:** created none during continuation. Recovered `20260925043942_AddMutationOutboxAndScopedUniqueness` and designer/snapshot retained. Reviewed changes: filtered timer uniqueness, scoped slug indexes/100-char bound, outbox table, guarded ProjectType repair. Preflight blocks duplicate active timers, oversized slugs and unknown/inconsistent ProjectType. Down requires resolving cross-scope duplicate slugs and draining/exporting pending outbox. No application database update. `migrations list --no-connect` reports 18 migrations; `has-pending-model-changes` reports none. Previous-session applied-history evidence remains historical, not reverified against a real app DB here.
15. **Backend build:** PASS, 0 warnings and 0 errors.
16. **Backend tests:** PASS, 189 passed, 0 failed, 0 skipped.
17. **SQL Phase 2 tests:** PASS, 29 passed, 0 failed, 0 skipped, including migrations/concurrency/failure injection on disposable LocalDB only.
18. **Frontend auth tests:** PASS, 8/8 with `node --test tests/authRefresh.test.js`.
19. **Vitest:** PASS, 13 tests across 4 files.
20. **Frontend build:** PASS; existing >500 kB bundle warning remains.
21. **Frontend lint:** Phase 2 changed-file lint PASS (10 files). Broad dirty-tree lint remains FAIL on seven unrelated errors detailed above; no claim of repository-wide lint PASS.
22. **Whitespace:** `git diff --check` PASS; additional scan of untracked backend/test source found zero trailing-whitespace findings.
23. **Secret scan:** PASS for the tracked-source plus new backend/frontend-test pattern scan (397 files). Thirteen redacted candidates were generated test inputs/variable references or localization password labels; no real credential found. Values were not printed. This is a source-pattern scan, not a history scan or credential-rotation audit.
24. **Exact files changed:** continuation list and complete Phase 2 file inventory below. Existing Phase 2 changes were continued incrementally.
25. **Unrelated state:** all 31 documentation deletions, other frontend refactor edits, `.agents/`, context hook files, roadmap/audit drafts, phase1 context note and `skills-lock.json` preserved. No staging, commit, push, stash, backup ref, history rewrite or Phase 3 work.
26. **Git status:** complete final `git status --short` snapshot below. Branch remains `feature/project-collaboration-dashboard-publish`.

## Exact files modified or added during this continuation

This list is distinct from the inherited Phase 2 changes and unrelated dirty state.

```text
backend/TaskHub.API/Controllers/NotificationController.cs
backend/TaskHub.Application/Repositories/Interfaces/IProjectRepository.cs
backend/TaskHub.Application/Services/AttachmentCleanupService.cs
backend/TaskHub.Application/Services/CommentService.cs
backend/TaskHub.Application/Services/NotificationService.cs
backend/TaskHub.Application/Services/ProjectService.cs
backend/TaskHub.Application/Services/TaskItemService.cs
backend/TaskHub.Infrastructure/Repositories/ProjectRepository.cs
backend/TaskHub.Infrastructure/Storage/LocalAttachmentStorage.cs
backend/TaskHub.Tests/Fixtures/Phase2SqlFactory.cs
backend/TaskHub.Tests/Integration/Phase2MigrationTests.cs
backend/TaskHub.Tests/Integration/Phase2RecoveryTests.cs
docs/ai/AUTH_CONFIGURATION.md
docs/ai/PHASE_2_IMPLEMENTATION_REPORT.md
frontend/src/components/tasks/TaskFormModal.jsx
frontend/src/pages/projects/tabs/ProjectSettingsTab.jsx
frontend/src/pages/projects/tabs/ProjectTasksBoard.jsx
frontend/src/pages/tasks/MyTasksPage.jsx
frontend/tests/authRefresh.test.js
frontend/tests/phase2ProjectSettings.test.jsx
```

## Complete Phase 2 file inventory (inherited plus continuation)

69 files; frontend files may also contain preserved earlier refactor hunks.

```text
backend/TaskHub.API/Controllers/NotificationController.cs
backend/TaskHub.API/Controllers/ProjectController.cs
backend/TaskHub.API/Controllers/ProjectMemberController.cs
backend/TaskHub.API/Controllers/TimeTrackingController.cs
backend/TaskHub.API/Program.cs
backend/TaskHub.API/Workers/AttachmentCleanupWorker.cs
backend/TaskHub.Application/DTOs/AppDtos.cs
backend/TaskHub.Application/Repositories/Interfaces/IAuditRepository.cs
backend/TaskHub.Application/Repositories/Interfaces/IMutationRunner.cs
backend/TaskHub.Application/Repositories/Interfaces/INotificationRepository.cs
backend/TaskHub.Application/Repositories/Interfaces/IOutboxRepository.cs
backend/TaskHub.Application/Repositories/Interfaces/IProjectRepository.cs
backend/TaskHub.Application/Repositories/Interfaces/IRefreshTokenRepository.cs
backend/TaskHub.Application/Repositories/Interfaces/ITaskItemRepository.cs
backend/TaskHub.Application/Repositories/Interfaces/ITimeTrackingRepository.cs
backend/TaskHub.Application/Services/AttachmentCleanupService.cs
backend/TaskHub.Application/Services/AuditService.cs
backend/TaskHub.Application/Services/AuthService.cs
backend/TaskHub.Application/Services/CommentService.cs
backend/TaskHub.Application/Services/DurableDelivery.cs
backend/TaskHub.Application/Services/EmailService.cs
backend/TaskHub.Application/Services/INotificationService.cs
backend/TaskHub.Application/Services/Interfaces/IProjectService.cs
backend/TaskHub.Application/Services/Interfaces/ITimeTrackingService.cs
backend/TaskHub.Application/Services/Interfaces/ITokenService.cs
backend/TaskHub.Application/Services/NotificationService.cs
backend/TaskHub.Application/Services/ProjectService.cs
backend/TaskHub.Application/Services/SettingsService.cs
backend/TaskHub.Application/Services/TaskItemService.cs
backend/TaskHub.Application/Services/TeamService.cs
backend/TaskHub.Application/Services/TimeTrackingService.cs
backend/TaskHub.Application/Services/TokenService.cs
backend/TaskHub.Application/Validators/Phase2MutationValidators.cs
backend/TaskHub.Application/Validators/ProjectValidators.cs
backend/TaskHub.Application/Validators/TaskValidators.cs
backend/TaskHub.Domain/Entities/OutboxMessage.cs
backend/TaskHub.Infrastructure/Data/AppDbContext.cs
backend/TaskHub.Infrastructure/Migrations/20260925043942_AddMutationOutboxAndScopedUniqueness.cs
backend/TaskHub.Infrastructure/Migrations/20260925043942_AddMutationOutboxAndScopedUniqueness.Designer.cs
backend/TaskHub.Infrastructure/Migrations/AppDbContextModelSnapshot.cs
backend/TaskHub.Infrastructure/Repositories/AuditRepository.cs
backend/TaskHub.Infrastructure/Repositories/MutationRunner.cs
backend/TaskHub.Infrastructure/Repositories/NotificationRepository.cs
backend/TaskHub.Infrastructure/Repositories/OutboxRepository.cs
backend/TaskHub.Infrastructure/Repositories/ProjectRepository.cs
backend/TaskHub.Infrastructure/Repositories/RefreshTokenRepository.cs
backend/TaskHub.Infrastructure/Repositories/TaskItemRepository.cs
backend/TaskHub.Infrastructure/Repositories/TimeTrackingRepository.cs
backend/TaskHub.Infrastructure/Storage/LocalAttachmentStorage.cs
backend/TaskHub.Tests/Fixtures/Phase2SqlFactory.cs
backend/TaskHub.Tests/Integration/Phase2MigrationTests.cs
backend/TaskHub.Tests/Integration/Phase2RecoveryTests.cs
backend/TaskHub.Tests/Integration/Phase2SqlMutationTests.cs
backend/TaskHub.Tests/Integration/Phase2TaskMutationTests.cs
backend/TaskHub.Tests/Services/ProjectBusinessRulesTests.cs
backend/TaskHub.Tests/Services/RealtimeAuthorizationTests.cs
backend/TaskHub.Tests/Services/TaskAssignmentBusinessRulesTests.cs
docs/ai/AUTH_CONFIGURATION.md
docs/ai/PHASE_2_IMPLEMENTATION_REPORT.md
frontend/src/api/authApi.js
frontend/src/api/axiosInstance.js
frontend/src/api/taskApi.js
frontend/src/components/tasks/TaskFormModal.jsx
frontend/src/pages/projects/tabs/ProjectSettingsTab.jsx
frontend/src/pages/projects/tabs/ProjectTasksBoard.jsx
frontend/src/pages/tasks/MyTasksPage.jsx
frontend/tests/authRefresh.test.js
frontend/tests/phase2Mutations.test.jsx
frontend/tests/phase2ProjectSettings.test.jsx
```

## Final git status --short

```text
 M backend/TaskHub.API/Controllers/NotificationController.cs
 M backend/TaskHub.API/Controllers/ProjectController.cs
 M backend/TaskHub.API/Controllers/ProjectMemberController.cs
 M backend/TaskHub.API/Controllers/TimeTrackingController.cs
 M backend/TaskHub.API/Program.cs
 M backend/TaskHub.Application/DTOs/AppDtos.cs
 M backend/TaskHub.Application/Repositories/Interfaces/IProjectRepository.cs
 M backend/TaskHub.Application/Repositories/Interfaces/IRefreshTokenRepository.cs
 M backend/TaskHub.Application/Repositories/Interfaces/ITaskItemRepository.cs
 M backend/TaskHub.Application/Repositories/Interfaces/ITimeTrackingRepository.cs
 M backend/TaskHub.Application/Services/AuditService.cs
 M backend/TaskHub.Application/Services/AuthService.cs
 M backend/TaskHub.Application/Services/CommentService.cs
 M backend/TaskHub.Application/Services/EmailService.cs
 M backend/TaskHub.Application/Services/INotificationService.cs
 M backend/TaskHub.Application/Services/Interfaces/IProjectService.cs
 M backend/TaskHub.Application/Services/Interfaces/ITimeTrackingService.cs
 M backend/TaskHub.Application/Services/Interfaces/ITokenService.cs
 M backend/TaskHub.Application/Services/NotificationService.cs
 M backend/TaskHub.Application/Services/ProjectService.cs
 M backend/TaskHub.Application/Services/SettingsService.cs
 M backend/TaskHub.Application/Services/TaskItemService.cs
 M backend/TaskHub.Application/Services/TeamService.cs
 M backend/TaskHub.Application/Services/TimeTrackingService.cs
 M backend/TaskHub.Application/Services/TokenService.cs
 M backend/TaskHub.Application/Validators/ProjectValidators.cs
 M backend/TaskHub.Application/Validators/TaskValidators.cs
 M backend/TaskHub.Infrastructure/Data/AppDbContext.cs
 M backend/TaskHub.Infrastructure/Migrations/AppDbContextModelSnapshot.cs
 M backend/TaskHub.Infrastructure/Repositories/ProjectRepository.cs
 M backend/TaskHub.Infrastructure/Repositories/RefreshTokenRepository.cs
 M backend/TaskHub.Infrastructure/Repositories/TaskItemRepository.cs
 M backend/TaskHub.Infrastructure/Repositories/TimeTrackingRepository.cs
 M backend/TaskHub.Infrastructure/Storage/LocalAttachmentStorage.cs
 M backend/TaskHub.Tests/Services/ProjectBusinessRulesTests.cs
 M backend/TaskHub.Tests/Services/RealtimeAuthorizationTests.cs
 M backend/TaskHub.Tests/Services/TaskAssignmentBusinessRulesTests.cs
 M docs/ai/AUTH_CONFIGURATION.md
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
 M frontend/src/api/authApi.js
 M frontend/src/api/axiosInstance.js
 M frontend/src/api/taskApi.js
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
 M frontend/src/stores/useCalendarStore.js
 M frontend/tests/authRefresh.test.js
?? .agents/
?? backend/TaskHub.API/Workers/
?? backend/TaskHub.Application/Repositories/Interfaces/IAuditRepository.cs
?? backend/TaskHub.Application/Repositories/Interfaces/IMutationRunner.cs
?? backend/TaskHub.Application/Repositories/Interfaces/INotificationRepository.cs
?? backend/TaskHub.Application/Repositories/Interfaces/IOutboxRepository.cs
?? backend/TaskHub.Application/Services/AttachmentCleanupService.cs
?? backend/TaskHub.Application/Services/DurableDelivery.cs
?? backend/TaskHub.Application/Validators/Phase2MutationValidators.cs
?? backend/TaskHub.Domain/Entities/OutboxMessage.cs
?? backend/TaskHub.Infrastructure/Migrations/20260925043942_AddMutationOutboxAndScopedUniqueness.Designer.cs
?? backend/TaskHub.Infrastructure/Migrations/20260925043942_AddMutationOutboxAndScopedUniqueness.cs
?? backend/TaskHub.Infrastructure/Repositories/AuditRepository.cs
?? backend/TaskHub.Infrastructure/Repositories/MutationRunner.cs
?? backend/TaskHub.Infrastructure/Repositories/NotificationRepository.cs
?? backend/TaskHub.Infrastructure/Repositories/OutboxRepository.cs
?? backend/TaskHub.Tests/Fixtures/Phase2SqlFactory.cs
?? backend/TaskHub.Tests/Integration/Phase2MigrationTests.cs
?? backend/TaskHub.Tests/Integration/Phase2RecoveryTests.cs
?? backend/TaskHub.Tests/Integration/Phase2SqlMutationTests.cs
?? backend/TaskHub.Tests/Integration/Phase2TaskMutationTests.cs
?? docs/ai/.phase1-task-context-check.txt
?? docs/ai/PHASE_2_IMPLEMENTATION_REPORT.md
?? docs/ai/TASKHUB_DEVELOPMENT_ROADMAP.md
?? docs/ai/TASKHUB_FULL_AUDIT_AND_ROADMAP.md
?? frontend/src/context/authState.js
?? frontend/src/context/taskState.js
?? frontend/src/context/themeState.js
?? frontend/tests/phase2Mutations.test.jsx
?? frontend/tests/phase2ProjectSettings.test.jsx
?? skills-lock.json
```
