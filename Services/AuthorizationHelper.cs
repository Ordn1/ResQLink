using ResQLink.Services;

namespace ResQLink.Services;

public static class AuthorizationHelper
{
    // Define which pages each role can access
    private static readonly Dictionary<string, string[]> RoleAccess = new()
    {
        {
            "Admin", new[]
            {
                "/home", "/evacuees", "/shelters", "/disasters",
                "/inventory", "/stocks", "/suppliers", "/categories",
                "/reports", "/audit-logs", "/manage-users", "/settings"
            }
        },
        {
            "Inventory Manager", new[]
            {
                "/home", "/inventory", "/stocks", "/suppliers", "/categories", "/settings"
            }
        },
        {
            "Finance Manager", new[]
            {
                "/home", "/inventory", "/reports", "/settings"
            }
        },
        {
            "Volunteer", new[]
            {
                "/home", "/evacuees", "/shelters", "/disasters", "/settings"
            }
        }
    };

    public static bool CanAccess(this AuthState authState, string path)
    {
        if (!authState.IsAuthenticated || authState.CurrentRole == null)
            return false;

        // Admin has access to everything
        if (authState.CurrentRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            return true;

        // Check if the role has access to this path
        if (RoleAccess.TryGetValue(authState.CurrentRole, out var allowedPaths))
        {
            // Normalize path
            var normalizedPath = "/" + path.TrimStart('/').ToLowerInvariant();
            
            // Check exact match or starts with allowed path
            return allowedPaths.Any(allowed => 
                normalizedPath.Equals(allowed, StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.StartsWith(allowed + "/", StringComparison.OrdinalIgnoreCase));
        }

        return false;
    }

    public static bool IsInventoryManager(this AuthState authState)
    {
        return authState.IsAuthenticated && 
               authState.CurrentRole != null &&
               authState.CurrentRole.Equals("Inventory Manager", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsFinanceManager(this AuthState authState)
    {
        return authState.IsAuthenticated && 
               authState.CurrentRole != null &&
               authState.CurrentRole.Equals("Finance Manager", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsAdmin(this AuthState authState)
    {
        return authState.IsAuthenticated && 
               authState.CurrentRole != null &&
               authState.CurrentRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);
    }

    public static bool CanAccessInventoryManagement(this AuthState authState)
    {
        // Only Admin and Inventory Manager can fully manage inventory (add/edit/delete)
        return authState.IsAdmin() || authState.IsInventoryManager();
    }

    public static bool CanViewInventory(this AuthState authState)
    {
        // Admin, Inventory Manager, and Finance Manager can view inventory
        return authState.IsAdmin() || authState.IsInventoryManager() || authState.IsFinanceManager();
    }

    public static bool CanAccessReports(this AuthState authState)
    {
        // Admin and Finance Manager can access reports
        return authState.IsAdmin() || authState.IsFinanceManager();
    }
}