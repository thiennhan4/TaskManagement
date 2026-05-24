using System.ComponentModel.DataAnnotations;
using TaskHub.backend.Models;

namespace TaskHub.backend.DTOs;

// ── Team Request DTOs ──

public class CreateTeamDto
{
    [Required, MinLength(2)]
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

public class UpdateTeamDto
{
    [Required, MinLength(2)]
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

public class AddTeamMemberDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = null!;
    public TeamRole Role { get; set; } = TeamRole.Member;
}

public class ChangeTeamRoleDto
{
    [Required]
    public TeamRole Role { get; set; }
}

// ── Team Response DTOs ──

public class TeamResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public Guid CreatedById { get; set; }
    public string CreatedByName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public int MemberCount { get; set; }
    public TeamRole? CurrentUserRole { get; set; }
}

public class TeamDetailResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public Guid CreatedById { get; set; }
    public string CreatedByName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public List<TeamMemberResponseDto> Members { get; set; } = new();
}

public class TeamMemberResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public TeamRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
}
