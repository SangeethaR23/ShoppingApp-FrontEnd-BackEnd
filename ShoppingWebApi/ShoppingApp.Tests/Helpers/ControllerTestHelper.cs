using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ShoppingApp.Tests.Helpers;

public static class ControllerTestHelper
{
    /// <summary>Sets a fake authenticated user on the controller with the given userId and role.</summary>
    public static void SetUser(ControllerBase controller, int userId, string role = "User")
    {
        var claims = new List<Claim>
        {
            new("userId", userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role)
        };
        var identity  = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    /// <summary>Sets an unauthenticated (no userId claim) user on the controller.</summary>
    public static void SetAnonymousUser(ControllerBase controller)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
        };
    }
}
