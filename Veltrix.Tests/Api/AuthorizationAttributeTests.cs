using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.RateLimiting;
using Veltrix.API.Controllers;

namespace Veltrix.Tests.Api;

/// <summary>
/// Guards against a silent security bug: in ASP.NET Core, [AllowAnonymous] on a controller
/// OVERRIDES [Authorize(Roles = ...)] on its actions, making a protected endpoint public.
/// </summary>
public class AuthorizationAttributeTests
{
    public static IEnumerable<object[]> AllActions() =>
        typeof(AuthController).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .SelectMany(controller => controller
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any())
                .Select(action => new object[] { controller.Name, action.Name }));

    [Theory]
    [MemberData(nameof(AllActions))]
    public void ProtectedActions_AreNeverOverriddenByAllowAnonymous(string controllerName, string actionName)
    {
        var controller = typeof(AuthController).Assembly.GetTypes().Single(t => t.Name == controllerName);
        var action = controller.GetMethod(actionName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;

        var requiresAuth = action.GetCustomAttributes<AuthorizeAttribute>().Any()
                           || controller.GetCustomAttributes<AuthorizeAttribute>().Any();
        var allowsAnonymous = action.GetCustomAttributes<AllowAnonymousAttribute>().Any()
                              || controller.GetCustomAttributes<AllowAnonymousAttribute>().Any();

        // An action may be public OR protected, never both at once
        Assert.False(requiresAuth && allowsAnonymous,
            $"{controllerName}.{actionName} has [Authorize] but is also [AllowAnonymous] (which wins).");
    }

    [Theory]
    [InlineData(typeof(RoundsController), nameof(RoundsController.Cancel), "Operator,Admin")]
    [InlineData(typeof(GamesController), nameof(GamesController.Create), "Operator,Admin")]
    [InlineData(typeof(GamesController), nameof(GamesController.Update), "Operator,Admin")]
    [InlineData(typeof(GamesController), nameof(GamesController.UpdateStatus), "Operator,Admin")]
    public void StaffEndpoints_RequireTheExpectedRoles(Type controller, string actionName, string roles)
    {
        var action = controller.GetMethod(actionName)!;

        Assert.Contains(action.GetCustomAttributes<AuthorizeAttribute>(), a => a.Roles == roles);
        Assert.Empty(controller.GetCustomAttributes<AllowAnonymousAttribute>());
    }

    [Theory]
    [InlineData(typeof(BetsController))]
    [InlineData(typeof(LimitsController))]
    public void BettingAndLimits_AreForPlayersOnly(Type controller)
    {
        var authorize = controller.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorize);
        Assert.Equal("Player", authorize!.Roles);
    }

    public static IEnumerable<object[]> AdminControllers() =>
        typeof(AuthController).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t)
                        && t.GetCustomAttribute<RouteAttribute>()?.Template.StartsWith("api/admin") == true)
            .Select(t => new object[] { t.Name });

    [Fact]
    public void AdminControllers_AreFound()
    {
        // Guards the test below: if the route prefix changed, it would silently check nothing
        Assert.Equal(4, AdminControllers().Count());
    }

    [Theory]
    [MemberData(nameof(AdminControllers))]
    public void EveryApiAdminRoute_RequiresTheAdminRole_OnTheController(string controllerName)
    {
        var controller = typeof(AuthController).Assembly.GetTypes().Single(t => t.Name == controllerName);

        // On the CLASS, so a new action added later is protected automatically
        Assert.Contains(controller.GetCustomAttributes<AuthorizeAttribute>(), a => a.Roles == "Admin");

        var publicActions = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttributes<AllowAnonymousAttribute>().Any());
        Assert.Empty(publicActions);
    }

    [Theory]
    [InlineData(typeof(NotificationsController))]
    [InlineData(typeof(StatsController))]
    public void PersonalData_RequiresLogin(Type controller)
    {
        Assert.NotNull(controller.GetCustomAttribute<AuthorizeAttribute>());
    }

    [Fact]
    public void Leaderboard_IsPublic()
    {
        var action = typeof(LeaderboardController).GetMethod(nameof(LeaderboardController.Get))!;

        Assert.NotEmpty(action.GetCustomAttributes<AllowAnonymousAttribute>());
    }
}

public class RateLimitAttributeTests
{
    [Theory]
    [InlineData(nameof(AuthController.Login))]
    [InlineData(nameof(AuthController.Register))]
    [InlineData(nameof(AuthController.ForgotPassword))]
    [InlineData(nameof(AuthController.ResetPassword))]
    public void PasswordAndCodeEndpoints_AreRateLimited(string action)
    {
        var method = typeof(AuthController).GetMethod(action)!;

        Assert.Empty(method.GetCustomAttributes<DisableRateLimitingAttribute>());
        Assert.Contains(typeof(AuthController).GetCustomAttributes<EnableRateLimitingAttribute>(), a => a.PolicyName == "auth");
    }

    [Theory]
    [InlineData(nameof(AuthController.Refresh))]
    [InlineData(nameof(AuthController.Logout))]
    public void TokenEndpoints_AreNotRateLimited(string action)
    {
        Assert.NotEmpty(typeof(AuthController).GetMethod(action)!.GetCustomAttributes<DisableRateLimitingAttribute>());
    }
}
