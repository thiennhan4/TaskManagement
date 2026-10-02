using FluentValidation;
using TaskHub.Application.Validators;
using Microsoft.AspNetCore.Http;


using TaskHub.Application.DTOs;
using TaskHub.Domain.Exceptions;
using TaskHub.Domain.Entities;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using TaskHub.Application.Hubs;

namespace TaskHub.Application.Services;

public class TaskItemService : ITaskItemService
{
    private readonly IMutationRunner _mutations;
    private readonly DurableDelivery _delivery;
    private readonly ITaskItemRepository _taskRepository;
    private readonly IBoardListRepository _listRepository;
    private readonly IBoardRepository _boardRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IPermissionService _permissionService;
    private readonly IAuditService _auditService;
    private readonly ITaskCollaborationRepository _collaboration;
    private readonly IUserRepository _users;
    private readonly ITeamRepository _teams;
    private readonly IAttachmentStorage _storage;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly IProtectedHubContext _hubContext;

    public TaskItemService(
        ITaskItemRepository taskRepository,
        IBoardListRepository listRepository,
        IBoardRepository boardRepository,
        IProjectRepository projectRepository,
        IPermissionService permissionService,
        IAuditService auditService,
        ITaskCollaborationRepository collaboration,
        IUserRepository users,
        ITeamRepository teams,
        IAttachmentStorage storage,
        IEmailService emailService,
        INotificationService notificationService,
        IProtectedHubContext hubContext, IMutationRunner mutations, DurableDelivery delivery)
    {
        _mutations = mutations;
        _delivery = delivery;
        _taskRepository = taskRepository;
        _listRepository = listRepository;
        _boardRepository = boardRepository;
        _projectRepository = projectRepository;
        _permissionService = permissionService;
        _auditService = auditService;
        _collaboration = collaboration;
        _users = users;
        _teams = teams;
        _storage = storage;
        _emailService = emailService;
        _notificationService = notificationService;
        _hubContext = hubContext;
    }

    public async Task<PagedTaskResponseDto> GetTasksAsync(TaskFilterDto filter, Guid userId, CancellationToken ct = default)
    {
        var page = await GetPageAsync(filter, userId, ct);
        return new PagedTaskResponseDto { Tasks=page.Items, TotalCount=page.TotalItems, Page=page.Page, PageSize=page.PageSize };
    }

    public async Task<PagedResult<TaskResponseDto>> GetPageAsync(TaskFilterDto filter, Guid userId, CancellationToken ct = default)
    {
        await new TaskFilterValidator().ValidateAndThrowAsync(filter, ct);
        if (filter.ListId.HasValue) {
            var list = await _listRepository.GetListByIdAsync(filter.ListId.Value, ct) ?? throw new NotFoundException("BoardList",filter.ListId.Value);
            var board = await _boardRepository.GetBoardByIdAsync(list.BoardId,ct) ?? throw new NotFoundException("Board",list.BoardId);
            await _permissionService.AuthorizeBoardActionAsync(userId,board,BoardAction.View,ct);
        }
        if (filter.BoardId.HasValue) {
            var board = await _boardRepository.GetBoardByIdAsync(filter.BoardId.Value,ct) ?? throw new NotFoundException("Board",filter.BoardId.Value);
            await _permissionService.AuthorizeBoardActionAsync(userId,board,BoardAction.View,ct);
        }
        return await _taskRepository.GetPageAsync(filter,userId,await _permissionService.IsAdminAsync(userId,ct),ct);
    }

    public async Task<TaskSummaryDto> GetSummaryAsync(Guid userId, CancellationToken ct = default) =>
        await _taskRepository.GetSummaryAsync(userId, await _permissionService.IsAdminAsync(userId, ct), ct);

    public async Task<PagedResult<TaskCalendarDto>> GetCalendarPageAsync(CalendarFilterDto filter, Guid userId, CancellationToken ct = default)
    {
        await new CalendarFilterValidator().ValidateAndThrowAsync(filter,ct);
        if (filter.ProjectId.HasValue) {
            var project = await _projectRepository.GetByIdAsync(filter.ProjectId.Value,ct) ?? throw new NotFoundException("Project",filter.ProjectId.Value);
            await _permissionService.AuthorizeProjectActionAsync(userId,project,ProjectAction.View,ct);
        }
        if (filter.BoardId.HasValue) {
            var board = await _boardRepository.GetBoardByIdAsync(filter.BoardId.Value,ct) ?? throw new NotFoundException("Board",filter.BoardId.Value);
            await _permissionService.AuthorizeBoardActionAsync(userId,board,BoardAction.View,ct);
        }
        return await _taskRepository.GetCalendarPageAsync(filter,userId,ct);
    }

