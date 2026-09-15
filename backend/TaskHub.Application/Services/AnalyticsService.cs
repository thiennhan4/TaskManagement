using Microsoft.EntityFrameworkCore;
using TaskHub.Application.Data;
using TaskHub.Application.DTOs;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Domain.Entities;

namespace TaskHub.Application.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IAppDbContext _context;

    public AnalyticsService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<AnalyticsOverviewDto> GetOverviewAsync(Guid userId, Guid? boardId = null, int days = 30, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var from = now.AddDays(-days);

        // Base query — tasks owned by user or assigned to user
        var tasksQuery = _context.Tasks
            .AsNoTracking()
            .Where(t => !t.IsDeleted && (t.OwnerId == userId || t.AssignedToId == userId));

        if (boardId.HasValue)
            tasksQuery = tasksQuery.Where(t => t.List.BoardId == boardId.Value);

        var allTasks = await tasksQuery
            .Select(t => new
            {
                t.Id,
                t.Status,
                t.Priority,
                t.DueDate,
                t.CreatedAt,
                t.UpdatedAt
            })
            .ToListAsync(ct);

        var totalTasks = allTasks.Count;
        var completedTasks = allTasks.Count(t => t.Status == TaskItemStatus.Done);
        var overdueTasks = allTasks.Count(t => t.DueDate.HasValue && t.DueDate.Value < now && t.Status != TaskItemStatus.Done);
        var completionRate = totalTasks > 0 ? Math.Round((double)completedTasks / totalTasks * 100, 1) : 0;

        // Average completion time (for tasks completed in the period)
        var completedInPeriod = allTasks
            .Where(t => t.Status == TaskItemStatus.Done && t.UpdatedAt.HasValue && t.UpdatedAt >= from)
            .ToList();

        var avgCompletionDays = completedInPeriod.Count > 0
            ? Math.Round(completedInPeriod.Average(t => (t.UpdatedAt!.Value - t.CreatedAt).TotalDays), 1)
            : 0;

        // Time tracked
        var timeQuery = _context.TimeEntries.AsNoTracking()
            .Where(te => te.UserId == userId && te.EndTime != null && te.StartTime >= from);
        var totalTimeTracked = await timeQuery.SumAsync(te => te.DurationSeconds, ct);

        // Status distribution
        var statusDistribution = allTasks
            .GroupBy(t => t.Status.ToString())
            .Select(g => new TaskDistributionDto
            {
                Label = g.Key,
                Count = g.Count(),
                Percentage = totalTasks > 0 ? Math.Round((double)g.Count() / totalTasks * 100, 1) : 0
            }).ToList();

        // Priority distribution
        var priorityDistribution = allTasks
            .GroupBy(t => t.Priority.ToString())
            .Select(g => new TaskDistributionDto
            {
                Label = g.Key,
                Count = g.Count(),
                Percentage = totalTasks > 0 ? Math.Round((double)g.Count() / totalTasks * 100, 1) : 0
            }).ToList();

        // Velocity — tasks created vs completed per week (last N weeks)
        var weeks = Math.Min(days / 7, 12);
        var velocity = new List<VelocityPointDto>();
        for (int i = weeks - 1; i >= 0; i--)
        {
            var weekStart = now.AddDays(-(i + 1) * 7);
            var weekEnd = now.AddDays(-i * 7);
            var created = allTasks.Count(t => t.CreatedAt >= weekStart && t.CreatedAt < weekEnd);
            var completed = allTasks.Count(t => t.Status == TaskItemStatus.Done && t.UpdatedAt.HasValue
                && t.UpdatedAt >= weekStart && t.UpdatedAt < weekEnd);

            velocity.Add(new VelocityPointDto
            {
                Period = $"W{weeks - i}",
                Created = created,
                Completed = completed
            });
        }

        // Burndown — remaining incomplete tasks over last N days (sampled daily)
        var burndownDays = Math.Min(days, 30);
        var burndown = new List<BurndownPointDto>();
        var totalForBurndown = allTasks.Count(t => t.CreatedAt <= from);
        for (int i = 0; i <= burndownDays; i++)
        {
            var date = from.AddDays(i);
            var remaining = allTasks.Count(t => t.CreatedAt <= date
                && (t.Status != TaskItemStatus.Done || (t.UpdatedAt.HasValue && t.UpdatedAt > date)));
            var ideal = Math.Max(0, totalForBurndown - (int)((double)totalForBurndown / burndownDays * i));

            burndown.Add(new BurndownPointDto
            {
                Date = date.Date,
                Remaining = remaining,
                Ideal = ideal
            });
        }

        // Productivity heatmap — tasks completed by day-of-week and hour
        var heatmap = completedInPeriod
            .Where(t => t.UpdatedAt.HasValue)
            .GroupBy(t => new { DayOfWeek = (int)t.UpdatedAt!.Value.DayOfWeek, Hour = t.UpdatedAt.Value.Hour })
            .Select(g => new ProductivityDayDto
            {
                DayOfWeek = g.Key.DayOfWeek,
                Hour = g.Key.Hour,
                TasksCompleted = g.Count()
            }).ToList();

        return new AnalyticsOverviewDto
        {
            TotalTasks = totalTasks,
            CompletedTasks = completedTasks,
            OverdueTasks = overdueTasks,
            CompletionRate = completionRate,
            AvgCompletionDays = avgCompletionDays,
            TotalTimeTrackedSeconds = totalTimeTracked,
            StatusDistribution = statusDistribution,
            PriorityDistribution = priorityDistribution,
            Velocity = velocity,
            Burndown = burndown,
            ProductivityHeatmap = heatmap
        };
    }
}
