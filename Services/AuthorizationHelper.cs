using ResQLink.Services;

namespace ResQLink.Services;

public static class AuthorizationHelper
{
    // Define which pages each role can access
    private static readonly Dictionary<string, string[]> RoleAccess = new()
    {
        {
            "Super Admin", new[]
            {
                "/home", "/evacuees", "/shelters", "/disasters", "/volunteers",
                "/inventory", "/stocks", "/suppliers", "/categories",
                "/finance", "/reports", "/reports/enhanced",
                "/audit-logs", "/admin/audit-logs", "/admin/archives", "/manage-users", "/settings"
            }
        },
        {
            "Admin", new[]
            {
                "/home", "/evacuees", "/shelters", "/disasters", "/volunteers",
                "/inventory", "/stocks", "/suppliers", "/categories",
                "/finance", "/reports", "/reports/enhanced",
                "/audit-logs", "/admin/audit-logs", "/admin/archives", "/manage-users", "/settings"
            }
        },
        {
            "Inventory Manager", new[]
            {
                "/home", "/inventory", "/stocks", "/suppliers", "/categories", "/reports", "/settings"
            }
        },
        {
            "Finance Manager", new[]
            {
                "/home", "/inventory", "/finance", "/reports", "/reports/enhanced", "/settings"
            }
        },
        {
            "Operation Officer", new[]
            {
                "/home", "/evacuees", "/shelters", "/disasters", "/volunteers", "/reports", "/settings"
            }
        },
        {
            "Volunteer", new[]
            {
                "/volunteer-dashboard", "/settings"
            }
        }
    };

    public static bool CanAccess(this AuthState authState, string path)
    {
        if (!authState.IsAuthenticated || authState.CurrentRole == null)
            return false;

        // Super Admin has access to everything except volunteer dashboard
        if (authState.CurrentRole.Equals("Super Admin", StringComparison.OrdinalIgnoreCase))
        {
            return !path.Equals("/volunteer-dashboard", StringComparison.OrdinalIgnoreCase);
        }

        // Admin has access to everything except volunteer dashboard
        if (authState.CurrentRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            return !path.Equals("/volunteer-dashboard", StringComparison.OrdinalIgnoreCase);
        }

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

    public static bool IsSuperAdmin(this AuthState authState)
    {
        return authState.IsAuthenticated && 
               authState.CurrentRole != null &&
               authState.CurrentRole.Equals("Super Admin", StringComparison.OrdinalIgnoreCase);
    }

    public static bool CanAccessInventoryManagement(this AuthState authState)
    {
        // Super Admin, Admin, and Inventory Manager can fully manage inventory (add/edit/delete)
        return authState.IsSuperAdmin() || authState.IsAdmin() || authState.IsInventoryManager();
    }

    public static bool CanViewInventory(this AuthState authState)
    {
        // Super Admin, Admin, Inventory Manager, and Finance Manager can view inventory
        return authState.IsSuperAdmin() || authState.IsAdmin() || authState.IsInventoryManager() || authState.IsFinanceManager();
    }

    public static bool CanAccessReports(this AuthState authState)
    {
        // Super Admin, Admin, Finance Manager, Operation Officer, and Inventory Manager can access reports
        return authState.IsSuperAdmin() || authState.IsAdmin() || authState.IsFinanceManager() || 
               (authState.CurrentRole != null && authState.CurrentRole.Equals("Operation Officer", StringComparison.OrdinalIgnoreCase)) ||
               authState.IsInventoryManager();
    }
}