    public async Task<List<TaskResponseDto>> GetTasksByListAsync(Guid listId, Guid userId, CancellationToken ct = default)
    {
        var list = await _listRepository.GetListByIdAsync(listId, ct)
            ?? throw new NotFoundException("BoardList", listId);

        var board = await _boardRepository.GetBoardByIdAsync(list.BoardId, ct)
            ?? throw new NotFoundException("Board", list.BoardId);

        await _permissionService.AuthorizeBoardActionAsync(userId, board, BoardAction.View, ct);

        var tasks = await _taskRepository.GetTasksByListIdAsync(listId, ct);
        var result = new List<TaskResponseDto>(); foreach (var task in tasks) result.Add(await MapToDtoAsync(task, ct)); return result;
    }

    public async Task<TaskDetailResponseDto> GetTaskByIdAsync(Guid taskId, Guid userId, CancellationToken ct = default)
    {
        var authTask = await _taskRepository.GetTaskForAuthorizationAsync(taskId, ct)
            ?? throw new NotFoundException("Task", taskId);
        await _permissionService.AuthorizeTaskActionAsync(userId, authTask, TaskAction.View, ct);

        var task = await _taskRepository.GetByIdWithDetailsAsync(taskId, ct)
            ?? throw new NotFoundException("Task", taskId);

        var detail = MapToDetailDto(task, userId);
        detail.Capabilities = new TaskCapabilitiesDto
        {
            CanEdit = await CanActAsync(userId, authTask, TaskAction.Update, ct),
            CanDelete = await CanActAsync(userId, authTask, TaskAction.Delete, ct),
            CanAssign = await CanActAsync(userId, authTask, TaskAction.Assign, ct),
            CanChangeStatus = await CanActAsync(userId, authTask, TaskAction.ChangeStatus, ct)
        };
        var counts = await _taskRepository.GetHeaderAsync(taskId, ct);
        detail.CommentsCount = counts!.CommentsCount;
        detail.AttachmentsCount = counts.AttachmentsCount;
        return detail;
    }

    private async Task<bool> CanActAsync(Guid userId, TaskItem task, TaskAction action, CancellationToken ct)
    {
        try { await _permissionService.AuthorizeTaskActionAsync(userId, task, action, ct); return true; }
        catch (ForbiddenException) { return false; }
    }

    public async Task<PagedResult<EligibleAssigneeDto>> GetEligibleAssigneesAsync(Guid taskId, Guid userId, PageQueryDto query, CancellationToken ct = default)
    {
        await new PageQueryValidator().ValidateAndThrowAsync(query, ct);
        var task = await _taskRepository.GetTaskForAuthorizationAsync(taskId, ct)
            ?? throw new NotFoundException("Task", taskId);
        await _permissionService.AuthorizeTaskActionAsync(userId, task, TaskAction.Assign, ct);
        return await _taskRepository.GetEligibleAssigneesAsync(task, query.Page, query.PageSize, ct);
    }

    public Task<TaskResponseDto> CreateTaskAsync(Guid listId, CreateTaskDto dto, Guid userId, CancellationToken ct = default) =>
        _mutations.RunAsync(() => CreateTaskAsyncCore(listId, dto, userId, ct), ct);

    private async Task<TaskResponseDto> CreateTaskAsyncCore(Guid listId, CreateTaskDto dto, Guid userId, CancellationToken ct = default)
    {
        var list = await _listRepository.GetListByIdAsync(listId, ct)
            ?? throw new NotFoundException("BoardList", listId);

        var board = await _boardRepository.GetBoardByIdAsync(list.BoardId, ct)
            ?? throw new NotFoundException("Board", list.BoardId);

        await _permissionService.AuthorizeBoardActionAsync(userId, board, BoardAction.CreateTask, ct);
        var assignedToId = await NormalizeProjectTaskAssigneeAsync(board, dto.AssignedToId, userId, ct, dto.TeamId);
        var teamId = ResolveProjectTaskTeamId(board.Project, dto.TeamId);

        var maxPos = await _collaboration.GetMaxPositionAsync(listId, ct);

        var task = new TaskItem
        {
            Title = dto.Title,
            Description = dto.Description,
            ListId = listId,
            OwnerId = userId,
            Position = maxPos + 1,
            Status = TaskItemStatus.Todo,
            Priority = dto.Priority ?? TaskItemPriority.Medium,
            DueDate = dto.DueDate,
            StartDate = dto.StartDate,
            Label = dto.Label,
            AssignedToId = assignedToId,
            TeamId = teamId,
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        };

        await _taskRepository.CreateTaskAsync(task, ct);
        await LogActivityAsync(task.Id, userId, ActivityLogAction.Created, null, null, ct);

        // Manually attach relations we already have to avoid re-querying
        task.List = list;
        task.Owner = await _users.GetByIdAsync(userId, ct) ?? null!;
        if (task.AssignedToId.HasValue)
        {
            task.AssignedTo = await _users.GetByIdAsync(task.AssignedToId.Value, ct);
        }

        var createdTaskDto = await MapToDtoAsync(task, ct);
        await _delivery.EnqueueAsync("Realtime", new { Group = $"board_{board.Id}", Event = "TaskCreated", Arguments = new object?[] {  createdTaskDto } }, () => _hubContext.Clients.Group($"board_{board.Id}").SendAsync("TaskCreated", createdTaskDto), ct);

        return createdTaskDto;
    }

