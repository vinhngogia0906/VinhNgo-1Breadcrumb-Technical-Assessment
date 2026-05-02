using LibraryApi.Application.DTOs;
using LibraryApi.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace LibraryApi.Web.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto dto, CancellationToken ct)
        => Ok(await _auth.RegisterAsync(dto, ct));

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto, CancellationToken ct)
        => Ok(await _auth.LoginAsync(dto, ct));
}
