using MongoDB.Driver;
using WatchParty.Backend.Models;

namespace WatchParty.Backend.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameAsync(string username);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> UsernameExistsAsync(string username);
}

public class UserRepository : MongoRepository<User>, IUserRepository
{
    public UserRepository(IMongoDatabase database) : base(database, "users")
    {
        var indexKeysDefinition = Builders<User>.IndexKeys.Ascending(u => u.Email);
        _collection.Indexes.CreateOneAsync(new CreateIndexModel<User>(indexKeysDefinition));
        
        var usernameIndex = Builders<User>.IndexKeys.Ascending(u => u.Username);
        _collection.Indexes.CreateOneAsync(new CreateIndexModel<User>(usernameIndex));
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _collection.Find(u => u.Email == email.ToLower()).FirstOrDefaultAsync();
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _collection.Find(u => u.Username.ToLower() == username.ToLower()).FirstOrDefaultAsync();
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _collection.Find(u => u.Email == email.ToLower()).AnyAsync();
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        return await _collection.Find(u => u.Username.ToLower() == username.ToLower()).AnyAsync();
    }
}
