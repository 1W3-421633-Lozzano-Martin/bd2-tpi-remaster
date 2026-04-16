using WatchParty.Backend.DTOs;
using WatchParty.Backend.Models;

namespace WatchParty.Backend.Services;

public interface IRoomService
{
    Task<RoomDto?> CreateRoomAsync(CreateRoomDto dto, string userId, string username);
    Task<RoomDto?> GetRoomByCodeAsync(string code);
    Task<RoomStateDto?> GetRoomStateAsync(string code);
    Task<RoomDto?> JoinRoomAsync(string code, string? password);
    Task<bool> UpdateRoomAsync(string code, UpdateRoomDto dto, string userId);
    Task<bool> UpdateVideoStateAsync(string code, string? videoUrl, double position, bool isPlaying);
    Task<bool> ChangeMovieAsync(string code, string movieId, string movieTitle, string? thumbnail);
    Task<bool> DeleteRoomAsync(string code, string userId);
    Task<IEnumerable<RoomDto>> GetActiveRoomsAsync();
    Task<IEnumerable<RoomDto>> GetUserRoomsAsync(string userId);
    Task<bool> ValidateRoomPasswordAsync(string code, string password);
}

public class RoomService : IRoomService
{
    private readonly IRoomRepository _roomRepository;
    private readonly IMovieRepository _movieRepository;
    private readonly IRedisService _redisService;

    public RoomService(
        IRoomRepository roomRepository, 
        IMovieRepository movieRepository,
        IRedisService redisService)
    {
        _roomRepository = roomRepository;
        _movieRepository = movieRepository;
        _redisService = redisService;
    }

    public async Task<RoomDto?> CreateRoomAsync(CreateRoomDto dto, string userId, string username)
    {
        var code = await GenerateUniqueCodeAsync();
        
        var room = new Room
        {
            Code = code,
            Name = dto.Name,
            CreatorId = userId,
            CreatorUsername = username,
            VideoUrl = dto.VideoUrl,
            IsPrivate = dto.IsPrivate,
            Password = dto.IsPrivate && !string.IsNullOrEmpty(dto.Password) 
                ? BCrypt.Net.BCrypt.HashPassword(dto.Password) 
                : null,
            MaxViewers = dto.MaxViewers,
            CreatedAt = DateTime.UtcNow
        };

        await _roomRepository.CreateAsync(room);
        return ToRoomDto(room, 0);
    }

    public async Task<RoomDto?> GetRoomByCodeAsync(string code)
    {
        var room = await _roomRepository.GetByCodeAsync(code);
        if (room == null) return null;
        
        var viewerCount = await _redisService.GetRoomViewerCountAsync(code);
        return ToRoomDto(room, viewerCount);
    }

    public async Task<RoomStateDto?> GetRoomStateAsync(string code)
    {
        var room = await _roomRepository.GetByCodeAsync(code);
        if (room == null) return null;

        var viewerCount = await _redisService.GetRoomViewerCountAsync(code);
        
        return new RoomStateDto(
            room.Id,
            room.Code,
            room.Name,
            room.CurrentMovie != null ? new MovieInfoDto(room.CurrentMovie.Id, room.CurrentMovie.Title, room.CurrentMovie.Thumbnail) : null,
            room.VideoUrl,
            room.VideoPosition,
            room.IsPlaying,
            viewerCount,
            new List<MovieInfoDto>(),
            null
        );
    }

    public async Task<RoomDto?> JoinRoomAsync(string code, string? password)
    {
        var room = await _roomRepository.GetByCodeAsync(code);
        if (room == null || !room.IsActive) return null;

        if (room.IsPrivate && !string.IsNullOrEmpty(room.Password))
        {
            if (string.IsNullOrEmpty(password) || !BCrypt.Net.BCrypt.Verify(password, room.Password))
                return null;
        }

        var viewerCount = await _redisService.GetRoomViewerCountAsync(code);
        return ToRoomDto(room, viewerCount);
    }

