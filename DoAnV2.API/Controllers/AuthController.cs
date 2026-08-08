using DoAnV2.Application.Features.Auth.Commands;
using DoAnV2.Application.Features.Auth.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoAnV2.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>POST /api/v1/auth/register - Đăng ký tài khoản (Public).</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest body, CancellationToken ct)
    {
        var cmd = new RegisterCommand(
            body.FullName, body.Email, body.Phone, body.Password, body.RoleRequested);
        var result = await _mediator.Send(cmd, ct);
        return Ok(result);
    }

    /// <summary>POST /api/v1/auth/login - Đăng nhập, sinh JWT (Public).</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest body, CancellationToken ct)
    {
        var cmd = new LoginCommand(body.Email, body.Password);
        var result = await _mediator.Send(cmd, ct);
        return Ok(result);
    }

    /// <summary>POST /api/v1/auth/refresh-token - Cấp lại AccessToken (Public).</summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest body, CancellationToken ct)
    {
        var cmd = new RefreshTokenCommand(body.AccessToken, body.RefreshToken);
        var result = await _mediator.Send(cmd, ct);
        return Ok(result);
    }
}
