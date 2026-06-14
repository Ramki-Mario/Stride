using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.Modules.Identity.API.Dtos;
using STRIDE.Modules.Identity.Application.Commands.LoginUser;
using STRIDE.Modules.Identity.Application.Commands.RefreshToken;
using STRIDE.Modules.Identity.Application.Commands.RegisterUser;
using STRIDE.Modules.Identity.Application.Commands.RevokeToken;

namespace STRIDE.Modules.Identity.API.Controllers;

/// <summary>
/// Handles anonymous authentication flows — login, registration, token refresh, and revoke.
///
/// All endpoints are [AllowAnonymous] and called server-to-server from STRIDE.BFF.
/// Tokens are never exposed to the Angular SPA (ADR-007 / ADR-011).
/// </summary>
[ApiController]
[Route("api/identity/auth")]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Authenticate user. Returns a signed JWT access token on success.
    /// The shape of the 200 response matches <c>HostLoginResponse</c> in STRIDE.BFF.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]   // validation failure
    [ProducesResponseType(StatusCodes.Status401Unauthorized)] // wrong credentials / unknown tenant
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new LoginCommand(request.Email, request.Password), cancellationToken);

        if (result.IsFailure)
            return Unauthorized(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Register a new user account.  Tenant is derived from email domain.
    /// Returns 201 Created with the new user's profile on success.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RegisterCommand(request.Email, request.Password, request.DisplayName), cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error!.Contains("already exists", StringComparison.OrdinalIgnoreCase))
                return Conflict(new { error = result.Error });

            // Tenant not found or other business rule failure.
            return UnprocessableEntity(new { error = result.Error });
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>
    /// Rotate a refresh token. Returns a new access + refresh token pair on success.
    /// Called by STRIDE.BFF when the current access token has expired or is about to expire.
    /// No JWT required — the refresh token is the credential.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(TokenPairResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(
        [FromBody] TokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RefreshTokenCommand(request.Token), cancellationToken);

        if (result.IsFailure)
            return Unauthorized(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Revoke a refresh token. Idempotent — already-revoked tokens return 204.
    /// Called by STRIDE.BFF on logout.
    /// </summary>
    [HttpPost("revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeToken(
        [FromBody] TokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RevokeTokenCommand(request.Token), cancellationToken);

        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return NoContent();
    }
}
