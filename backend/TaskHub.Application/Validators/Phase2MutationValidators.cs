using FluentValidation;
using TaskHub.Application.DTOs;
using TaskHub.Domain.Entities;
namespace TaskHub.Application.Validators;

public record TaskDates(DateTime? StartDate, DateTime? DueDate);
public sealed class TaskDatesValidator : AbstractValidator<TaskDates>
{
    public TaskDatesValidator() => RuleFor(x => x).Must(x => !x.StartDate.HasValue || !x.DueDate.HasValue || x.StartDate <= x.DueDate)
        .WithMessage("StartDate must be on or before DueDate.");
}
public sealed class MoveTaskDtoValidator : AbstractValidator<MoveTaskDto>
{
    public MoveTaskDtoValidator() { RuleFor(x => x.ListId).NotEmpty(); RuleFor(x => x.Position).GreaterThanOrEqualTo(0); }
}
public sealed class ChangeTaskStatusDtoValidator : AbstractValidator<ChangeTaskStatusDto>
{
    public ChangeTaskStatusDtoValidator() => RuleFor(x => x.NewStatus).IsInEnum();
}
public sealed class UpdateProjectDtoValidator : AbstractValidator<UpdateProjectDto>
{
    public UpdateProjectDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).When(x => x.Name != null);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(100).When(x => x.Slug != null);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.Visibility).IsInEnum().When(x => x.Visibility.HasValue);
    }
}
