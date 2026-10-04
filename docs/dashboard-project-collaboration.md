# Dashboard and project collaboration

## Personal and Team scope

Dashboard statistics, task velocity, activity, and Upcoming use the same project hierarchy filter. Personal scope includes only Personal projects owned by the authenticated user. A Team task does not enter Personal scope because of task ownership or assignment. Team scope requires a Team ID and Team membership, then includes only projects bound to that Team that the user can access. Dashboard collection responses for activity and Upcoming are paged.

## Task velocity

Velocity reports tasks created and distinct tasks completed in each bucket. Completion time comes from a `TaskActivityLog` status transition into `Done`; a task completed twice within one bucket counts once in that bucket. A task completed again in a later bucket counts in that later bucket. Old completed tasks without a recorded transition cannot be assigned a historical completion time.

Buckets use UTC boundaries and contain zero counts when empty. Week is Monday through Sunday, seven daily buckets. Month is the current calendar month, one bucket per day. SixMonths is the current month and five previous calendar months, six monthly buckets. Year is January through December of the current calendar year, twelve monthly buckets. Events after the request time are excluded from the current bucket.

## Activity

The dashboard feed merges existing `ProjectActivityLog` and `TaskActivityLog` records, newest first with ID as a stable tie breaker. It includes project creation and member events, task creation, status, assignment, and comment events when recorded. The feed returns actor, event, entity, project, Team, display names, and creation time. Old operations without logs are not backfilled. Activity is fetched by scoped project and task queries and returned as `PagedResult<DashboardActivityDto>`.

Both task update and dedicated assignment flows record an assignment event only when the assignee changes. Repeating a status or assignment value does not add another activity entry. Comment and project member events are recorded by their services after the corresponding data is persisted.

The Project Detail Activity tab requests `/api/v1/projects/{projectId}/activity` in five-item pages. The service authorizes access to that project through `PermissionService` before the repository applies the project filter. A Project Guest can read this feed without gaining access to the wider Team dashboard.

## Upcoming

`DueDate` is used as a calendar deadline because existing task forms include date-only values. Upcoming includes incomplete tasks due from 00:00 UTC today through the end of the seventh following UTC day, expressed as `[today, today + 8 days)`. Overdue tasks, completed tasks, and deleted tasks are excluded. Results sort by due date, then task ID, and are paged. This preserves date-only deadlines due today even after midnight.

## Personal to Team conversion

Conversion is a dedicated one-way operation. Only the Personal owner can convert, and the owner must have access to the target Team under the existing Team project creation rule. The target Team must exist, the project slug must be free there, and existing assignees and project members must be valid in that Team. The project becomes Team bound and Team visible; its original owner receives the project Owner role. Board, list, task, comment, time entry, and history IDs remain intact. Task Team IDs are updated. Relational persistence uses one repository transaction. The UI asks for explicit confirmation.

The EF `ProjectType` sentinel is `Team`, matching the existing SQL default. This ensures a newly created `Personal` project is explicitly included in SQL inserts. This mapping correction does not change the database schema.

## Real-time access

The notification hub requires authentication. Joining Board, Task, Project, or Team groups calls `RealtimeAccessService`, which loads the resource through repositories and delegates access checks to `PermissionService` before joining. Task typing also checks access. Board presence names and avatars come from the user record, not client input. Clients rejoin Board and Task groups after reconnect; dashboard clients rejoin Team or Personal project groups. Project, member, task, and comment activity broadcasts occur only after activity persistence, with a small event payload that prompts the dashboard to refetch. Existing Board and Task events remain available after their mutations persist.

The Personal dashboard lists boards belonging to the user's Personal projects. Its board action opens Projects because creating a project supplies a default board; the legacy standalone board creation endpoint does not associate a board with a project. Dashboard requests discard stale responses after a scope or Team change.

Project board deep links route through Project Detail with the board ID in the query string. Project Detail selects that board and applies its Personal or Team controls, including Guest read-only controls. Standalone boards remain on the standalone board page.

## Existing legacy API debt

The pre-existing Board, Task, and most Project routes remain under unversioned `/api/...`; only the new dashboard aliases and conversion operation use `/api/v1/...`. Some legacy controllers still return manual error responses or use broad collection responses, and some older services still query `IAppDbContext` directly. These paths were not globally migrated as part of the dashboard and project collaboration work.
