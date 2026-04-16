using MongoDB.Driver;
using WatchParty.Backend.Models;

namespace WatchParty.Backend.Repositories;

public interface IRoomRepository : IRepository<Room>
{
    Task<Room?> GetByCodeAsync(string code);
    Task<Room?> GetByCodeWithAuthAsync(string code, string? password);
    Task<IEnumerable<Room>> GetActiveRoomsAsync(int limit = 50);
    Task<IEnumerable<Room>> GetUserRoomsAsync(string userId);
    Task<bool> CodeExistsAsync(string code);
}

public class RoomRepository : MongoRepository<Room>, IRoomRepository
{
    public RoomRepository(IMongoDatabase database) : base(database, "rooms")
    {
        var codeIndex = Builders<Room>.IndexKeys.Ascending(r => r.Code);
        _collection.Indexes.CreateOneAsync(new CreateIndexModel<Room>(codeIndex, new CreateIndexOptions { Unique = true }));
        
        var creatorIndex = Builders<Room>.IndexKeys.Ascending(r => r.CreatorId);
        _collection.Indexes.CreateOneAsync(new CreateIndexModel<Room>(creatorIndex));
    }

    public async Task<Room?> GetByCodeAsync(string code)
    {
        return await _collection.Find(r => r.Code == code && r.IsActive).FirstOrDefaultAsync();
    }

    public async Task<Room?> GetByCodeWithAuthAsync(string code, string? password)
    {
        var filter = Builders<Room>.Filter.And(
            Builders<Room>.Filter.Eq(r => r.Code, code),
            Builders<Room>.Filter.Eq(r => r.IsActive, true)
        );

        if (!string.IsNullOrEmpty(password))
        {
            filter = Builders<Room>.Filter.And(
                filter,
                Builders<Room>.Filter.Or(
                    Builders<Room>.Filter.Eq(r => r.Password, null),
                    Builders<Room>.Filter.Eq(r => r.Password, password)
                )
            );
        }

        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Room>> GetActiveRoomsAsync(int limit = 50)
    {
        return await _collection
            .Find(r => r.IsActive)
            .SortByDescending(r => r.CreatedAt)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<Room>> GetUserRoomsAsync(string userId)
    {
        return await _collection
            .Find(r => r.CreatorId == userId && r.IsActive)
            .SortByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> CodeExistsAsync(string code)
    {
        return await _collection.Find(r => r.Code == code).AnyAsync();
    }
}
