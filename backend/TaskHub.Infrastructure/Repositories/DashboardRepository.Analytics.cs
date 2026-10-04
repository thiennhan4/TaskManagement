using Microsoft.EntityFrameworkCore;
using TaskHub.Application.DTOs;
using TaskHub.Domain.Entities;

namespace TaskHub.Infrastructure.Repositories;

public partial class DashboardRepository
{
    public async Task<AnalyticsOverviewDto> GetAnalyticsAsync(DashboardScopeCriteria criteria, Guid? boardId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var tasks = BuildTaskScope(BuildProjectScope(criteria));
        if (boardId.HasValue) tasks = tasks.Where(t => t.List.BoardId == boardId);
        var result = await tasks.GroupBy(t => 1).Select(g => new AnalyticsOverviewDto {
            TotalTasks=g.Count(), CompletedTasks=g.Count(t => t.Status == TaskItemStatus.Done),
            OverdueTasks=g.Count(t => t.DueDate < to && t.Status != TaskItemStatus.Done)
        }).SingleOrDefaultAsync(ct) ?? new();
        result.CompletionRate = result.TotalTasks == 0 ? 0 : Math.Round(100.0 * result.CompletedTasks / result.TotalTasks, 1);
        var statuses = await tasks.GroupBy(t => t.Status).Select(g => new { Key=g.Key, Count=g.Count() }).ToListAsync(ct);
        var priorities = await tasks.GroupBy(t => t.Priority).Select(g => new { Key=g.Key, Count=g.Count() }).ToListAsync(ct);
        result.StatusDistribution = statuses.Select(g => new TaskDistributionDto { Label=g.Key.ToString(), Count=g.Count, Percentage=Math.Round(100.0*g.Count/result.TotalTasks,1) }).ToList();
        result.PriorityDistribution = priorities.Select(g => new TaskDistributionDto { Label=g.Key.ToString(), Count=g.Count, Percentage=Math.Round(100.0*g.Count/result.TotalTasks,1) }).ToList();
        var logs = _context.TaskActivityLogs.AsNoTracking().Where(l => l.Action == ActivityLogAction.StatusChanged &&
            l.NewValue == nameof(TaskItemStatus.Done) && l.OldValue != nameof(TaskItemStatus.Done) &&
            l.CreatedAt >= from && l.CreatedAt < to && tasks.Any(t => t.Id == l.TaskId));
        var created = await tasks.Where(t => t.CreatedAt >= from && t.CreatedAt <= to).GroupBy(t => t.CreatedAt.Date)
            .Select(g => new { Date=g.Key, Count=g.Count() }).ToListAsync(ct);
        var completed = await logs.GroupBy(l => l.CreatedAt.Date)
            .Select(g => new { Date=g.Key, Count=g.Select(l => l.TaskId).Distinct().Count() }).ToListAsync(ct);
        var heat = await logs.GroupBy(l => new { Date=l.CreatedAt.Date, l.CreatedAt.Hour })
            .Select(g => new { g.Key.Date, g.Key.Hour, Count=g.Count() }).ToListAsync(ct);
        result.ProductivityHeatmap = heat.GroupBy(x => new { Day=(int)x.Date.DayOfWeek, x.Hour }).Select(g => new ProductivityDayDto {
            DayOfWeek=g.Key.Day, Hour=g.Key.Hour, TasksCompleted=g.Sum(x => x.Count)
        }).ToList();
        result.TotalTimeTrackedSeconds = await _context.TimeEntries.AsNoTracking().Where(e => e.EndTime.HasValue &&
            e.StartTime >= from && e.StartTime < to && tasks.Any(t => t.Id == e.TaskId)).SumAsync(e => e.DurationSeconds,ct);
        var latest = logs.GroupBy(l => l.TaskId).Select(g => new { TaskId=g.Key, Date=g.Max(l => l.CreatedAt) });
        var done = tasks.Where(t => t.Status == TaskItemStatus.Done).Join(latest,t => t.Id,l => l.TaskId,(t,l) => new { t.CreatedAt, CompletedAt=l.Date });
        if (_context.Database.IsRelational())
            result.AvgCompletionDays = Math.Round(await done.Select(x => x.CompletedAt < x.CreatedAt ? 0 : (double?)EF.Functions.DateDiffSecond(x.CreatedAt,x.CompletedAt)/86400.0).AverageAsync(ct) ?? 0,1);
        else
            result.AvgCompletionDays = Math.Round(await done.Select(x => (double?)Math.Max(0,(x.CompletedAt-x.CreatedAt).TotalDays)).AverageAsync(ct) ?? 0,1);
        // Preserve the rolling time-of-day cutoff and the existing 30-day burndown horizon.
        // Split each day at the cutoff in SQL; only bounded aggregate rows are materialized.
        var days = Math.Max(1,(int)Math.Ceiling((to-from).TotalDays));
        var burndownDays = Math.Min(days,30);
        var cutoff = from.TimeOfDay;
        var lastBurndown = from.AddDays(burndownDays);
        var initial = await tasks.CountAsync(t => t.CreatedAt <= from && (t.Status != TaskItemStatus.Done || logs.Any(l => l.TaskId == t.Id)),ct);
        var remainingCreated = await tasks.Where(t => t.CreatedAt > from && t.CreatedAt <= lastBurndown && (t.Status != TaskItemStatus.Done || logs.Any(l => l.TaskId == t.Id)))
            .GroupBy(t => new { Date=t.CreatedAt.Date, BeforeCutoff=t.CreatedAt.TimeOfDay <= cutoff })
            .Select(g => new { g.Key.Date, g.Key.BeforeCutoff, Count=g.Count() }).ToListAsync(ct);
        var doneDays = await done.Where(x => x.CompletedAt <= lastBurndown)
            .GroupBy(x => new { Date=x.CompletedAt.Date, BeforeCutoff=x.CompletedAt.TimeOfDay <= cutoff })
            .Select(g => new { g.Key.Date, g.Key.BeforeCutoff, Count=g.Count() }).ToListAsync(ct);
        for(var day=0;day<=days;day++)
        {
            var date=from.Date.AddDays(day);
            result.Velocity.Add(new VelocityPointDto { Period=date.ToString("yyyy-MM-dd"), Created=created.Where(x=>x.Date==date).Sum(x=>x.Count), Completed=completed.Where(x=>x.Date==date).Sum(x=>x.Count) });
            if (day <= burndownDays)
                result.Burndown.Add(new BurndownPointDto {
                    Date=date,
                    Remaining=Math.Max(0,initial+remainingCreated.Where(x=>x.Date.AddDays(x.BeforeCutoff?0:1)<=date).Sum(x=>x.Count)-doneDays.Where(x=>x.Date.AddDays(x.BeforeCutoff?0:1)<=date).Sum(x=>x.Count)),
                    Ideal=Math.Max(0,initial-(int)Math.Round((double)initial*day/burndownDays))
                });
        }
        return result;
    }
}
