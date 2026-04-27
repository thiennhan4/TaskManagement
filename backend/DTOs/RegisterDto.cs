using System.ComponentModel.DataAnnotations;

namespace TaskHub.backend.DTOs;

public class RegisterDto
{
    [Required, MinLength(2)]
    public string FullName { get; set; } = null!;

    [Required, EmailAddress]
    public string Email { get; set; } = null!;

    [Required, MinLength(6)]
    public string Password { get; set; } = null!;
}
