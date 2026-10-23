using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StayBnB.Data;
using StayBnB.Models.Entities;
using StayBnB.Models.Enums;
using StayBnB.Models.ViewModels;
using StayBnB.Services;

namespace StayBnB.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _db;
    private readonly IActivityLogger _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext db,
        IActivityLogger logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
        _logger = logger;
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new RegisterViewModel());
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel vm, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(vm);

        var user = new ApplicationUser
        {
            UserName = vm.Email,
            Email = vm.Email,
            FirstName = vm.FirstName.Trim(),
            LastName = vm.LastName.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, vm.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(vm);
        }

        // Every new registration starts life as a Guest.
        var roleResult = await _userManager.AddToRoleAsync(user, RoleSeeder.GuestRole);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            foreach (var error in roleResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(vm);
        }

        await _logger.LogAsync(user.Id, "Registered an account", ActivityType.Authentication, GetIp());

        await _signInManager.SignInAsync(user, isPersistent: true);

        TempData["Toast"] = $"Welcome to StayBnB, {user.FirstName}! Your account is ready.";
        return RedirectToAction("Index", "Properties");
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var result = await _signInManager.PasswordSignInAsync(vm.Email, vm.Password, vm.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(vm.Email);
            if (user is not null)
                await _logger.LogAsync(user.Id, "Logged in", ActivityType.Authentication, GetIp());

            if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
                return LocalRedirect(vm.ReturnUrl);

            return await RedirectToDashboard(user);
        }

        await _logger.LogAsync(null, $"Failed login attempt for {vm.Email}", ActivityType.Authentication, GetIp());

        ModelState.AddModelError(string.Empty, "That email and password combination is not correct.");
        return View(vm);
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _signInManager.SignOutAsync();

        if (userId is not null)
            await _logger.LogAsync(userId, "Logged out", ActivityType.Authentication, GetIp());

        TempData["Toast"] = "You have been signed out.";
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return RedirectToAction("Login");

        var vm = new ProfileViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            ProfilePictureUrl = user.ProfilePictureUrl,
            Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Guest",
            CreatedAt = user.CreatedAt
        };

        return View(vm);
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel vm)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return RedirectToAction("Login");

        if (!ModelState.IsValid)
        {
            vm.Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Guest";
            vm.CreatedAt = user.CreatedAt;
            return View(vm);
        }

        user.FirstName = vm.FirstName.Trim();
        user.LastName = vm.LastName.Trim();
        user.ProfilePictureUrl = vm.ProfilePictureUrl;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            vm.Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Guest";
            return View(vm);
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["Toast"] = "Your profile has been updated.";
        return RedirectToAction("Profile");
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task<IActionResult> RedirectToDashboard(ApplicationUser? user)
    {
        if (user is null)
            return RedirectToAction("Index", "Properties");

        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Contains(RoleSeeder.SuperAdminRole) || roles.Contains(RoleSeeder.AdminRole))
            return RedirectToAction("Index", "AdminDashboard");

        if (roles.Contains(RoleSeeder.HostRole))
            return RedirectToAction("Index", "HostDashboard");

        return RedirectToAction("Index", "Properties");
    }

    private string? GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
