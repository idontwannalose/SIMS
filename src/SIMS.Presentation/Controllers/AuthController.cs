using Microsoft.AspNetCore.Mvc;
using SIMS.Application.Interfaces;

namespace SIMS.Presentation.Controllers;

/// <summary>
/// Presentation Controller handling JWT-backed User Authentication and Password Management.
/// </summary>
public class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly CurrentUserContext _userContext;

    public AuthController(IAuthService authService, CurrentUserContext userContext)
    {
        _authService = authService;
        _userContext = userContext;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        var existingUser = _userContext.GetCurrentUser();
        if (existingUser != null)
        {
            if (existingUser.MustChangePassword)
            {
                return RedirectToAction(nameof(ChangePassword));
            }
            return RedirectToAction("Index", "Home");
        }

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    public IActionResult Login(string email, string password, string? returnUrl = null)
    {
        var (user, errorMessage) = _authService.Authenticate(email, password);
        if (user == null)
        {
            TempData["ErrorMessage"] = errorMessage;
            return View();
        }

        _userContext.SignIn(user);

        // Enforce mandatory password change upon first login for newly created accounts
        if (user.MustChangePassword)
        {
            TempData["WarningMessage"] = "First-time login detected. Please set a new personal password to activate your account.";
            return RedirectToAction(nameof(ChangePassword));
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult ChangePassword()
    {
        var currentUser = _userContext.GetCurrentUser();
        if (currentUser == null)
        {
            return RedirectToAction(nameof(Login));
        }

        ViewBag.CurrentUser = currentUser;
        return View(currentUser);
    }

    [HttpPost]
    public IActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
    {
        var currentUser = _userContext.GetCurrentUser();
        if (currentUser == null)
        {
            return RedirectToAction(nameof(Login));
        }

        ViewBag.CurrentUser = currentUser;

        var (success, message) = _authService.ChangePassword(
            currentUser.Email,
            currentPassword,
            newPassword,
            confirmPassword);

        if (!success)
        {
            TempData["ErrorMessage"] = message;
            return View(currentUser);
        }

        // Refresh user & re-issue signed JWT cookie with MustChangePassword = false
        var updatedUser = _authService.Login(currentUser.Email);
        if (updatedUser != null)
        {
            _userContext.SignIn(updatedUser);
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public IActionResult Logout()
    {
        _userContext.SignOut();
        return RedirectToAction(nameof(Login));
    }
}