    public Task<TaskResponseDto> CreatePersonalTaskAsync(CreateTaskDto dto, Guid userId, CancellationToken ct = default) =>
        _mutations.RunAsync(() => CreatePersonalTaskAsyncCore(dto, userId, ct), ct);

    private async Task<TaskResponseDto> CreatePersonalTaskAsyncCore(CreateTaskDto dto, Guid userId, CancellationToken ct = default)
    {
        if (dto.AssignedToId.HasValue && dto.AssignedToId.Value != userId)
        {
            throw new BusinessValidationException("Personal tasks can only be assigned to their owner.");
        }

        dto.AssignedToId = userId;

        var list = await _collaboration.EnsurePersonalListAsync(userId, ct);

        return await CreateTaskAsync(list.Id, dto, userId, ct);
    }

    public Task<TaskResponseDto> UpdateTaskAsync(Guid taskId, UpdateTaskDto dto, Guid userId, CancellationToken ct = default) =>
        _mutations.RunAsync(() => UpdateTaskAsyncCore(taskId, dto, userId, ct), ct);

    private async Task<TaskResponseDto> UpdateTaskAsyncCore(Guid taskId, UpdateTaskDto dto, Guid userId, CancellationToken ct = default)
    {
        var task = await GetTaskWithRelationsAsync(taskId, ct);
        await _permissionService.AuthorizeTaskActionAsync(userId, task, TaskAction.Update, ct);
        var previousAssignee = task.AssignedToId;
        if (dto.AssignmentSpecified)
        {
            var assignee = await NormalizeProjectTaskAssigneeAsync(task.List.Board, dto.AssignedToId, userId, ct, task.TeamId);
            if (assignee != task.AssignedToId)
                await _permissionService.AuthorizeTaskActionAsync(userId, task, TaskAction.Assign, ct);
            task.AssignedToId = assignee;
        }

        var previousTitle = task.Title;
        var previousStatus = task.Status;

        if (!new TaskDatesValidator().Validate(new TaskDates(
            dto.StartDateSpecified ? dto.StartDate : task.StartDate, dto.DueDateSpecified ? dto.DueDate : task.DueDate)).IsValid)
            throw new BusinessValidationException("StartDate must be on or before DueDate.");
        task.Title = dto.Title;
        if (dto.DescriptionSpecified) task.Description = dto.Description;
        if (dto.StatusSpecified) task.Status = dto.Status;
        if (dto.PrioritySpecified) task.Priority = dto.Priority;
        if (dto.DueDateSpecified) task.DueDate = dto.DueDate;
        if (dto.StartDateSpecified) task.StartDate = dto.StartDate;
        if (dto.LabelSpecified) task.Label = dto.Label;
        if (dto.ProgressSpecified) task.Progress = dto.Progress;

        await _taskRepository.UpdateTaskAsync(task, ct);

        if (previousTitle != dto.Title)
            await LogActivityAsync(taskId, userId, ActivityLogAction.Updated, previousTitle, dto.Title, ct);

        if (previousStatus != task.Status)
            await LogActivityAsync(taskId, userId, ActivityLogAction.StatusChanged, previousStatus.ToString(), task.Status.ToString(), ct);
        if (previousAssignee != task.AssignedToId)
            await LogActivityAsync(taskId, userId,
                task.AssignedToId.HasValue ? ActivityLogAction.Assigned : ActivityLogAction.Unassigned,
                previousAssignee?.ToString(), task.AssignedToId?.ToString(), ct);

        var updatedTaskDto = await MapToDtoAsync(task, ct);
        await _delivery.EnqueueAsync("Realtime", new { Group = $"board_{task.List.BoardId}", Event = "TaskUpdated", Arguments = new object?[] {  updatedTaskDto } }, () => _hubContext.Clients.Group($"board_{task.List.BoardId}").SendAsync("TaskUpdated", updatedTaskDto), ct);

        return updatedTaskDto;
    }

    public Task<TaskResponseDto> ChangeStatusAsync(Guid taskId, ChangeTaskStatusDto dto, Guid userId, CancellationToken ct = default) =>
        _mutations.RunAsync(() => ChangeStatusAsyncCore(taskId, dto, userId, ct), ct);

