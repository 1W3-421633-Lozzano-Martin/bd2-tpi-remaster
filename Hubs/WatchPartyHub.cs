using Microsoft.AspNetCore.SignalR;
using WatchParty.Backend.DTOs;
using WatchParty.Backend.Services;

namespace WatchParty.Backend.Hubs;

public class WatchPartyHub : Hub
{
    private readonly IRoomService _roomService;
    private readonly IRedisService _redisService;
    private readonly IAuthService _authService;

    public WatchPartyHub(
        IRoomService roomService,
        IRedisService redisService,
        IAuthService authService)
    {
        _roomService = roomService;
        _redisService = redisService;
        _authService = authService;
    }

    public async Task JoinRoom(string roomCode, string? password)
    {
        var room = await _roomService.JoinRoomAsync(roomCode, password);
        if (room == null)
        {
            await Clients.Caller.SendAsync("Error", "Room not found or invalid password");
            return;
        }

        var userId = GetUserId();
        var username = GetUsername();

        await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
        await _redisService.AddViewerAsync(roomCode, userId, username);

        var viewers = await _redisService.GetRoomViewersAsync(roomCode);
        var messages = await _redisService.GetRecentMessagesAsync(roomCode);
        var state = await _roomService.GetRoomStateAsync(roomCode);
        var voteSession = await _redisService.GetActiveVoteSessionAsync(roomCode);

        await Clients.Caller.SendAsync("RoomJoined", new
        {
            Room = room,
            Viewers = viewers,
            Messages = messages,
            State = state,
            ActiveVote = voteSession
        });

        await Clients.Group(roomCode).SendAsync("ViewerJoined", new
        {
            UserId = userId,
            Username = username,
            ViewerCount = await _redisService.GetRoomViewerCountAsync(roomCode)
        });
    }

    public async Task LeaveRoom(string roomCode)
    {
        var userId = GetUserId();
        var username = GetUsername();

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomCode);
        await _redisService.RemoveViewerAsync(roomCode, userId);

        await Clients.Group(roomCode).SendAsync("ViewerLeft", new
        {
            UserId = userId,
            Username = username,
            ViewerCount = await _redisService.GetRoomViewerCountAsync(roomCode)
        });
    }

    public async Task SendMessage(string content, string type = "text")
    {
        var roomCode = GetRoomCode();
        if (string.IsNullOrEmpty(roomCode)) return;

        var userId = GetUserId();
        var user = await _authService.GetUserByIdAsync(userId);
        if (user == null) return;

        var message = new ChatMessageDto(
            Guid.NewGuid().ToString(),
            roomCode,
            userId,
            user.Username,
            user.AvatarUrl,
            content,
            type,
            DateTime.UtcNow
        );

        await _redisService.CacheMessageAsync(message);
        await Clients.Group(roomCode).SendAsync("NewMessage", message);
    }

    public async Task SyncVideo(string? videoUrl, double position, bool isPlaying)
    {
        var roomCode = GetRoomCode();
        if (string.IsNullOrEmpty(roomCode)) return;

        var userId = GetUserId();
        var room = await _roomService.GetRoomByCodeAsync(roomCode);
        if (room == null || room.CreatorId != userId) return;

        await _roomService.UpdateVideoStateAsync(roomCode, videoUrl, position, isPlaying);
        await Clients.Group(roomCode).SendAsync("VideoSync", new { videoUrl, position, isPlaying });
    }

    public async Task ChangeMovie(string movieId, string movieTitle, string? thumbnail)
    {
        var roomCode = GetRoomCode();
        if (string.IsNullOrEmpty(roomCode)) return;

        var userId = GetUserId();
        var room = await _roomService.GetRoomByCodeAsync(roomCode);
        if (room == null || room.CreatorId != userId) return;

        await _roomService.ChangeMovieAsync(roomCode, movieId, movieTitle, thumbnail);
        await Clients.Group(roomCode).SendAsync("MovieChanged", new
        {
            MovieId = movieId,
            MovieTitle = movieTitle,
            Thumbnail = thumbnail,
            Position = 0
        });
    }

    public async Task StartVote(string movieId, string movieTitle, int durationSeconds = 60)
    {
        var roomCode = GetRoomCode();
        if (string.IsNullOrEmpty(roomCode)) return;

        var userId = GetUserId();
        var room = await _roomService.GetRoomByCodeAsync(roomCode);
        if (room == null || room.CreatorId != userId) return;

        await _redisService.StartVoteSessionAsync(roomCode, movieId, movieTitle, durationSeconds);
        
        await Clients.Group(roomCode).SendAsync("VoteStarted", new
        {
            MovieId = movieId,
            MovieTitle = movieTitle,
            EndsAt = DateTime.UtcNow.AddSeconds(durationSeconds).ToString("o")
        });

        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(durationSeconds));
            await EndVoteInternal(roomCode);
        });
    }

    public async Task CastVote(string movieId)
    {
        var roomCode = GetRoomCode();
        if (string.IsNullOrEmpty(roomCode)) return;

        var userId = GetUserId();
        await _redisService.AddVoteAsync(roomCode, userId, movieId);
        
        var voteSession = await _redisService.GetActiveVoteSessionAsync(roomCode);
        if (voteSession != null)
        {
            await Clients.Group(roomCode).SendAsync("VoteUpdated", voteSession);
        }
    }

    public async Task EndVote()
    {
        var roomCode = GetRoomCode();
        if (string.IsNullOrEmpty(roomCode)) return;

        var userId = GetUserId();
        var room = await _roomService.GetRoomByCodeAsync(roomCode);
        if (room == null || room.CreatorId != userId) return;

        await EndVoteInternal(roomCode);
    }

    private async Task EndVoteInternal(string roomCode)
    {
        var voteSession = await _redisService.GetActiveVoteSessionAsync(roomCode);
        if (voteSession == null) return;

        var winnerId = voteSession.Votes.OrderByDescending(v => v.Value).FirstOrDefault().Key;
        
        if (!string.IsNullOrEmpty(winnerId))
        {
            await _roomService.ChangeMovieAsync(roomCode, winnerId, voteSession.MovieTitle, null);
        }

        await _redisService.EndVoteSessionAsync(roomCode);
        
        await Clients.Group(roomCode).SendAsync("VoteEnded", new
        {
            WinnerId = winnerId,
            WinnerTitle = voteSession.MovieTitle,
            Votes = voteSession.Votes
        });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var roomCode = GetRoomCode();
        if (!string.IsNullOrEmpty(roomCode))
        {
            var userId = GetUserId();
            await _redisService.RemoveViewerAsync(roomCode, userId);

            await Clients.Group(roomCode).SendAsync("ViewerDisconnected", new
            {
                UserId = userId,
                ViewerCount = await _redisService.GetRoomViewerCountAsync(roomCode)
            });
        }

        await base.OnDisconnectedAsync(exception);
    }

    private string GetUserId()
    {
        return Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
            ?? Context.QueryString["userId"] 
            ?? "anonymous";
    }

    private string GetUsername()
    {
        return Context.User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
            ?? Context.QueryString["username"]
            ?? "Anonymous";
    }

    private string? GetRoomCode()
    {
        var groups = Context.Groups.ToList();
        return groups.FirstOrDefault();
    }
}
