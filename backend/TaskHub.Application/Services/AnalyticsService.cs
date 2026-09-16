using TaskHub.Application.DTOs;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Domain.Entities;
using TaskHub.Domain.Exceptions;

namespace TaskHub.Application.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IDashboardService _dashboardService;
    private readonly IDashboardRepository _dashboardRepository;
    private readonly IBoardRepository _boardRepository;
    private readonly IPermissionService _permissionService;

    public AnalyticsService(
        IDashboardService dashboardService,
        IDashboardRepository dashboardRepository,
        IBoardRepository boardRepository,
        IPermissionService permissionService)
    {
        _dashboardService = dashboardService;
        _dashboardRepository = dashboardRepository;
        _boardRepository = boardRepository;
        _permissionService = permissionService;
    }

    public async Task<AnalyticsOverviewDto> GetOverviewAsync(
        Guid userId, DashboardQueryDto scope, Guid? boardId = null, int days = 30, CancellationToken ct = default)
    {
        if (days is < 1 or > 365)
        {
            throw new BadRequestException("days must be between 1 and 365.");
        }

        var criteria = await _dashboardService.ResolveScopeAsync(userId, scope, ct);
        if (boardId.HasValue)
        {
            var board = await _boardRepository.GetBoardByIdAsync(boardId.Value, ct)
                ?? throw new NotFoundException("Board", boardId.Value);
            await _permissionService.AuthorizeBoardActionAsync(userId, board, BoardAction.View, ct);
        }

        var now = DateTime.UtcNow;
        var from = now.AddDays(-days);
        var snapshot = await _dashboardRepository.GetAnalyticsSnapshotAsync(criteria, boardId, from, now, ct);
        var tasks = snapshot.Tasks;
        var total = tasks.Count;
        var done = tasks.Count(task => task.Status == TaskItemStatus.Done);
        var completions = snapshot.Completions;
        var latestCompletions = completions
            .GroupBy(completion => completion.TaskId)
            .Select(group => group.MaxBy(completion => completion.CompletedAt)!)
            .ToList();
        var taskById = tasks.ToDictionary(task => task.Id);
        var completionDurations = latestCompletions
            .Where(completion => taskById.TryGetValue(completion.TaskId, out var task) && task.Status == TaskItemStatus.Done)
            .Select(completion => Math.Max(0, (completion.CompletedAt - taskById[completion.TaskId].CreatedAt).TotalDays))
            .ToList();

        var velocity = (await _dashboardService.GetVelocityAsync(userId, scope, ct))
            .Select(bucket => new VelocityPointDto
            {
                Period = (bucket.End - bucket.Start).TotalDays == 1
                    ? bucket.Start.ToString("yyyy-MM-dd")
                    : bucket.Start.ToString("yyyy-MM"),
                Created = bucket.Created,
                Completed = bucket.Completed
            }).ToList();

        var burndownDays = Math.Min(days, 30);
        var completedInPeriodIds = latestCompletions.Select(completion => completion.TaskId).ToHashSet();
        var completedBeforePeriodIds = tasks
            .Where(task => task.Status == TaskItemStatus.Done && !completedInPeriodIds.Contains(task.Id))
            .Select(task => task.Id)
            .ToHashSet();
        var burndownCompletions = latestCompletions
            .Where(completion => taskById.TryGetValue(completion.TaskId, out var task) && task.Status == TaskItemStatus.Done)
            .ToList();
        var initial = tasks.Count(task => task.CreatedAt <= from && !completedBeforePeriodIds.Contains(task.Id));
        var burndown = Enumerable.Range(0, burndownDays + 1)
            .Select(index =>
            {
                var date = from.AddDays(index);
                var created = tasks.Count(task => task.CreatedAt <= date);
                var completed = tasks.Count(task => task.CreatedAt <= date && completedBeforePeriodIds.Contains(task.Id)) +
                    burndownCompletions.Count(completion => completion.CompletedAt <= date);
                return new BurndownPointDto
                {
                    Date = date.Date,
                    Remaining = Math.Max(0, created - completed),
                    Ideal = Math.Max(0, initial - (int)Math.Round((double)initial * index / burndownDays))
                };
            }).ToList();

        return new AnalyticsOverviewDto
        {
            TotalTasks = total,
            CompletedTasks = done,
            OverdueTasks = tasks.Count(task => task.DueDate < now && task.Status != TaskItemStatus.Done),
            CompletionRate = total == 0 ? 0 : Math.Round(100.0 * done / total, 1),
            AvgCompletionDays = completionDurations.Count == 0 ? 0 : Math.Round(completionDurations.Average(), 1),
            TotalTimeTrackedSeconds = snapshot.TimeTrackedSeconds,
            StatusDistribution = Distribution(tasks, task => task.Status.ToString()),
            PriorityDistribution = Distribution(tasks, task => task.Priority.ToString()),
            Velocity = velocity,
            Burndown = burndown,
            ProductivityHeatmap = completions
                .GroupBy(completion => new { Day = (int)completion.CompletedAt.DayOfWeek, completion.CompletedAt.Hour })
                .Select(group => new ProductivityDayDto
                {
                    DayOfWeek = group.Key.Day,
                    Hour = group.Key.Hour,
                    TasksCompleted = group.Count()
                }).ToList()
        };
    }

    private static List<TaskDistributionDto> Distribution(
        List<DashboardAnalyticsTaskDto> tasks, Func<DashboardAnalyticsTaskDto, string> label)
    {
        return tasks.GroupBy(label)
            .Select(group => new TaskDistributionDto
            {
                Label = group.Key,
                Count = group.Count(),
                Percentage = Math.Round(100.0 * group.Count() / tasks.Count, 1)
            }).ToList();
    }
}