    private async Task<TaskResponseDto> ChangeStatusAsyncCore(Guid taskId, ChangeTaskStatusDto dto, Guid userId, CancellationToken ct = default)
    {
        var task = await GetTaskWithRelationsAsync(taskId, ct);
        await _permissionService.AuthorizeTaskActionAsync(userId, task, TaskAction.ChangeStatus, ct);
        var oldStatus = task.Status;
        
        task.Status = dto.NewStatus;
        await _taskRepository.UpdateTaskAsync(task, ct);

        if (oldStatus != dto.NewStatus)
            await LogActivityAsync(taskId, userId, ActivityLogAction.StatusChanged, oldStatus.ToString(), dto.NewStatus.ToString(), ct);

        var updatedTaskDto = await MapToDtoAsync(task, ct);
        await _delivery.EnqueueAsync("Realtime", new { Group = $"board_{task.List.BoardId}", Event = "TaskUpdated", Arguments = new object?[] {  updatedTaskDto } }, () => _hubContext.Clients.Group($"board_{task.List.BoardId}").SendAsync("TaskUpdated", updatedTaskDto), ct);

        return updatedTaskDto;
    }

    public Task<TaskResponseDto> AssignTaskAsync(Guid taskId, AssignTaskDto dto, Guid userId, CancellationToken ct = default) =>
        _mutations.RunAsync(() => AssignTaskAsyncCore(taskId, dto, userId, ct), ct);

    private async Task<TaskResponseDto> AssignTaskAsyncCore(Guid taskId, AssignTaskDto dto, Guid userId, CancellationToken ct = default)
    {
        var task = await GetTaskWithRelationsAsync(taskId, ct);
        await _permissionService.AuthorizeTaskActionAsync(userId, task, TaskAction.Assign, ct);
        var oldAssignee = task.AssignedToId;

        task.AssignedToId = await NormalizeProjectTaskAssigneeAsync(task.List.Board, dto.AssignedToUserId, userId, ct, task.TeamId);
        await _taskRepository.UpdateTaskAsync(task, ct);

        if (oldAssignee != task.AssignedToId)
        {
            var action = task.AssignedToId.HasValue ? ActivityLogAction.Assigned : ActivityLogAction.Unassigned;
            await LogActivityAsync(taskId, userId, action, oldAssignee?.ToString(), task.AssignedToId?.ToString(), ct);
        }

        var updatedTaskDto = await MapToDtoAsync(task, ct);
        await _delivery.EnqueueAsync("Realtime", new { Group = $"board_{task.List.BoardId}", Event = "TaskUpdated", Arguments = new object?[] {  updatedTaskDto } }, () => _hubContext.Clients.Group($"board_{task.List.BoardId}").SendAsync("TaskUpdated", updatedTaskDto), ct);

        return updatedTaskDto;
    }

