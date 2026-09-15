using System.ComponentModel.DataAnnotations;
using TaskHub.Domain.Entities;

namespace TaskHub.Application.DTOs;

public class ProjectMemberDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public ProjectRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
}

public class UpdateProjectMemberRoleDto
{
    public ProjectRole Role { get; set; }
}

public class InviteMemberDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    public ProjectRole Role { get; set; } = ProjectRole.Member;
}

public class AcceptInvitationDto
{
    [Required]
    public string Token { get; set; } = null!;
}



