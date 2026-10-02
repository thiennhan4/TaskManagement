namespace TaskHub.Application.DTOs;

public class TimeEntryDto
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public string? TaskTitle { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int DurationSeconds { get; set; }
    public string? Description { get; set; }
    public bool IsBillable { get; set; }
    public bool IsRunning => EndTime == null;
    public DateTime CreatedAt { get; set; }
}

public class StartTimerDto
{
    public Guid TaskId { get; set; }
    public string? Description { get; set; }
    public bool IsBillable { get; set; }
}

public class StopTimerDto
{
    public string? Description { get; set; }
}

public class ManualTimeEntryDto
{
    public Guid TaskId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Description { get; set; }
    public bool IsBillable { get; set; }
}

public class TimeReportDto
{
    public int TotalTaskGroups { get; set; }
    public int TaskGroupPage { get; set; }
    public int TaskGroupPageSize { get; set; }
    public int TaskGroupTotalPages => TaskGroupPageSize == 0 ? 0 : (int)Math.Ceiling(TotalTaskGroups / (double)TaskGroupPageSize);
    public int TotalSeconds { get; set; }
    public int BillableSeconds { get; set; }
    public int NonBillableSeconds { get; set; }
    public int EntryCount { get; set; }
    public List<TimeReportGroupDto> ByTask { get; set; } = new();
    public List<TimeReportGroupDto> ByUser { get; set; } = new();
    public List<TimeReportDayDto> ByDay { get; set; } = new();
}

public class TimeReportGroupDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int TotalSeconds { get; set; }
    public int EntryCount { get; set; }
}

public class TimeReportDayDto
{
    public DateTime Date { get; set; }
    public int TotalSeconds { get; set; }
    public int EntryCount { get; set; }
}
