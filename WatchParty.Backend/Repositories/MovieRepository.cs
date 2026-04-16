using MongoDB.Driver;
using WatchParty.Backend.Models;

namespace WatchParty.Backend.Repositories;

public interface IMovieRepository : IRepository<Movie>
{
    Task<IEnumerable<Movie>> SearchAsync(string? query, string? genre, int page, int limit);
    Task<IEnumerable<Movie>> GetApprovedMoviesAsync(int page = 1, int limit = 50);
    Task<IEnumerable<Movie>> GetPendingMoviesAsync();
    Task<Movie?> GetByTitleAsync(string title);
}

public class MovieRepository : MongoRepository<Movie>, IMovieRepository
{
    public MovieRepository(IMongoDatabase database) : base(database, "movies")
    {
        var titleIndex = Builders<Movie>.IndexKeys.Ascending(m => m.Title);
        _collection.Indexes.CreateOneAsync(new CreateIndexModel<Movie>(titleIndex));
        
        var approvedIndex = Builders<Movie>.IndexKeys.Ascending(m => m.IsApproved);
        _collection.Indexes.CreateOneAsync(new CreateIndexModel<Movie>(approvedIndex));
    }

    public async Task<IEnumerable<Movie>> SearchAsync(string? query, string? genre, int page, int limit)
    {
        var filterBuilder = Builders<Movie>.Filter;
        var filter = filterBuilder.Eq(m => m.IsApproved, true);

        if (!string.IsNullOrEmpty(query))
        {
            filter = filterBuilder.And(
                filter,
                filterBuilder.Regex(m => m.Title, new MongoDB.Bson.BsonRegularExpression(query, "i"))
            );
        }

        if (!string.IsNullOrEmpty(genre))
        {
            filter = filterBuilder.And(
                filter,
                filterBuilder.Eq(m => m.Genre, genre)
            );
        }

        return await _collection
            .Find(filter)
            .SortByDescending(m => m.ViewCount)
            .Skip((page - 1) * limit)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<Movie>> GetApprovedMoviesAsync(int page = 1, int limit = 50)
    {
        return await _collection
            .Find(m => m.IsApproved)
            .SortByDescending(m => m.ViewCount)
            .Skip((page - 1) * limit)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<Movie>> GetPendingMoviesAsync()
    {
        return await _collection
            .Find(m => !m.IsApproved)
            .SortByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<Movie?> GetByTitleAsync(string title)
    {
        return await _collection.Find(m => m.Title.ToLower() == title.ToLower()).FirstOrDefaultAsync();
    }
}
