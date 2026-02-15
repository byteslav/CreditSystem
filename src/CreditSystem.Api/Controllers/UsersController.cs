namespace CreditSystem.Api.Controllers;

using CreditSystem.Application.DTOs;
using CreditSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Get current authenticated user's information
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current user's information including credits and registration date</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub) 
            ?? User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new { message = "Invalid or missing user ID in token" });
        }

        var userResponse = await _userService.GetCurrentUserAsync(userId, cancellationToken);

        if (userResponse == null)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(userResponse);
    }
}
