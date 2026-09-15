using System.ComponentModel.DataAnnotations;

namespace TaskHub.Application.DTOs;

public class GoogleAuthDto
{
    [Required]
    public string Credential { get; set; } = null!;
}



