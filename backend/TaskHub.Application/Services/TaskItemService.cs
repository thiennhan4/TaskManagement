using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TaskHub.Application.Data;
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
    private readonly ITaskItemRepository _taskRepository;
    private readonly IBoardListRepository _listRepository;
    private readonly IBoardRepository _boardRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IPermissionService _permissionService;
    private readonly IAuditService _auditService;
    private readonly IAppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public TaskItemService(
        ITaskItemRepository taskRepository,
        IBoardListRepository listRepository,
        IBoardRepository boardRepository,
        IProjectRepository projectRepository,
        IPermissionService permissionService,
        IAuditService auditService,
        IAppDbContext context,
        IEmailService emailService,
        INotificationService notificationService,
        IHubContext<NotificationHub> hubContext)
    {
        _taskRepository = taskRepository;
        _listRepository = listRepository;
        _boardRepository = boardRepository;
        _projectRepository = projectRepository;
        _permissionService = permissionService;
        _auditService = auditService;
        _context = context;
        _emailService = emailService;
        _notificationService = notificationService;
        _hubContext = hubContext;
    }

    public async Task<PagedTaskResponseDto> GetTasksAsync(TaskFilterDto filter, Guid userId, CancellationToken ct = default)
    {
        var isAdmin = await _permissionService.IsAdminAsync(userId, ct);
        var (tasks, totalCount) = await _taskRepository.GetTasksAsync(filter, userId, isAdmin, ct);

        // Map to DTOs
        var taskDtos = tasks.Select(MapToDto).ToList();

        return new PagedTaskResponseDto
        {
            Tasks = taskDtos,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<List<TaskResponseDto>> GetTasksByListAsync(Guid listId, Guid userId, CancellationToken ct = default)
    {
        var list = await _listRepository.GetListByIdAsync(listId, ct)
            ?? throw new NotFoundException("BoardList", listId);

        var board = await _boardRepository.GetBoardByIdAsync(list.BoardId, ct)
            ?? throw new NotFoundException("Board", list.BoardId);

        await _permissionService.AuthorizeBoardActionAsync(userId, board, BoardAction.View, ct);

        var tasks = await _taskRepository.GetTasksByListIdAsync(listId, ct);
        return tasks.Select(MapToDto).ToList();
    }

    public async Task<TaskDetailResponseDto> GetTaskByIdAsync(Guid taskId, Guid userId, CancellationToken ct = default)
    {
        var authTask = await _taskRepository.GetTaskForAuthorizationAsync(taskId, ct)
            ?? throw new NotFoundException("Task", taskId);
        await _permissionService.AuthorizeTaskActionAsync(userId, authTask, TaskAction.View, ct);

        var task = await _taskRepository.GetByIdWithDetailsAsync(taskId, ct)
            ?? throw new NotFoundException("Task", taskId);

        return MapToDetailDto(task, userId);
    }

    public async Task<TaskResponseDto> CreateTaskAsync(Guid listId, CreateTaskDto dto, Guid userId, CancellationToken ct = default)
    {
        var list = await _listRepository.GetListByIdAsync(listId, ct)
            ?? throw new NotFoundException("BoardList", listId);

        var board = await _boardRepository.GetBoardByIdAsync(list.BoardId, ct)
            ?? throw new NotFoundException("Board", list.BoardId);

        await _permissionService.AuthorizeBoardActionAsync(userId, board, BoardAction.CreateTask, ct);
        var assignedToId = await NormalizeProjectTaskAssigneeAsync(board, dto.AssignedToId, userId, ct);
        var teamId = ResolveProjectTaskTeamId(board.Project, dto.TeamId);

        var maxPos = await _context.Tasks
            .Where(t => t.ListId == listId && !t.IsDeleted)
            .MaxAsync(t => (int?)t.Position, ct) ?? -1;

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
        task.Owner = await _context.Users.FindAsync(new object[] { userId }, ct) ?? null!;
        if (task.AssignedToId.HasValue)
        {
            task.AssignedTo = await _context.Users.FindAsync(new object[] { task.AssignedToId.Value }, ct);
        }

        var createdTaskDto = MapToDto(task);
        await _hubContext.Clients.Group($"board_{board.Id}").SendAsync("TaskCreated", createdTaskDto);

        return createdTaskDto;
    }

    public async Task<TaskResponseDto> CreatePersonalTaskAsync(CreateTaskDto dto, Guid userId, CancellationToken ct = default)
    {
        if (dto.AssignedToId.HasValue && dto.AssignedToId.Value != userId)
        {
            throw new BusinessValidationException("Personal tasks can only be assigned to their owner.");
        }

        dto.AssignedToId = userId;

        var board = await _context.Boards
            .FirstOrDefaultAsync(b => b.OwnerId == userId && b.Name == "Personal Tasks", ct);
        
        if (board == null)
        {
            board = new Board
            {
                Name = "Personal Tasks",
                Color = "#6366f1",
                OwnerId = userId
            };
            _context.Boards.Add(board);
            await _context.SaveChangesAsync(ct);
        }

        var list = await _context.Lists
            .FirstOrDefaultAsync(l => l.BoardId == board.Id && l.Name == "Todo", ct);

        if (list == null)
        {
            list = new BoardList
            {
                Name = "Todo",
                BoardId = board.Id,
                Position = 0
            };
            _context.Lists.Add(list);
            await _context.SaveChangesAsync(ct);
        }

        return await CreateTaskAsync(list.Id, dto, userId, ct);
    }

    public async Task<TaskResponseDto> UpdateTaskAsync(Guid taskId, UpdateTaskDto dto, Guid userId, CancellationToken ct = default)
    {
        var task = await GetTaskWithRelationsAsync(taskId, ct);
        await _permissionService.AuthorizeTaskActionAsync(userId, task, TaskAction.Update, ct);
        task.AssignedToId = await NormalizeProjectTaskAssigneeAsync(task.List.Board, dto.AssignedToId, userId, ct);

        // Track changes for logs
        if (task.Title != dto.Title)
            await LogActivityAsync(taskId, userId, ActivityLogAction.Updated, task.Title, dto.Title, ct);

        var previousStatus = task.Status;

        task.Title = dto.Title;
        task.Description = dto.Description;
        task.Status = dto.Status;
        task.Priority = dto.Priority;
        task.DueDate = dto.DueDate;
        task.StartDate = dto.StartDate;
        task.Label = dto.Label;
        task.Progress = dto.Progress;

        await _taskRepository.UpdateTaskAsync(task, ct);

        if (previousStatus != dto.Status)
            await LogActivityAsync(taskId, userId, ActivityLogAction.StatusChanged, previousStatus.ToString(), dto.Status.ToString(), ct);

        var updatedTaskDto = MapToDto(await GetTaskWithRelationsAsync(task.Id, ct));
        await _hubContext.Clients.Group($"board_{task.List.BoardId}").SendAsync("TaskUpdated", updatedTaskDto);

        return updatedTaskDto;
    }

    public async Task<TaskResponseDto> ChangeStatusAsync(Guid taskId, ChangeTaskStatusDto dto, Guid userId, CancellationToken ct = default)
    {
        var task = await GetTaskWithRelationsAsync(taskId, ct);
        await _permissionService.AuthorizeTaskActionAsync(userId, task, TaskAction.ChangeStatus, ct);
        var oldStatus = task.Status;
        
        task.Status = dto.NewStatus;
        await _taskRepository.UpdateTaskAsync(task, ct);

        if (oldStatus != dto.NewStatus)
            await LogActivityAsync(taskId, userId, ActivityLogAction.StatusChanged, oldStatus.ToString(), dto.NewStatus.ToString(), ct);

        var updatedTaskDto = MapToDto(task);
        await _hubContext.Clients.Group($"board_{task.List.BoardId}").SendAsync("TaskUpdated", updatedTaskDto);

        return updatedTaskDto;
    }

    public async Task<TaskResponseDto> AssignTaskAsync(Guid taskId, AssignTaskDto dto, Guid userId, CancellationToken ct = default)
    {
        var task = await GetTaskWithRelationsAsync(taskId, ct);
        await _permissionService.AuthorizeTaskActionAsync(userId, task, TaskAction.Assign, ct);
        var oldAssignee = task.AssignedToId;

        task.AssignedToId = await NormalizeProjectTaskAssigneeAsync(task.List.Board, dto.AssignedToUserId, userId, ct);
        await _taskRepository.UpdateTaskAsync(task, ct);

        var action = dto.AssignedToUserId.HasValue ? ActivityLogAction.Assigned : ActivityLogAction.Unassigned;
        await LogActivityAsync(taskId, userId, action, oldAssignee?.ToString(), dto.AssignedToUserId?.ToString(), ct);

        var updatedTaskDto = MapToDto(await GetTaskWithRelationsAsync(task.Id, ct));
        await _hubContext.Clients.Group($"board_{task.List.BoardId}").SendAsync("TaskUpdated", updatedTaskDto);

        return updatedTaskDto;
    }

    public async Task DeleteTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default)
    {
        var task = await GetTaskWithRelationsAsync(taskId, ct);
        await _permissionService.AuthorizeTaskActionAsync(userId, task, TaskAction.Delete, ct);
        if (task != null)
        {
            var boardId = task.List?.BoardId;
            await _taskRepository.DeleteTaskAsync(taskId, userId, ct);
            if (boardId.HasValue)
            {
                await _hubContext.Clients.Group($"board_{boardId.Value}").SendAsync("TaskDeleted", taskId);
            }
        }
    }

    public async Task<TaskResponseDto> MoveTaskAsync(Guid taskId, MoveTaskDto dto, Guid userId, CancellationToken ct = default)
    {
        var task = await GetTaskWithRelationsAsync(taskId, ct);
        await _permissionService.AuthorizeTaskActionAsync(userId, task, TaskAction.Update, ct);
        
        var targetList = await _listRepository.GetListByIdAsync(dto.ListId, ct)
            ?? throw new NotFoundException("BoardList", dto.ListId);
        var targetBoard = await _boardRepository.GetBoardByIdAsync(targetList.BoardId, ct)
            ?? throw new NotFoundException("Board", targetList.BoardId);
        await _permissionService.AuthorizeBoardActionAsync(userId, targetBoard, BoardAction.CreateTask, ct);

        task.ListId = dto.ListId;
        task.Position = dto.Position;
        await _taskRepository.UpdateTaskAsync(task, ct);

        var updatedTaskDto = MapToDto(await GetTaskWithRelationsAsync(task.Id, ct));
        await _hubContext.Clients.Group($"board_{task.List.BoardId}").SendAsync("TaskUpdated", updatedTaskDto);

        return updatedTaskDto;
    }

    public async Task<TaskResponseDto> UpdateProgressAsync(Guid taskId, UpdateProgressDto dto, Guid userId, CancellationToken ct = default)
    {
        var task = await GetTaskWithRelationsAsync(taskId, ct);
        await _permissionService.AuthorizeTaskActionAsync(userId, task, TaskAction.Update, ct);
        task.Progress = dto.Progress;
        await _taskRepository.UpdateTaskAsync(task, ct);

        var updatedTaskDto = MapToDto(task);
        await _hubContext.Clients.Group($"board_{task.List.BoardId}").SendAsync("TaskUpdated", updatedTaskDto);

        return updatedTaskDto;
    }

    public async Task<List<TaskResponseDto>> GetMyTasksAsync(Guid userId, CancellationToken ct = default)
    {
        var tasks = await _taskRepository.GetByUserIdAsync(userId, ct);
        return tasks.Select(MapToDto).ToList();
    }

    public async Task<List<TaskCalendarDto>> GetCalendarTasksAsync(CalendarFilterDto filter, Guid userId, CancellationToken ct = default)
    {
        var tasks = await _taskRepository.GetCalendarTasksAsync(filter.Start, filter.End, userId, filter.ProjectId, filter.BoardId, ct);
        
        return tasks.Select(t => new TaskCalendarDto
        {
            Id = t.Id,
            Title = t.Title,
            StartDate = t.StartDate,
            DueDate = t.DueDate,
            Status = t.Status,
            Priority = t.Priority,
            BoardName = t.List?.Board?.Name,
            ProjectName = t.List?.Board?.Project?.Name,
            Color = t.List?.Board?.Project?.Color ?? t.List?.Board?.Color
        }).ToList();
    }

    // Attachments
    public async Task<AttachmentResponseDto> UploadAttachmentAsync(Guid taskId, IFormFile file, Guid userId, CancellationToken ct = default)
    {
        if (file == null || file.Length == 0) throw new BusinessValidationException("File is empty.");
        if (file.Length > 10 * 1024 * 1024) throw new BusinessValidationException("File exceeds 10MB limit.");

        var hasAccess = await _taskRepository.CanUserAccessTaskAsync(taskId, userId, ct);
        if (!hasAccess) throw new ForbiddenException("You do not have access to this task.");

        var user = await _context.Users.FindAsync(new object[] { userId }, ct);

        var ext = Path.GetExtension(file.FileName);
        var allowedExts = new[] { ".pdf", ".doc", ".docx", ".jpg", ".png", ".txt" };
        if (!allowedExts.Contains(ext.ToLower())) throw new BusinessValidationException("Invalid file type.");

        var fileName = $"{Guid.NewGuid()}{ext}";
        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "tasks", taskId.ToString());
        Directory.CreateDirectory(uploadDir);

        var filePath = Path.Combine(uploadDir, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, ct);
        }

        var relativePath = $"/uploads/tasks/{taskId}/{fileName}";

        var attachment = new TaskAttachment
        {
            TaskId = taskId,
            UploadedByUserId = userId,
            FileName = file.FileName,
            FilePath = relativePath,
            FileSize = file.Length,
            ContentType = file.ContentType
        };

        _context.TaskAttachments.Add(attachment);
        await _context.SaveChangesAsync(ct);

        await LogActivityAsync(taskId, userId, ActivityLogAction.AttachmentAdded, null, file.FileName, ct);

        return new AttachmentResponseDto
        {
            Id = attachment.Id,
            FileName = attachment.FileName,
            FilePath = attachment.FilePath,
            FileSize = attachment.FileSize,
            ContentType = attachment.ContentType,
            UploadedByUserName = user!.FullName,
            UploadedAt = attachment.UploadedAt,
            FileUrl = attachment.FilePath,
            CanDelete = true
        };
    }

    public async Task<List<AttachmentResponseDto>> GetAttachmentsAsync(Guid taskId, Guid userId, CancellationToken ct = default)
    {
        var hasAccess = await _taskRepository.CanUserAccessTaskAsync(taskId, userId, ct);
        if (!hasAccess) throw new ForbiddenException("You do not have access to this task.");

        var attachments = await _context.TaskAttachments
            .Include(a => a.UploadedByUser)
            .Where(a => a.TaskId == taskId)
            .OrderByDescending(a => a.UploadedAt)
            .ToListAsync(ct);

        return attachments.Select(a => new AttachmentResponseDto
        {
            Id = a.Id,
            FileName = a.FileName,
            FilePath = a.FilePath,
            FileSize = a.FileSize,
            ContentType = a.ContentType,
            UploadedByUserName = a.UploadedByUser.FullName,
            UploadedAt = a.UploadedAt,
            FileUrl = a.FilePath,
            CanDelete = a.UploadedByUserId == userId
        }).ToList();
    }

    public async Task DeleteAttachmentAsync(Guid attachmentId, Guid userId, CancellationToken ct = default)
    {
        var attachment = await _context.TaskAttachments.Include(a => a.Task).FirstOrDefaultAsync(a => a.Id == attachmentId, ct)
            ?? throw new NotFoundException("Attachment", attachmentId);

        if (attachment.UploadedByUserId != userId && attachment.Task.OwnerId != userId && !await _permissionService.IsAdminAsync(userId, ct))
            throw new ForbiddenException("You cannot delete this attachment.");

        var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", attachment.FilePath.TrimStart('/'));
        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }

        _context.TaskAttachments.Remove(attachment);
        await _context.SaveChangesAsync(ct);

        await LogActivityAsync(attachment.TaskId, userId, ActivityLogAction.AttachmentRemoved, attachment.FileName, null, ct);
    }

    // Activity Logs
    public async Task<List<ActivityLogResponseDto>> GetActivityLogsAsync(Guid taskId, Guid userId, CancellationToken ct = default)
    {
        var hasAccess = await _taskRepository.CanUserAccessTaskAsync(taskId, userId, ct);
        if (!hasAccess) throw new ForbiddenException("You do not have access to this task.");

        var logs = await _context.TaskActivityLogs
            .Include(l => l.User)
            .Where(l => l.TaskId == taskId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(ct);

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
    public async Task InviteMemberToTaskAsync(Guid taskId, InviteTaskMemberDto dto, Guid currentUserId, CancellationToken ct = default)
    {
        var hasAccess = await _taskRepository.CanUserAccessTaskAsync(taskId, currentUserId, ct);
        if (!hasAccess && !await _permissionService.IsAdminAsync(currentUserId, ct))
            throw new ForbiddenException("You do not have permission to invite members to this task.");

        var task = await GetTaskWithRelationsAsync(taskId, ct);
        var targetEmail = dto.Email.Trim().ToLower();

        // Determine Workspace / Team
        Guid? workspaceId = task.TeamId;
        if (workspaceId == null)
        {
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == task.List.Board.ProjectId, ct);
            if (project != null) workspaceId = project.WorkspaceId;
        }

        // Block invite for personal tasks (no workspace)
        if (!workspaceId.HasValue)
        {
            throw new BusinessValidationException("Cannot invite members to a personal task. Please convert this to a team project first.");
        }

        var invitee = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == targetEmail, ct);

        bool isInWorkspace = false;
        if (invitee != null && workspaceId.HasValue)
        {
            isInWorkspace = await _context.TeamMembers.AnyAsync(tm => tm.TeamId == workspaceId.Value && tm.UserId == invitee.Id, ct);
        }

        if (invitee != null && isInWorkspace)
        {
            // Already in workspace, add to task directly
            task.AssignedToId = invitee.Id;
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

        _context.TaskInvitations.Add(invitation);
        await _context.SaveChangesAsync(ct);

        var inviteLink = $"http://localhost:5173/accept-task-invite?token={token}";
        var body = $"<p>You have been invited to collaborate on task: <b>{task.Title}</b></p>" +
                   $"<p>Click <a href='{inviteLink}'>here</a> to accept the invitation.</p>";

        await _emailService.SendEmailAsync(targetEmail, "Task Invitation", body);
    }

    public async Task AcceptTaskInvitationAsync(AcceptTaskInvitationDto dto, Guid currentUserId, CancellationToken ct = default)
    {
        var invitation = await _context.TaskInvitations
            .Include(i => i.Task)
            .FirstOrDefaultAsync(i => i.Token == dto.Token && !i.IsAccepted, ct)
            ?? throw new BusinessValidationException("Invalid or already accepted invitation token.");

        if (invitation.ExpiresAt < DateTime.UtcNow)
            throw new BusinessValidationException("Invitation has expired.");

        var user = await _context.Users.FindAsync(new object[] { currentUserId }, ct)
            ?? throw new NotFoundException("User", currentUserId);

        // Optional: Ensure the user's email matches the invitee email
        // if (user.Email.ToLower() != invitation.InviteeEmail.ToLower())
        //     throw new BusinessValidationException("This invitation is for a different email address.");

        var task = await GetTaskWithRelationsAsync(invitation.TaskId, ct);
        
        Guid? workspaceId = task.TeamId;
        if (workspaceId == null)
        {
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == task.List.Board.ProjectId, ct);
            if (project != null) workspaceId = project.WorkspaceId;
        }

        if (workspaceId.HasValue)
        {
            var isInWorkspace = await _context.TeamMembers.AnyAsync(tm => tm.TeamId == workspaceId.Value && tm.UserId == currentUserId, ct);
            if (!isInWorkspace)
            {
                _context.TeamMembers.Add(new TeamMember
                {
                    TeamId = workspaceId.Value,
                    UserId = currentUserId,
                    Role = TeamRole.Member
                });
            }
        }

        task.AssignedToId = currentUserId;
        invitation.IsAccepted = true;
        invitation.AcceptedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

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
        await _taskRepository.AddActivityLogAsync(new TaskActivityLog
        {
            TaskId = taskId,
            UserId = userId,
            Action = action,
            OldValue = oldVal,
            NewValue = newVal
        }, ct);
    }

    private async Task<TaskItem> GetTaskWithRelationsAsync(Guid taskId, CancellationToken ct)
    {
        return await _context.Tasks
            .Include(t => t.List)
                .ThenInclude(l => l.Board)
                    .ThenInclude(b => b.Project)
            .Include(t => t.Owner)
            .Include(t => t.AssignedTo)
            .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, ct)
            ?? throw new NotFoundException("Task", taskId);
    }

    private async Task<Guid?> NormalizeProjectTaskAssigneeAsync(Board board, Guid? requestedAssigneeId, Guid userId, CancellationToken ct)
    {
        var project = board.Project;
        if (project == null)
            return requestedAssigneeId;

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

    private static TaskResponseDto MapToDto(TaskItem task) => new()
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
        IsOverdue = task.DueDate.HasValue && task.DueDate.Value < DateTime.UtcNow && task.Status != TaskItemStatus.Done
    };

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
                FilePath = a.FilePath,
                FileSize = a.FileSize,
                ContentType = a.ContentType,
                UploadedByUserName = a.UploadedByUser.FullName,
                UploadedAt = a.UploadedAt,
                FileUrl = a.FilePath,
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





