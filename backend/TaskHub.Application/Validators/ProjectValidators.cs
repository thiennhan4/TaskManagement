using FluentValidation;
using TaskHub.Application.DTOs;
using TaskHub.Domain.Entities;

namespace TaskHub.Application.Validators;

public class CreateProjectDtoValidator : AbstractValidator<CreateProjectDto>
{
    public CreateProjectDtoValidator()
    {
        RuleFor(x => x.ProjectType).IsInEnum();
        RuleFor(x => x.Visibility).IsInEnum();
        RuleFor(x => x.Name)
            .MaximumLength(100)
            .When(x => x.Name != null);

        RuleFor(x => x.Slug)
            .MaximumLength(100)
            .When(x => x.Slug != null);

        RuleFor(x => x.WorkspaceId)
            .NotNull()
            .When(x => x.ProjectType == ProjectType.Team)
            .WithMessage("Team projects require a workspace.");
    }
}
