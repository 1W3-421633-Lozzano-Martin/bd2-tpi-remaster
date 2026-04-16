namespace WatchParty.Backend.DTOs;

public record ChatMessageDto(
    string Id,
    string RoomId,
    string UserId,
    string Username,
    string? AvatarUrl,
    string Content,
    string Type,
    DateTime CreatedAt
);

public record SendMessageDto(string Content, string Type = "text");

public record ViewerDto(string UserId, string Username, string? AvatarUrl, bool IsCreator, DateTime JoinedAt);
