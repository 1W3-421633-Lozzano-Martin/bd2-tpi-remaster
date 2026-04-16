using WatchParty.Backend.DTOs;
using WatchParty.Backend.Models;

namespace WatchParty.Backend.Services;

public interface IMovieService
{
    Task<MovieDto?> GetMovieByIdAsync(string id);
    Task<IEnumerable<MovieDto>> SearchMoviesAsync(string? query, string? genre, int page, int limit);
    Task<IEnumerable<MovieDto>> GetApprovedMoviesAsync(int page = 1, int limit = 50);
    Task<MovieDto?> AddMovieAsync(CreateMovieDto dto, string userId);
    Task<bool> ApproveMovieAsync(string id);
    Task<bool> DeleteMovieAsync(string id);
}

public class MovieService : IMovieService
{
    private readonly IMovieRepository _movieRepository;
    private readonly IUserRepository _userRepository;

    public MovieService(IMovieRepository movieRepository, IUserRepository userRepository)
    {
        _movieRepository = movieRepository;
        _userRepository = userRepository;
    }

    public async Task<MovieDto?> GetMovieByIdAsync(string id)
    {
        var movie = await _movieRepository.GetByIdAsync(id);
        if (movie == null) return null;

        var user = await _userRepository.GetByIdAsync(movie.AddedBy);
        return ToMovieDto(movie, user?.Username ?? "Unknown");
    }

    public async Task<IEnumerable<MovieDto>> SearchMoviesAsync(string? query, string? genre, int page, int limit)
    {
        var movies = await _movieRepository.SearchAsync(query, genre, page, limit);
        var movieDtos = new List<MovieDto>();
        
        foreach (var movie in movies)
        {
            var user = await _userRepository.GetByIdAsync(movie.AddedBy);
            movieDtos.Add(ToMovieDto(movie, user?.Username ?? "Unknown"));
        }
        
        return movieDtos;
    }

    public async Task<IEnumerable<MovieDto>> GetApprovedMoviesAsync(int page = 1, int limit = 50)
    {
        var movies = await _movieRepository.GetApprovedMoviesAsync(page, limit);
        var movieDtos = new List<MovieDto>();
        
        foreach (var movie in movies)
        {
            var user = await _userRepository.GetByIdAsync(movie.AddedBy);
            movieDtos.Add(ToMovieDto(movie, user?.Username ?? "Unknown"));
        }
        
        return movieDtos;
    }

    public async Task<MovieDto?> AddMovieAsync(CreateMovieDto dto, string userId)
    {
        var movie = new Movie
        {
            Title = dto.Title,
            Description = dto.Description,
            VideoUrl = dto.VideoUrl,
            Thumbnail = dto.Thumbnail,
            Duration = dto.Duration,
            Genre = dto.Genre,
            Year = dto.Year,
            AddedBy = userId,
            IsApproved = true,
            CreatedAt = DateTime.UtcNow
        };

        await _movieRepository.CreateAsync(movie);

        var user = await _userRepository.GetByIdAsync(userId);
        return ToMovieDto(movie, user?.Username ?? "Unknown");
    }

    public async Task<bool> ApproveMovieAsync(string id)
    {
        var movie = await _movieRepository.GetByIdAsync(id);
        if (movie == null) return false;

        movie.IsApproved = true;
        return await _movieRepository.UpdateAsync(id, movie);
    }

    public async Task<bool> DeleteMovieAsync(string id)
    {
        return await _movieRepository.DeleteAsync(id);
    }

    private static MovieDto ToMovieDto(Movie movie, string addedByUsername) => new(
        movie.Id,
        movie.Title,
        movie.Description,
        movie.VideoUrl,
        movie.Thumbnail,
        movie.Duration,
        movie.Genre,
        movie.Year,
        addedByUsername,
        movie.CreatedAt,
        movie.IsApproved,
        movie.ViewCount
    );
}
