using FluentValidation;
using TaskHub.Application.DTOs;

namespace TaskHub.Application.Validators;

public sealed class TaskFilterValidator : AbstractValidator<TaskFilterDto>
{
    public TaskFilterValidator()
    {
        RuleFor(x => x.Page).InclusiveBetween(1, 1000000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.Priority).IsInEnum().When(x => x.Priority.HasValue);
        RuleFor(x => x.SearchKeyword).MaximumLength(200);
        RuleFor(x => x.SortBy).Must(x => x == null || new[] { "createdat", "duedate", "priority", "title" }.Contains(x.ToLowerInvariant()));
        RuleFor(x => x.SortOrder).Must(x => x == null || x.Equals("asc", StringComparison.OrdinalIgnoreCase) || x.Equals("desc", StringComparison.OrdinalIgnoreCase));
    }
}
public sealed class CalendarFilterValidator : AbstractValidator<CalendarFilterDto>
{
    public CalendarFilterValidator()
    {
        Include(new PageQueryValidator());
        RuleFor(x => x.Start).NotEmpty();
        RuleFor(x => x.End).NotEmpty();
        RuleFor(x => x).Must(x => x.End >= x.Start && x.End - x.Start <= TimeSpan.FromDays(62))
            .WithMessage("Calendar range must be ordered and at most 62 days.");
    }
}
public sealed class TimeQueryValidator : AbstractValidator<TimeQueryDto>
{
    public TimeQueryValidator()
    {
        Include(new PageQueryValidator());
        RuleFor(x => x.From).NotEmpty();
        RuleFor(x => x.To).NotEmpty();
        RuleFor(x => x).Must(x => x.To >= x.From && x.To - x.From <= TimeSpan.FromDays(366))
            .WithMessage("Time range must be ordered and at most 366 days.");
    }
}
public sealed class ProjectQueryValidator : AbstractValidator<ProjectQueryDto>
{
    public ProjectQueryValidator()
    {
        Include(new PageQueryValidator());
        RuleFor(x => x.Search).MaximumLength(200);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
    }
}
public sealed class NotificationQueryValidator : AbstractValidator<NotificationQueryDto>
{
    public NotificationQueryValidator() => Include(new PageQueryValidator());
}
