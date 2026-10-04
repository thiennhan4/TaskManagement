using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskHub.Domain.Entities;

/// <summary>
/// Tracks time spent on a task by a user — supports start/stop timer and manual entry.
/// </summary>
[Table("TimeEntries")]
public class TimeEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TaskId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>When the timer was started (UTC).</summary>
    public DateTime StartTime { get; set; }

    /// <summary>When the timer was stopped (UTC). Null while running.</summary>
    public DateTime? EndTime { get; set; }

    /// <summary>Total tracked duration in seconds. Computed on stop.</summary>
    public int DurationSeconds { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>Whether this time is billable.</summary>
    public bool IsBillable { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // ── Navigation ──
    public TaskItem Task { get; set; } = null!;
    public AppUser User { get; set; } = null!;
}
