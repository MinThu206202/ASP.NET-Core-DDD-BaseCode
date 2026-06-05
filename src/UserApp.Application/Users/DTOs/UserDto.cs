namespace UserApp.Application.Users.DTOs;

public record UserDto(Guid Id, string Email, string FullName, string Status, DateTime CreatedAt);
