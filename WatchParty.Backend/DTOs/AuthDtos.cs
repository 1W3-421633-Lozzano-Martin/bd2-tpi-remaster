namespace WatchParty.Backend.DTOs;

public record RegisterDto(string Username, string Email, string Password);

public record LoginDto(string Email, string Password);

public record AuthResponseDto(string Token, UserDto User);

public record UserDto(string Id, string Username, string Email, string? AvatarUrl);
