using System.ComponentModel.DataAnnotations;

namespace TaskHub.backend.DTOs;

public class GoogleAuthDto
{
    [Required]
    public string Credential { get; set; } = null!;
}
