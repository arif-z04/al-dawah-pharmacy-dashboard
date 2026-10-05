using AlDawahPharma.Application.Common;
using AlDawahPharma.Application.DTOs;
using AlDawahPharma.Application.Exceptions;
using AlDawahPharma.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlDawahPharma.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;

    public UsersController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetAll()
    {
        var users = await _userRepository.GetAllAsync();
        var dtos = users.Select(u => new UserDto
        {
            UserID = u.UserID,
            FullName = u.FullName,
            Username = u.Username,
            Role = u.Role,
            Phone = u.Phone,
            Email = u.Email,
            CreatedAt = u.CreatedAt
        });
        return Ok(ApiResponse<IEnumerable<UserDto>>.Ok(dtos));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(long id)
    {
        var u = await _userRepository.GetByIdAsync(id);
        if (u == null) throw new NotFoundException("User", id);

        return Ok(ApiResponse<UserDto>.Ok(new UserDto
        {
            UserID = u.UserID,
            FullName = u.FullName,
            Username = u.Username,
            Role = u.Role,
            Phone = u.Phone,
            Email = u.Email,
            CreatedAt = u.CreatedAt
        }));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> Create([FromBody] CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            throw new ValidationException("Full name, username, and password are required.");

        var created = await _userRepository.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.UserID }, ApiResponse<UserDto>.Ok(new UserDto
        {
            UserID = created.UserID,
            FullName = created.FullName,
            Username = created.Username,
            Role = created.Role,
            Phone = created.Phone,
            Email = created.Email,
            CreatedAt = created.CreatedAt
        }, "User created successfully."));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse>> Update(long id, [FromBody] UpdateUserRequest request)
    {
        var existing = await _userRepository.GetByIdAsync(id);
        if (existing == null) throw new NotFoundException("User", id);

        var updated = await _userRepository.UpdateAsync(id, request);
        return Ok(ApiResponse.Ok("User updated successfully."));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> Delete(long id)
    {
        var existing = await _userRepository.GetByIdAsync(id);
        if (existing == null) throw new NotFoundException("User", id);

        await _userRepository.DeleteAsync(id);
        return Ok(ApiResponse.Ok("User deleted successfully."));
    }
}
