using System.Security.Claims;
using AlDawahPharma.Application.Common;
using AlDawahPharma.Application.DTOs;
using AlDawahPharma.Application.Exceptions;
using AlDawahPharma.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlDawahPharma.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            throw new ValidationException("Username and password are required.");

        var user = await _userRepository.GetByUsernameAsync(request.Username);
        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.Password))
        {
            throw new UnauthorizedException("Invalid username or password.");
        }

        var token = _jwtTokenService.GenerateToken(user);

        return Ok(ApiResponse<LoginResponse>.Ok(new LoginResponse
        {
            Token = token,
            UserID = user.UserID,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role,
            ExpiresAt = DateTime.UtcNow.AddHours(12)
        }, "Login successful."));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedException("Invalid user identity in token.");

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new NotFoundException("User", userId);

        return Ok(ApiResponse<UserDto>.Ok(new UserDto
        {
            UserID = user.UserID,
            FullName = user.FullName,
            Username = user.Username,
            Role = user.Role,
            Phone = user.Phone,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        }));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedException();

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new NotFoundException("User", userId);

        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.Password))
            throw new ValidationException("Current password does not match.");

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            throw new ValidationException("New password must be at least 6 characters long.");

        var newHash = _passwordHasher.HashPassword(request.NewPassword);
        await _userRepository.ChangePasswordAsync(userId, newHash);

        return Ok(ApiResponse.Ok("Password changed successfully."));
    }
}