    public async Task<bool> UpdateRoomAsync(string code, UpdateRoomDto dto, string userId)
    {
        var room = await _roomRepository.GetByCodeAsync(code);
        if (room == null || room.CreatorId != userId) return false;

        if (dto.Name != null) room.Name = dto.Name;
        if (dto.VideoUrl != null) room.VideoUrl = dto.VideoUrl;
        if (dto.IsPrivate.HasValue) room.IsPrivate = dto.IsPrivate.Value;
        if (dto.MaxViewers.HasValue) room.MaxViewers = dto.MaxViewers.Value;
        if (dto.Password != null) room.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        return await _roomRepository.UpdateAsync(room.Id, room);
    }

    public async Task<bool> UpdateVideoStateAsync(string code, string? videoUrl, double position, bool isPlaying)
    {
        var room = await _roomRepository.GetByCodeAsync(code);
        if (room == null) return false;

        if (videoUrl != null) room.VideoUrl = videoUrl;
        room.VideoPosition = position;
        room.IsPlaying = isPlaying;

        return await _roomRepository.UpdateAsync(room.Id, room);
    }

    public async Task<bool> ChangeMovieAsync(string code, string movieId, string movieTitle, string? thumbnail)
    {
        var room = await _roomRepository.GetByCodeAsync(code);
        if (room == null) return false;

        room.CurrentMovie = new MovieInfo { Id = movieId, Title = movieTitle, Thumbnail = thumbnail };
        room.VideoPosition = 0;
        room.IsPlaying = true;

        var movie = await _movieRepository.GetByIdAsync(movieId);
        if (movie != null)
        {
            movie.ViewCount++;
            await _movieRepository.UpdateAsync(movie.Id, movie);
        }

        return await _roomRepository.UpdateAsync(room.Id, room);
    }

    public async Task<bool> DeleteRoomAsync(string code, string userId)
    {
        var room = await _roomRepository.GetByCodeAsync(code);
        if (room == null || room.CreatorId != userId) return false;

        room.IsActive = false;
        return await _roomRepository.UpdateAsync(room.Id, room);
    }

    public async Task<IEnumerable<RoomDto>> GetActiveRoomsAsync()
    {
        var rooms = await _roomRepository.GetActiveRoomsAsync();
        var roomDtos = new List<RoomDto>();
        
        foreach (var room in rooms)
        {
            var viewerCount = await _redisService.GetRoomViewerCountAsync(room.Code);
            roomDtos.Add(ToRoomDto(room, viewerCount));
        }
        
        return roomDtos;
    }

    public async Task<IEnumerable<RoomDto>> GetUserRoomsAsync(string userId)
    {
        var rooms = await _roomRepository.GetUserRoomsAsync(userId);
        var roomDtos = new List<RoomDto>();
        
        foreach (var room in rooms)
        {
            var viewerCount = await _redisService.GetRoomViewerCountAsync(room.Code);
            roomDtos.Add(ToRoomDto(room, viewerCount));
        }
        
        return roomDtos;
    }

    public async Task<bool> ValidateRoomPasswordAsync(string code, string password)
    {
        var room = await _roomRepository.GetByCodeAsync(code);
        if (room == null || !room.IsPrivate || string.IsNullOrEmpty(room.Password)) return false;
        return BCrypt.Net.BCrypt.Verify(password, room.Password);
    }

    private async Task<string> GenerateUniqueCodeAsync()
    {
        var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        string code;
        
        do
        {
            code = new string(Enumerable.Range(0, 6).Select(_ => chars[random.Next(chars.Length)]).ToArray());
        }
        while (await _roomRepository.CodeExistsAsync(code));

        return code;
    }

    private static RoomDto ToRoomDto(Room room, int viewerCount) => new(
        room.Id,
        room.Code,
        room.Name,
        room.CreatorId,
        room.CreatorUsername,
        room.CurrentMovie != null ? new MovieInfoDto(room.CurrentMovie.Id, room.CurrentMovie.Title, room.CurrentMovie.Thumbnail) : null,
        room.VideoUrl,
        room.VideoPosition,
        room.IsPlaying,
        room.IsPrivate,
        viewerCount,
        room.CreatedAt
    );
}
