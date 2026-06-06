using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.Modules.Identity.API.Dtos;
using STRIDE.Modules.Identity.Application.Commands.LoginUser;
using STRIDE.Modules.Identity.Application.Commands.RegisterUser;

namespace STRIDE.Modules.Identity.API.Controllers;

/// <summary>
/// Handles anonymous authentication flows — login and self-registration.
///
/// POST /api/identity/auth/login
///   Called by STRIDE.BFF (IdentityApiClient) — returns a JWT that the BFF
///   exchanges for a Redis-backed HttpOnly session cookie (ADR-007).
///
/// POST /api/identity/auth/register
///   Self-registration endpoint. Tenant is resolved from the email domain.
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
}
