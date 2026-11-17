using Microsoft.AspNetCore.Http;
using SIMS.Application.Interfaces;
using SIMS.Application.Services;
using SIMS.Domain.Entities;

namespace SIMS.Presentation.Controllers;

/// <summary>
/// Manages the authenticated user session across MVC requests using signed HMAC-SHA256 JWT tokens.
/// </summary>
public class CurrentUserContext
{
    private const string JwtCookieKey = "SIMS_JwtAuthToken";
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuthService _authService;

    public CurrentUserContext(IHttpContextAccessor httpContextAccessor, IAuthService authService)
    {
        _httpContextAccessor = httpContextAccessor;
        _authService = authService;
    }

    public User? GetCurrentUser()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        string? jwtToken = httpContext?.Request.Cookies[JwtCookieKey];

        var payload = SecurityTokenHelper.ValidateAndDecodeJwtToken(jwtToken);
        if (payload != null && !string.IsNullOrWhiteSpace(payload.Email))
        {
            return _authService.Login(payload.Email);
        }

        return null;
    }

    public void SignIn(User user)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        string jwtToken = SecurityTokenHelper.GenerateJwtToken(user);

        httpContext?.Response.Cookies.Append(JwtCookieKey, jwtToken, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });
    }

    public void SignOut()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        httpContext?.Response.Cookies.Delete(JwtCookieKey);
        _authService.Logout();
    }
}
