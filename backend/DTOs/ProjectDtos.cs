using System.ComponentModel.DataAnnotations;
using TaskHub.backend.Models;

namespace TaskHub.backend.DTOs;

public class CreateProjectDto
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(100)]
    public string? Slug { get; set; }

    public string? Description { get; set; }
    public string? Emoji { get; set; }
    public string? Color { get; set; }
    
    public Guid? WorkspaceId { get; set; }
    
    public ProjectVisibility Visibility { get; set; } = ProjectVisibility.Private;
    
    public int ProjectType { get; set; } = 1; // 1 = Personal, 2 = Team
}

public class UpdateProjectDto
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(100)]
    public string? Slug { get; set; }

    public string? Description { get; set; }
    public string? Emoji { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? Color { get; set; }
    public ProjectStatus? Status { get; set; }
    public ProjectVisibility? Visibility { get; set; }
}

public class ProjectResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
    public string? Emoji { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? Color { get; set; }
    public ProjectStatus Status { get; set; }
    public ProjectVisibility Visibility { get; set; }
    public int ProjectType { get; set; }
    public Guid OwnerId { get; set; }
    public string OwnerName { get; set; } = null!;
    public Guid? WorkspaceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public bool IsArchived { get; set; }
    
    public int MemberCount { get; set; }
    public int BoardCount { get; set; }
}
