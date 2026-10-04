using FluentValidation;
using TaskHub.Application.DTOs;

namespace TaskHub.Application.Validators;

public class InviteMemberValidator : AbstractValidator<InviteMemberDto>
{
    public InviteMemberValidator() { RuleFor(x => x.Email).NotEmpty().EmailAddress(); RuleFor(x => x.Role).IsInEnum(); }
}
public class UpdateProjectMemberRoleValidator : AbstractValidator<UpdateProjectMemberRoleDto>
{
    public UpdateProjectMemberRoleValidator() { RuleFor(x => x.Role).IsInEnum(); }
}
public class AddTeamMemberValidator : AbstractValidator<AddTeamMemberDto>
{
    public AddTeamMemberValidator() { RuleFor(x => x.Email).NotEmpty().EmailAddress(); RuleFor(x => x.Role).IsInEnum(); }
}
public class ChangeTeamRoleValidator : AbstractValidator<ChangeTeamRoleDto>
{
    public ChangeTeamRoleValidator() { RuleFor(x => x.Role).IsInEnum(); }
}
