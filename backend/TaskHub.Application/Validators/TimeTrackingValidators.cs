using FluentValidation;
using TaskHub.Application.DTOs;
namespace TaskHub.Application.Validators;
public class ManualTimeEntryValidator : AbstractValidator<ManualTimeEntryDto>
{
    public ManualTimeEntryValidator()
    {
        RuleFor(x=>x.TaskId).NotEmpty();
        RuleFor(x=>x.EndTime).GreaterThan(x=>x.StartTime);
        RuleFor(x=>x).Must(x=>(x.EndTime-x.StartTime).TotalSeconds <= int.MaxValue).WithMessage("Time range is too large.");
    }
}
public class StartTimerValidator : AbstractValidator<StartTimerDto>
{
    public StartTimerValidator() { RuleFor(x=>x.TaskId).NotEmpty(); }
}
