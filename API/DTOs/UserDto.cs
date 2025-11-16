namespace API.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class UpdateUserDto
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

