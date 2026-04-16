using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchParty.Backend.DTOs;
using WatchParty.Backend.Services;

namespace WatchParty.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _roomService;

    public RoomsController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveRooms()
    {
        var rooms = await _roomService.GetActiveRoomsAsync();
        return Ok(rooms);
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> GetRoom(string code)
    {
        var room = await _roomService.GetRoomByCodeAsync(code.ToUpper());
        if (room == null)
            return NotFound(new { message = "Room not found" });

        return Ok(room);
    }

    [HttpGet("{code}/state")]
    public async Task<IActionResult> GetRoomState(string code)
    {
        var state = await _roomService.GetRoomStateAsync(code.ToUpper());
        if (state == null)
            return NotFound(new { message = "Room not found" });

        return Ok(state);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(username))
            return Unauthorized();

        var room = await _roomService.CreateRoomAsync(dto, userId, username);
        return CreatedAtAction(nameof(GetRoom), new { code = room!.Code }, room);
    }

    [HttpPost("{code}/join")]
    public async Task<IActionResult> JoinRoom(string code, [FromBody] JoinRoomDto dto)
    {
        var room = await _roomService.JoinRoomAsync(code.ToUpper(), dto.Password);
        if (room == null)
            return Unauthorized(new { message = "Invalid room code or password" });

        return Ok(room);
    }

    [HttpPut("{code}")]
    [Authorize]
    public async Task<IActionResult> UpdateRoom(string code, [FromBody] UpdateRoomDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var success = await _roomService.UpdateRoomAsync(code.ToUpper(), dto, userId);
        if (!success)
            return BadRequest(new { message = "Failed to update room or not authorized" });

        return Ok(new { message = "Room updated successfully" });
    }

    [HttpDelete("{code}")]
    [Authorize]
    public async Task<IActionResult> DeleteRoom(string code)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var success = await _roomService.DeleteRoomAsync(code.ToUpper(), userId);
        if (!success)
            return BadRequest(new { message = "Failed to delete room or not authorized" });

        return Ok(new { message = "Room deleted successfully" });
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserRooms(string userId)
    {
        var rooms = await _roomService.GetUserRoomsAsync(userId);
        return Ok(rooms);
    }
}

public record JoinRoomDto(string? Password);
