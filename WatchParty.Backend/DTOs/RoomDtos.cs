namespace WatchParty.Backend.DTOs;

public record CreateRoomDto(string Name, string? VideoUrl, bool IsPrivate = false, string? Password = null, int MaxViewers = 50);

public record UpdateRoomDto(string? Name, string? VideoUrl, bool? IsPrivate, string? Password, int? MaxViewers);

public record RoomDto(
    string Id,
    string Code,
    string Name,
    string CreatorId,
    string CreatorUsername,
    MovieInfoDto? CurrentMovie,
    string? VideoUrl,
    double VideoPosition,
    bool IsPlaying,
    bool IsPrivate,
    int ViewerCount,
    DateTime CreatedAt
);

public record MovieInfoDto(string Id, string Title, string? Thumbnail);

public record RoomStateDto(
    string Id,
    string Code,
    string Name,
    MovieInfoDto? CurrentMovie,
    string? VideoUrl,
    double VideoPosition,
    bool IsPlaying,
    int ViewerCount,
    List<MovieInfoDto> Playlist,
    VoteSessionDto? ActiveVote
);

public record VoteSessionDto(string MovieId, string MovieTitle, Dictionary<string, int> Votes, string EndsAt);
