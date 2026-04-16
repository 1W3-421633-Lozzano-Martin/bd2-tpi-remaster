namespace WatchParty.Backend.DTOs;

public record CreateMovieDto(
    string Title,
    string? Description,
    string VideoUrl,
    string? Thumbnail,
    int Duration,
    string? Genre,
    int? Year
);

public record MovieDto(
    string Id,
    string Title,
    string? Description,
    string VideoUrl,
    string? Thumbnail,
    int Duration,
    string? Genre,
    int? Year,
    string AddedByUsername,
    DateTime CreatedAt,
    bool IsApproved,
    int ViewCount
);

public record MovieSearchDto(string? Query, string? Genre, int Page = 1, int Limit = 20);
