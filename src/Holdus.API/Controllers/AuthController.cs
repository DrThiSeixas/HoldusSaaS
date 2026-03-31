using Holdus.Application.DTOs;
using Holdus.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Holdus.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    /// <summary>
    /// Login — retorna JWT com claims de tenant e role.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _auth.LoginAsync(request);
        if (result == null)
            return Unauthorized(new ApiResponse<object>(false, null, "E-mail ou senha inválidos"));

        return Ok(new ApiResponse<TokenResponse>(true, result));
    }

    /// <summary>
    /// Registro — cria tenant (escritório) + primeiro usuário admin.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _auth.RegisterAsync(request);
        if (result == null)
            return Conflict(new ApiResponse<object>(false, null, "E-mail já cadastrado"));

        return CreatedAtAction(nameof(Login), new ApiResponse<TokenResponse>(true, result, "Escritório criado com sucesso"));
    }

    /// <summary>
    /// Retorna informações do usuário autenticado.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var claims = User.Claims.ToDictionary(c => c.Type, c => c.Value);
        return Ok(new ApiResponse<object>(true, new
        {
            userId = claims.GetValueOrDefault("sub"),
            email = claims.GetValueOrDefault("email"),
            name = claims.GetValueOrDefault("name"),
            role = claims.GetValueOrDefault("role"),
            tenantId = claims.GetValueOrDefault("tenant_id"),
            tenantSlug = claims.GetValueOrDefault("tenant_slug"),
        }));
    }
}
