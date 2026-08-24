using System.Security.Claims;
using LeadEngine.Api.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeadEngine.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController(AdminAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<AuthUserResponse>> Login(AuthLoginRequest request)
    {
        if (!authService.Validate(request.Email, request.Password))
        {
            return Unauthorized(new { mensagem = "Credenciais invalidas." });
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, request.Email.Trim().ToLowerInvariant()),
            new Claim(ClaimTypes.Name, request.Email.Trim())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = false });

        return Ok(new AuthUserResponse(request.Email.Trim()));
    }

    [HttpGet("me")]
    public ActionResult<AuthUserResponse> Me()
    {
        return User.Identity?.IsAuthenticated == true
            ? Ok(new AuthUserResponse(User.Identity.Name ?? string.Empty))
            : Unauthorized();
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}

public sealed record AuthLoginRequest(string Email, string Password);
public sealed record AuthUserResponse(string Email);
