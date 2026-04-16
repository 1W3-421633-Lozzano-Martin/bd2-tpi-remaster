using MongoDB.Driver;
using WatchParty.Backend.Models;

namespace WatchParty.Backend.Repositories;

public interface IVoteRepository : IRepository<Vote>
{
    Task<IEnumerable<Vote>> GetRoomVotesAsync(string roomId);
    Task<Vote?> GetUserVoteInRoomAsync(string roomId, string userId);
    Task<int> GetMovieVoteCountAsync(string roomId, string movieId);
    Task DeleteRoomVotesAsync(string roomId);
}

public class VoteRepository : MongoRepository<Vote>, IVoteRepository
{
    public VoteRepository(IMongoDatabase database) : base(database, "votes")
    {
        var roomIndex = Builders<Vote>.IndexKeys.Ascending(v => v.RoomId);
        _collection.Indexes.CreateOneAsync(new CreateIndexModel<Vote>(roomIndex));
        
        var userRoomIndex = Builders<Vote>.IndexKeys
            .Ascending(v => v.UserId)
            .Ascending(v => v.RoomId);
        _collection.Indexes.CreateOneAsync(new CreateIndexModel<Vote>(userRoomIndex, new CreateIndexOptions { Unique = true }));
    }

    public async Task<IEnumerable<Vote>> GetRoomVotesAsync(string roomId)
    {
        return await _collection.Find(v => v.RoomId == roomId).ToListAsync();
    }

    public async Task<Vote?> GetUserVoteInRoomAsync(string roomId, string userId)
    {
        return await _collection.Find(v => v.RoomId == roomId && v.UserId == userId).FirstOrDefaultAsync();
    }

    public async Task<int> GetMovieVoteCountAsync(string roomId, string movieId)
    {
        return (int)await _collection.CountDocumentsAsync(v => v.RoomId == roomId && v.MovieId == movieId);
    }

    public async Task DeleteRoomVotesAsync(string roomId)
    {
        await _collection.DeleteManyAsync(v => v.RoomId == roomId);
    }
}
