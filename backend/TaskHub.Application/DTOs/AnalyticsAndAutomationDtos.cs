namespace TaskHub.Application.DTOs;

// ── Analytics DTOs ──

public class AnalyticsOverviewDto
{
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int OverdueTasks { get; set; }
    public double CompletionRate { get; set; }
    public double AvgCompletionDays { get; set; }
    public int TotalTimeTrackedSeconds { get; set; }
    public List<TaskDistributionDto> StatusDistribution { get; set; } = new();
    public List<TaskDistributionDto> PriorityDistribution { get; set; } = new();
    public List<VelocityPointDto> Velocity { get; set; } = new();
    public List<BurndownPointDto> Burndown { get; set; } = new();
    public List<ProductivityDayDto> ProductivityHeatmap { get; set; } = new();
}

public class TaskDistributionDto
{
    public string Label { get; set; } = null!;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class VelocityPointDto
{
    public string Period { get; set; } = null!;
    public int Completed { get; set; }
    public int Created { get; set; }
}

public class BurndownPointDto
{
    public DateTime Date { get; set; }
    public int Remaining { get; set; }
    public int Ideal { get; set; }
}

public class ProductivityDayDto
{
    public int DayOfWeek { get; set; }
    public int Hour { get; set; }
    public int TasksCompleted { get; set; }
}


