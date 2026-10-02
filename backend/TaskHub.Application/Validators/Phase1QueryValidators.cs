using FluentValidation;
using TaskHub.Application.DTOs;

namespace TaskHub.Application.Validators;

public class PageQueryValidator : AbstractValidator<PageQueryDto>
{
    public PageQueryValidator()
    {
        RuleFor(x => x.Page).InclusiveBetween(1, 1000000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
public class KanbanQueryValidator : AbstractValidator<KanbanQueryDto>
{
    public KanbanQueryValidator()
    {
        Include(new PageQueryValidator());
        RuleFor(x => x.TaskPage).InclusiveBetween(1, 1000000);
        RuleFor(x => x.TaskPageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SearchKeyword).MaximumLength(200);
        RuleFor(x => x.BoardId).NotEqual(Guid.Empty).When(x => x.BoardId.HasValue);
        RuleFor(x => x.ListId).NotEqual(Guid.Empty).When(x => x.ListId.HasValue);
    }
}

public class TransferOwnershipValidator : AbstractValidator<TransferOwnershipDto> { public TransferOwnershipValidator() { RuleFor(x=>x.TargetUserId).NotEmpty(); } }