    public Task DeleteTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default) =>
        _mutations.RunAsync(() => DeleteTaskAsyncCore(taskId, userId, ct), ct);

    private async Task DeleteTaskAsyncCore(Guid taskId, Guid userId, CancellationToken ct = default)
    {
        var task = await GetTaskWithRelationsAsync(taskId, ct);
        await _permissionService.AuthorizeTaskActionAsync(userId, task, TaskAction.Delete, ct);
        if (task != null)
        {
            var boardId = task.List?.BoardId;
            await _taskRepository.DeleteTaskAsync(taskId, userId, ct);
            if (boardId.HasValue)
            {
                await _delivery.EnqueueAsync("Realtime", new { Group = $"board_{boardId.Value}", Event = "TaskDeleted", Arguments = new object?[] {  taskId } }, () => _hubContext.Clients.Group($"board_{boardId.Value}").SendAsync("TaskDeleted", taskId), ct);
            }
        }
    }

    public Task<TaskResponseDto> MoveTaskAsync(Guid taskId, MoveTaskDto dto, Guid userId, CancellationToken ct = default) =>
        _mutations.RunAsync(() => MoveTaskAsyncCore(taskId, dto, userId, ct), ct);

    private async Task<TaskResponseDto> MoveTaskAsyncCore(Guid taskId, MoveTaskDto dto, Guid userId, CancellationToken ct = default)
    {
        var task = await GetTaskWithRelationsAsync(taskId, ct);
        await _permissionService.AuthorizeTaskActionAsync(userId, task, TaskAction.Update, ct);
        
        var targetList = await _listRepository.GetListByIdAsync(dto.ListId, ct)
            ?? throw new NotFoundException("BoardList", dto.ListId);
        var targetBoard = await _boardRepository.GetBoardByIdAsync(targetList.BoardId, ct)
            ?? throw new NotFoundException("Board", targetList.BoardId);
        await _permissionService.AuthorizeBoardActionAsync(userId, targetBoard, BoardAction.CreateTask, ct);

        if (targetList.BoardId != task.List.BoardId)
            throw new BusinessValidationException("Tasks can only move within the same board.");
        if (dto.ExpectedUpdatedAtSpecified && dto.ExpectedUpdatedAt != task.UpdatedAt)
            throw new ConflictException("Task changed. Reload before moving it.");
        if (dto.Position < 0) throw new BusinessValidationException("Position must be nonnegative.");
        await _taskRepository.MoveWithinBoardAsync(task, targetList, dto.Position, ct);

        var updatedTaskDto = await MapToDtoAsync(task, ct);
        await _delivery.EnqueueAsync("Realtime", new { Group = $"board_{task.List.BoardId}", Event = "TaskUpdated", Arguments = new object?[] {  updatedTaskDto } }, () => _hubContext.Clients.Group($"board_{task.List.BoardId}").SendAsync("TaskUpdated", updatedTaskDto), ct);

        return updatedTaskDto;
    }

    public Task<TaskResponseDto> UpdateProgressAsync(Guid taskId, UpdateProgressDto dto, Guid userId, CancellationToken ct = default) =>
        _mutations.RunAsync(() => UpdateProgressAsyncCore(taskId, dto, userId, ct), ct);

    private async Task<TaskResponseDto> UpdateProgressAsyncCore(Guid taskId, UpdateProgressDto dto, Guid userId, CancellationToken ct = default)
    {
        var task = await GetTaskWithRelationsAsync(taskId, ct);
        await _permissionService.AuthorizeTaskActionAsync(userId, task, TaskAction.Update, ct);
        task.Progress = dto.Progress;
        await _taskRepository.UpdateTaskAsync(task, ct);

        var updatedTaskDto = await MapToDtoAsync(task, ct);
        await _delivery.EnqueueAsync("Realtime", new { Group = $"board_{task.List.BoardId}", Event = "TaskUpdated", Arguments = new object?[] {  updatedTaskDto } }, () => _hubContext.Clients.Group($"board_{task.List.BoardId}").SendAsync("TaskUpdated", updatedTaskDto), ct);

        return updatedTaskDto;
    }

    public async Task<List<TaskResponseDto>> GetMyTasksAsync(Guid userId, CancellationToken ct = default)
    {
        return (await GetPageAsync(new TaskFilterDto(),userId,ct)).Items;
    }

    public async Task<List<TaskCalendarDto>> GetCalendarTasksAsync(CalendarFilterDto filter, Guid userId, CancellationToken ct = default)
    {
        return (await GetCalendarPageAsync(filter,userId,ct)).Items;
    }

    public async Task<TaskHub.Application.Models.AttachmentDownload> DownloadAttachmentAsync(Guid taskId, Guid attachmentId, Guid userId, CancellationToken ct = default)
    {
        var task = await GetTaskWithRelationsAsync(taskId, ct);
        await _permissionService.AuthorizeTaskActionAsync(userId, task, TaskAction.View, ct);
        var attachment = await _collaboration.GetAttachmentAsync(attachmentId, ct)
            ?? throw new NotFoundException("Attachment", attachmentId);
        if (attachment.TaskId != taskId) throw new NotFoundException("Attachment", attachmentId);
        var content = await _storage.OpenAsync(attachment.FilePath, ct);
        return new(content, Path.GetFileName(attachment.FileName), attachment.ContentType);
    }

    public Task<AttachmentResponseDto> UploadAttachmentAsync(Guid taskId, IFormFile file, Guid userId, CancellationToken ct = default) =>
        _mutations.RunAsync(() => UploadAttachmentAsyncCore(taskId, file, userId, ct), ct);

    private async Task<AttachmentResponseDto> UploadAttachmentAsyncCore(Guid taskId, IFormFile file, Guid userId, CancellationToken ct = default)
    {
        if (file == null || file.Length == 0) throw new BusinessValidationException("File is empty.");
        if (file.Length > 10 * 1024 * 1024) throw new BusinessValidationException("File exceeds 10MB limit.");

        var task = await GetTaskWithRelationsAsync(taskId, ct);
        await _permissionService.AuthorizeTaskActionAsync(userId, task, TaskAction.View, ct);

        var user = await _users.GetByIdAsync(userId, ct);

        await _permissionService.AuthorizeTaskActionAsync(userId, task, TaskAction.Update, ct);
        await using var input=file.OpenReadStream();
        var stored=await _storage.StoreAsync(input, file.FileName, ct);
        _mutations.OnRollback(async () =>
        {
            try { await _storage.DeleteAsync(stored.Key, CancellationToken.None); }
            catch { await _delivery.EnqueueAsync("FileCleanup", new { Key = stored.Key },
                () => _storage.DeleteAsync(stored.Key, CancellationToken.None), CancellationToken.None); }
        });

        var attachment = new TaskAttachment
        {
            TaskId = taskId,
            UploadedByUserId = userId,
            FileName = Path.GetFileName(file.FileName),
            FilePath = stored.Key,
            FileSize = stored.Length,
            ContentType = stored.ContentType
        };

        await _collaboration.AddAttachmentAsync(attachment, ct);

        await LogActivityAsync(taskId, userId, ActivityLogAction.AttachmentAdded, null, file.FileName, ct);

        return new AttachmentResponseDto
        {
            Id = attachment.Id,
            FileName = attachment.FileName,
            FilePath = AttachmentLinks.Download(attachment.TaskId, attachment.Id),
            FileSize = attachment.FileSize,
            ContentType = attachment.ContentType,
            UploadedByUserName = user!.FullName,
            UploadedAt = attachment.UploadedAt,
            FileUrl = AttachmentLinks.Download(attachment.TaskId, attachment.Id),
            CanDelete = true
        };
    }

    public async Task<List<AttachmentResponseDto>> GetAttachmentsAsync(Guid taskId, Guid userId, CancellationToken ct = default)
    {
        var task = await GetTaskWithRelationsAsync(taskId, ct);
        await _permissionService.AuthorizeTaskActionAsync(userId, task, TaskAction.View, ct);

        var attachments = await _collaboration.GetAttachmentsAsync(taskId, ct);

        var result = new List<AttachmentResponseDto>();
        foreach (var a in attachments)
        {
            var canDelete = false;
            try { await _permissionService.AuthorizeAttachmentDeleteAsync(userId, task, a.UploadedByUserId, ct); canDelete = true; }
            catch (ForbiddenException) { }
            result.Add(new AttachmentResponseDto
            {
            Id = a.Id,
            FileName = a.FileName,
            FilePath = AttachmentLinks.Download(a.TaskId, a.Id),
            FileSize = a.FileSize,
            ContentType = a.ContentType,
            UploadedByUserName = a.UploadedByUser.FullName,
            UploadedAt = a.UploadedAt,
            FileUrl = AttachmentLinks.Download(a.TaskId, a.Id),
            CanDelete = canDelete
            });
        }
        return result;
    }

    public Task DeleteAttachmentAsync(Guid attachmentId, Guid userId, CancellationToken ct = default, Guid? taskId = null) =>
        _mutations.RunAsync(() => DeleteAttachmentAsyncCore(attachmentId, userId, ct, taskId), ct);

    private async Task DeleteAttachmentAsyncCore(Guid attachmentId, Guid userId, CancellationToken ct = default, Guid? taskId = null)
    {
        var attachment = await _collaboration.GetAttachmentAsync(attachmentId, ct)
            ?? throw new NotFoundException("Attachment", attachmentId);

        if(taskId.HasValue && taskId.Value!=attachment.TaskId) throw new NotFoundException("Attachment",attachmentId);
        var task=await GetTaskWithRelationsAsync(attachment.TaskId,ct);
        await _permissionService.AuthorizeAttachmentDeleteAsync(userId,task,attachment.UploadedByUserId,ct);
        await _delivery.EnqueueAsync("FileCleanup", new { Key = attachment.FilePath }, () => _storage.DeleteAsync(attachment.FilePath, CancellationToken.None), ct);

        await _collaboration.DeleteAttachmentAsync(attachment, ct);

        await LogActivityAsync(attachment.TaskId, userId, ActivityLogAction.AttachmentRemoved, attachment.FileName, null, ct);
    }

    // Activity Logs
    public async Task<List<ActivityLogResponseDto>> GetActivityLogsAsync(Guid taskId, Guid userId, CancellationToken ct = default)
    {
        var task = await GetTaskWithRelationsAsync(taskId, ct);
        await _permissionService.AuthorizeTaskActionAsync(userId, task, TaskAction.View, ct);

        var logs = await _collaboration.GetActivityAsync(taskId, ct);

        return logs.Select(l => new ActivityLogResponseDto
        {
            Action = l.Action.ToString(),
            ActionDescription = l.Action.ToString(), // could map to human-readable based on action
            OldValue = l.OldValue,
            NewValue = l.NewValue,
            UserName = l.User.FullName,
            CreatedAt = l.CreatedAt
        }).ToList();
    }

    // Task Invitations
    public Task InviteMemberToTaskAsync(Guid taskId, InviteTaskMemberDto dto, Guid currentUserId, CancellationToken ct = default) =>
        _mutations.RunAsync(() => InviteMemberToTaskAsyncCore(taskId, dto, currentUserId, ct), ct);

    private async Task InviteMemberToTaskAsyncCore(Guid taskId, InviteTaskMemberDto dto, Guid currentUserId, CancellationToken ct = default)
    {
        var task = await GetTaskWithRelationsAsync(taskId, ct);
        await _permissionService.AuthorizeTaskActionAsync(currentUserId, task, TaskAction.Assign, ct);
        var targetEmail = dto.Email.Trim().ToLowerInvariant();

        Guid? workspaceId = task.TeamId ?? task.List.Board.Project?.WorkspaceId;

        // Block invite for personal tasks (no workspace)
        if (!workspaceId.HasValue)
        {
            throw new BusinessValidationException("Cannot invite members to a personal task. Please convert this to a team project first.");
        }

        var invitee = await _users.GetByEmailAsync(targetEmail, ct);

        bool isInWorkspace = false;
        if (invitee != null && workspaceId.HasValue)
        {
            isInWorkspace = await _teams.GetTeamMemberAsync(workspaceId.Value, invitee.Id, ct) is not null;
        }

        if (invitee != null && isInWorkspace)
        {
            // Already in workspace, add to task directly
            task.AssignedToId = await NormalizeProjectTaskAssigneeAsync(task.List.Board, invitee.Id, currentUserId, ct, task.TeamId);
            await _taskRepository.UpdateTaskAsync(task, ct);

            await LogActivityAsync(taskId, currentUserId, ActivityLogAction.Assigned, null, invitee.Id.ToString(), ct);

            await _notificationService.CreateNotificationAsync(
                invitee.Id,
                "Assigned to Task",
                $"You have been assigned to task: {task.Title}",
                $"/tasks/{taskId}"
            );
            return;
        }

        // Create Invitation
        var token = Guid.NewGuid().ToString("N");
        var invitation = new TaskInvitation
        {
            TaskId = taskId,
            InviteeEmail = targetEmail,
            InvitedByUserId = currentUserId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await _collaboration.AddInvitationAsync(invitation, ct);

        var inviteLink = $"http://localhost:5173/accept-task-invite?token={token}";
        var body = $"<p>You have been invited to collaborate on task: <b>{task.Title}</b></p>" +
                   $"<p>Click <a href='{inviteLink}'>here</a> to accept the invitation.</p>";

        await _delivery.EnqueueAsync("Email", new object?[] { targetEmail, "Task Invitation", body }, () => _emailService.SendEmailAsync(targetEmail, "Task Invitation", body), ct);
    }

    public Task AcceptTaskInvitationAsync(AcceptTaskInvitationDto dto, Guid currentUserId, CancellationToken ct = default) =>
        _mutations.RunAsync(() => AcceptTaskInvitationAsyncCore(dto, currentUserId, ct), ct);

    private async Task AcceptTaskInvitationAsyncCore(AcceptTaskInvitationDto dto, Guid currentUserId, CancellationToken ct = default)
    {
        var invitation = await _collaboration.GetInvitationAsync(dto.Token, ct)
            ?? throw new BusinessValidationException("Invalid or already accepted invitation token.");

        if (invitation.ExpiresAt < DateTime.UtcNow)
            throw new BusinessValidationException("Invitation has expired.");

        var user = await _users.GetByIdAsync(currentUserId, ct)
            ?? throw new NotFoundException("User", currentUserId);

        if (!user.IsActive) throw new ForbiddenException("Inactive users cannot accept invitations.");

        if (!string.Equals(user.Email, invitation.InviteeEmail, StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenException("This invitation belongs to another recipient.");
        var task = await GetTaskWithRelationsAsync(invitation.TaskId, ct);
        await _permissionService.AuthorizeTaskActionAsync(invitation.InvitedByUserId, task, TaskAction.Assign, ct);
        if (task.List.Board.Project?.ProjectType == ProjectType.Personal || (!task.TeamId.HasValue && task.List.Board.Project == null))
            throw new BusinessValidationException("Personal tasks do not support invitations.");
        await _permissionService.AuthorizeTaskActionAsync(currentUserId, task, TaskAction.View, ct);
        task.AssignedToId = await NormalizeProjectTaskAssigneeAsync(task.List.Board, currentUserId, invitation.InvitedByUserId, ct, task.TeamId);
        invitation.IsAccepted = true;
        invitation.AcceptedAt = DateTime.UtcNow;

        await _collaboration.SaveAsync(ct);

        await LogActivityAsync(task.Id, currentUserId, ActivityLogAction.Assigned, null, currentUserId.ToString(), ct);

        await _notificationService.CreateNotificationAsync(
            invitation.InvitedByUserId,
            "Invitation Accepted",
            $"{user.FullName} accepted your invitation and joined task: {task.Title}",
            $"/tasks/{task.Id}"
        );
    }

    // â•â•â• PRIVATE HELPERS â•â•â•

    private async Task LogActivityAsync(Guid taskId, Guid userId, ActivityLogAction action, string? oldVal = null, string? newVal = null, CancellationToken ct = default)
    {
        var log = new TaskActivityLog
        {
            TaskId = taskId,
            UserId = userId,
            Action = action,
            OldValue = oldVal,
            NewValue = newVal
        };
        await _taskRepository.AddActivityLogAsync(log, ct);

        var task = await _taskRepository.GetTaskForAuthorizationAsync(taskId, ct);
        var project = task?.List?.Board?.Project;
        if (project == null) return;
        var payload = new ProjectActivityEventDto
        {
            ProjectId = project.Id,
            EventType = $"Task{action}",
            CreatedAt = log.CreatedAt
        };
        await _delivery.EnqueueAsync("Realtime", new { Group = $"project_{project.Id}", Event = "ProjectActivity", Arguments = new object?[] {  payload } }, () => _hubContext.Clients.Group($"project_{project.Id}").SendAsync("ProjectActivity", payload, ct), ct);
        if (project.WorkspaceId is Guid teamId)
            await _delivery.EnqueueAsync("Realtime", new { Group = $"team_{teamId}", Event = "ProjectActivity", Arguments = new object?[] {  payload } }, () => _hubContext.Clients.Group($"team_{teamId}").SendAsync("ProjectActivity", payload, ct), ct);
    }

    private async Task<TaskItem> GetTaskWithRelationsAsync(Guid taskId, CancellationToken ct)
    {
        return await _taskRepository.GetByIdWithDetailsAsync(taskId, ct) ?? throw new NotFoundException("Task", taskId);
    }

    private async Task<Guid?> NormalizeProjectTaskAssigneeAsync(Board board, Guid? requestedAssigneeId, Guid userId, CancellationToken ct, Guid? legacyTeamId = null)
    {
        var project = board.Project;
        if (project == null)
        {
            if (requestedAssigneeId.HasValue)
            {
                var recipient = await _users.GetByIdAsync(requestedAssigneeId.Value, ct);
                if (recipient is not { IsActive: true })
                    throw new BusinessValidationException("Task assignee must be an active user.");
                if (legacyTeamId.HasValue && await _teams.GetTeamMemberAsync(legacyTeamId.Value, recipient.Id, ct) == null)
                    throw new BusinessValidationException("Task assignee must be a team member.");
                if (!legacyTeamId.HasValue && recipient.Id != board.OwnerId)
                    throw new BusinessValidationException("Standalone tasks can only be assigned to the board owner.");
            }
            return requestedAssigneeId;
        }

        if (project.ProjectType == ProjectType.Personal)
        {
            if (requestedAssigneeId.HasValue && requestedAssigneeId.Value != project.OwnerId)
            {
                throw new BusinessValidationException("Personal project tasks can only be assigned to the project owner.");
            }

            return project.OwnerId;
        }

        if (!project.WorkspaceId.HasValue)
        {
            throw new BusinessValidationException("Team project tasks require a project workspace.");
        }

        if (!requestedAssigneeId.HasValue)
            return null;

        if (!await _projectRepository.IsUserEligibleProjectAssigneeAsync(project.Id, requestedAssigneeId.Value, ct))
        {
            throw new BusinessValidationException("Task assignee must be a project or workspace member.");
        }

        return requestedAssigneeId;
    }

    private static Guid? ResolveProjectTaskTeamId(Project? project, Guid? requestedTeamId)
    {
        if (project == null)
            return requestedTeamId;

        return project.ProjectType == ProjectType.Team ? project.WorkspaceId : null;
    }

    private async Task<TaskResponseDto> MapToDtoAsync(TaskItem task, CancellationToken ct) =>
        await _taskRepository.GetHeaderAsync(task.Id, ct) ?? throw new NotFoundException("Task", task.Id);
    private static TaskDetailResponseDto MapToDetailDto(TaskItem task, Guid currentUserId)
    {
        var dto = new TaskDetailResponseDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            Priority = task.Priority,
            Label = task.Label,
            Position = task.Position,
            Progress = task.Progress,
            DueDate = task.DueDate,
            StartDate = task.StartDate,
            ListId = task.ListId,
            ListName = task.List?.Name,
            BoardId = task.List?.BoardId,
            BoardName = task.List?.Board?.Name,
            OwnerId = task.OwnerId,
            OwnerName = task.Owner?.FullName,
            AssignedToId = task.AssignedToId,
            AssignedToName = task.AssignedTo?.FullName,
            TeamId = task.TeamId,
            WorkspaceId = task.TeamId ?? task.List?.Board?.Project?.WorkspaceId,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            CommentsCount = task.Comments?.Count ?? 0,
            AttachmentsCount = task.Attachments?.Count ?? 0,
            IsOverdue = task.DueDate.HasValue && task.DueDate.Value < DateTime.UtcNow && task.Status != TaskItemStatus.Done,
            
            Comments = task.Comments?.Select(c => new CommentResponseDto
            {
                Id = c.Id,
                Content = c.Content,
                UserId = c.UserId,
                UserName = c.User.FullName,
                UserAvatar = c.User.AvatarUrl,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                IsOwner = c.UserId == currentUserId
            }).ToList() ?? new(),
            
            Attachments = task.Attachments?.Select(a => new AttachmentResponseDto
            {
                Id = a.Id,
                FileName = a.FileName,
                FilePath = AttachmentLinks.Download(a.TaskId, a.Id),
                FileSize = a.FileSize,
                ContentType = a.ContentType,
                UploadedByUserName = a.UploadedByUser.FullName,
                UploadedAt = a.UploadedAt,
                FileUrl = AttachmentLinks.Download(a.TaskId, a.Id),
                CanDelete = a.UploadedByUserId == currentUserId || task.OwnerId == currentUserId
            }).ToList() ?? new(),

            ActivityLogs = task.ActivityLogs?.Select(l => new ActivityLogResponseDto
            {
                Action = l.Action.ToString(),
                ActionDescription = l.Action.ToString(),
                OldValue = l.OldValue,
                NewValue = l.NewValue,
                UserName = l.User.FullName,
                CreatedAt = l.CreatedAt
            }).ToList() ?? new()
        };

        return dto;
    }
}




