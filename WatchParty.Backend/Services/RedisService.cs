using WatchParty.Backend.DTOs;
using StackExchange.Redis;

namespace WatchParty.Backend.Services;

public interface IRedisService
{
    Task<int> GetRoomViewerCountAsync(string roomCode);
    Task AddViewerAsync(string roomCode, string userId, string username);
    Task RemoveViewerAsync(string roomCode, string userId);
    Task<IEnumerable<ViewerDto>> GetRoomViewersAsync(string roomCode);
    Task CacheMessageAsync(ChatMessageDto message);
    Task<IEnumerable<ChatMessageDto>> GetRecentMessagesAsync(string roomCode, int limit = 50);
    Task StartVoteSessionAsync(string roomCode, string movieId, string movieTitle, int durationSeconds);
    Task<VoteSessionDto?> GetActiveVoteSessionAsync(string roomCode);
    Task AddVoteAsync(string roomCode, string userId, string movieId);
    Task EndVoteSessionAsync(string roomCode);
}

public class RedisService : IRedisService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    public RedisService(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _db = redis.GetDatabase();
    }

    public async Task<int> GetRoomViewerCountAsync(string roomCode)
    {
        var key = GetViewersKey(roomCode);
        var count = await _db.SetLengthAsync(key);
        return (int)count;
    }

    public async Task AddViewerAsync(string roomCode, string userId, string username)
    {
        var key = GetViewersKey(roomCode);
        await _db.SetAddAsync(key, $"{userId}:{username}");
        await _db.KeyExpireAsync(key, TimeSpan.FromHours(24));
    }

    public async Task RemoveViewerAsync(string roomCode, string userId)
    {
        var key = GetViewersKey(roomCode);
        var members = await _db.SetMembersAsync(key);
        
        foreach (var member in members)
        {
            var value = member.ToString();
            if (value.StartsWith($"{userId}:"))
            {
                await _db.SetRemoveAsync(key, value);
                break;
            }
        }
    }

    public async Task<IEnumerable<ViewerDto>> GetRoomViewersAsync(string roomCode)
    {
        var key = GetViewersKey(roomCode);
        var members = await _db.SetMembersAsync(key);
        
        return members.Select(m =>
        {
            var parts = m.ToString().Split(':');
            return new ViewerDto(parts[0], parts.Length > 1 ? parts[1] : "Unknown", null, DateTime.UtcNow);
        });
    }

    public async Task CacheMessageAsync(ChatMessageDto message)
    {
        var key = GetMessagesKey(message.RoomId);
        var json = System.Text.Json.JsonSerializer.Serialize(message);
        
        await _db.ListRightPushAsync(key, json);
        await _db.ListTrimAsync(key, -100, -1);
        await _db.KeyExpireAsync(key, TimeSpan.FromHours(24));
    }

    public async Task<IEnumerable<ChatMessageDto>> GetRecentMessagesAsync(string roomCode, int limit = 50)
    {
        var key = GetMessagesKey(roomCode);
        var messages = await _db.ListRangeAsync(key, -limit, -1);
        
        return messages.Select(m =>
        {
            var json = m.ToString();
            return System.Text.Json.JsonSerializer.Deserialize<ChatMessageDto>(json)!;
        }).Where(m => m != null);
    }

    public async Task StartVoteSessionAsync(string roomCode, string movieId, string movieTitle, int durationSeconds)
    {
        var key = GetVoteKey(roomCode);
        var voteSession = new VoteSessionDto(
            movieId,
            movieTitle,
            new Dictionary<string, int>(),
            DateTime.UtcNow.AddSeconds(durationSeconds).ToString("o")
        );
        
        var json = System.Text.Json.JsonSerializer.Serialize(voteSession);
        await _db.StringSetAsync(key, json, TimeSpan.FromSeconds(durationSeconds + 10));
    }

    public async Task<VoteSessionDto?> GetActiveVoteSessionAsync(string roomCode)
    {
        var key = GetVoteKey(roomCode);
        var json = await _db.StringGetAsync(key);
        
        if (json.IsNullOrEmpty) return null;
        
        return System.Text.Json.JsonSerializer.Deserialize<VoteSessionDto>(json!);
    }

    public async Task AddVoteAsync(string roomCode, string userId, string movieId)
    {
        var key = GetVoteKey(roomCode);
        var json = await _db.StringGetAsync(key);
        
        if (json.IsNullOrEmpty) return;
        
        var voteSession = System.Text.Json.JsonSerializer.Deserialize<VoteSessionDto>(json!);
        if (voteSession == null) return;

        if (voteSession.Votes.ContainsKey(movieId))
            voteSession.Votes[movieId]++;
        else
            voteSession.Votes[movieId] = 1;

        var newJson = System.Text.Json.JsonSerializer.Serialize(voteSession);
        var ttl = await _db.KeyTimeToLiveAsync(key);
        if (ttl.HasValue)
            await _db.StringSetAsync(key, newJson, ttl);
    }

    public async Task EndVoteSessionAsync(string roomCode)
    {
        var key = GetVoteKey(roomCode);
        await _db.KeyDeleteAsync(key);
    }

    private static string GetViewersKey(string roomCode) => $"room:{roomCode}:viewers";
    private static string GetMessagesKey(string roomCode) => $"room:{roomCode}:messages";
    private static string GetVoteKey(string roomCode) => $"room:{roomCode}:vote";
}
