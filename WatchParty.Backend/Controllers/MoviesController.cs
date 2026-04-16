using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchParty.Backend.DTOs;
using WatchParty.Backend.Services;

namespace WatchParty.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MoviesController : ControllerBase
{
    private readonly IMovieService _movieService;

    public MoviesController(IMovieService movieService)
    {
        _movieService = movieService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMovies(
        [FromQuery] string? query,
        [FromQuery] string? genre,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20)
    {
        var movies = await _movieService.SearchMoviesAsync(query, genre, page, limit);
        return Ok(movies);
    }

    [HttpGet("popular")]
    public async Task<IActionResult> GetPopularMovies([FromQuery] int limit = 20)
    {
        var movies = await _movieService.GetApprovedMoviesAsync(1, limit);
        return Ok(movies);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetMovie(string id)
    {
        var movie = await _movieService.GetMovieByIdAsync(id);
        if (movie == null)
            return NotFound(new { message = "Movie not found" });

        return Ok(movie);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddMovie([FromBody] CreateMovieDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var movie = await _movieService.AddMovieAsync(dto, userId);
        return CreatedAtAction(nameof(GetMovie), new { id = movie!.Id }, movie);
    }

    [HttpPut("{id}/approve")]
    [Authorize]
    public async Task<IActionResult> ApproveMovie(string id)
    {
        var success = await _movieService.ApproveMovieAsync(id);
        if (!success)
            return NotFound(new { message = "Movie not found" });

        return Ok(new { message = "Movie approved successfully" });
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteMovie(string id)
    {
        var success = await _movieService.DeleteMovieAsync(id);
        if (!success)
            return NotFound(new { message = "Movie not found" });

        return Ok(new { message = "Movie deleted successfully" });
    }
}
