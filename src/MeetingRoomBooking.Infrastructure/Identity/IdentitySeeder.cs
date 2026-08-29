using Microsoft.AspNetCore.Identity;

namespace MeetingRoomBooking.Infrastructure.Identity;

public static class IdentitySeeder
{
    public static async Task SeedRolesAsync(
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        string[] roleNames =
        [
            ApplicationRoles.User,
            ApplicationRoles.Admin
        ];

        foreach (var roleName in roleNames)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var role = new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = roleName
            };

            var result = await roleManager.CreateAsync(role);

            if (!result.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    result.Errors.Select(error => error.Description));

                throw new InvalidOperationException(
                    $"Failed to create role '{roleName}': {errors}");
            }
        }
    }

    public static async Task SeedAdminAsync(
    UserManager<ApplicationUser> userManager,
    string email,
    string password,
    string displayName)
    {
        var admin = await userManager.FindByEmailAsync(email);

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName
            };

            var createResult = await userManager.CreateAsync(
                admin,
                password);

            if (!createResult.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    createResult.Errors.Select(
                        error => error.Description));

                throw new InvalidOperationException(
                    $"Failed to create the test administrator: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(
                admin,
                ApplicationRoles.Admin))
        {
            var roleResult = await userManager.AddToRoleAsync(
                admin,
                ApplicationRoles.Admin);

            if (!roleResult.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    roleResult.Errors.Select(
                        error => error.Description));

                throw new InvalidOperationException(
                    $"Failed to assign the Admin role: {errors}");
            }
        }
    }
}