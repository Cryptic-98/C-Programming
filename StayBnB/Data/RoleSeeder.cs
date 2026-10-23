using Microsoft.AspNetCore.Identity;
using StayBnB.Models.Entities;

namespace StayBnB.Data;

/// <summary>
/// Creates the four application roles and the SuperAdmin account on startup.
/// The SuperAdmin password is read from configuration (appsettings / user secrets)
/// and is never stored in the database script.
/// </summary>
public static class RoleSeeder
{
    public const string GuestRole = "Guest";
    public const string HostRole = "Host";
    public const string AdminRole = "Admin";
    public const string SuperAdminRole = "SuperAdmin";

    public static readonly string[] AllRoles = { GuestRole, HostRole, AdminRole, SuperAdminRole };

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in AllRoles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                    throw new InvalidOperationException(
                        $"Could not create the {role} role: " + string.Join(" ", result.Errors.Select(e => e.Description)));
            }
        }

        var email = configuration["SuperAdmin:Email"] ?? "superadmin@staybnb.co.za";
        var password = configuration["SuperAdmin:Password"];

        var superAdmin = await userManager.FindByEmailAsync(email);

        if (superAdmin is null)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException(
                    "Set SuperAdmin:Password in user secrets or environment configuration before creating the SuperAdmin account.");

            superAdmin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = "Super",
                LastName = "Admin",
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(superAdmin, password);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    "Could not create the SuperAdmin account: " + string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        // The SuperAdmin is placed in both SuperAdmin and Admin so shared admin
        // pages only need [Authorize(Roles = "Admin,SuperAdmin")].
        if (!await userManager.IsInRoleAsync(superAdmin, SuperAdminRole))
        {
            var result = await userManager.AddToRoleAsync(superAdmin, SuperAdminRole);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    "Could not assign the SuperAdmin role: " + string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        if (!await userManager.IsInRoleAsync(superAdmin, AdminRole))
        {
            var result = await userManager.AddToRoleAsync(superAdmin, AdminRole);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    "Could not assign the Admin role to the SuperAdmin: " + string.Join(" ", result.Errors.Select(e => e.Description)));
        }
    }
}
