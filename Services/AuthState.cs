using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ResQLink.Data.Entities;

namespace ResQLink.Services
{
    public class AuthState
    {
        public bool IsAuthenticated { get; private set; }
        public User? CurrentUser { get; private set; }
        public string? CurrentRole => CurrentUser?.Role?.RoleName;
        public int? UserId => CurrentUser?.UserId;

        public event Action? AuthenticationStateChanged;

        public void SetAuthenticated(bool value, User? user = null)
        {
            IsAuthenticated = value;
            CurrentUser = value ? user : null;
            AuthenticationStateChanged?.Invoke();
        }

        public void Logout()
        {
            IsAuthenticated = false;
            CurrentUser = null;
            AuthenticationStateChanged?.Invoke();
        }

        public bool IsInRole(string roleName) =>
            IsAuthenticated &&
            CurrentRole != null &&
            CurrentRole.Equals(roleName, StringComparison.OrdinalIgnoreCase);

        public bool HasAnyRole(params string[] roles) =>
            IsAuthenticated &&
            CurrentRole != null &&
            roles.Any(r => r.Equals(CurrentRole, StringComparison.OrdinalIgnoreCase));

        // Convenience helpers
        public bool IsFinanceManager() => IsInRole("Finance Manager");
        public bool IsInventoryManager() => IsInRole("Inventory Manager");
        public bool IsAdmin() => IsInRole("Admin");
        public bool IsOperationOfficer() => IsInRole("Operation Officer");
        public bool IsVolunteer() => IsInRole("Volunteer");

        public bool CanViewInventory() =>
            HasAnyRole("Admin", "Inventory Manager", "Finance Manager");

        public bool CanAccessReports() =>
            HasAnyRole("Admin", "Finance Manager", "Operation Officer", "Inventory Manager");

        public bool CanManageBudget() => 
            IsInRole("Admin") || IsInRole("Finance Manager");

        // Operation Officer and Admin can access disaster response pages (NOT Volunteers)
        public bool CanAccessDisasterResponse() =>
            HasAnyRole("Admin", "Operation Officer") || 
            (!IsInventoryManager() && !IsFinanceManager() && !IsVolunteer());

        public bool CanAccess(string path)
        {
            if (!IsAuthenticated) return false;
            path = path.ToLowerInvariant();

            // Volunteers can ONLY access their dashboard
            if (IsVolunteer())
            {
                return path == "/volunteer-dashboard" || path == "/";
            }

            // Normalize dynamic category routes
            if (path.StartsWith("/inventory/category/"))
                return CanViewInventory();

            return path switch
            {
                "/home" => !IsVolunteer(),
                "/volunteer-dashboard" => IsVolunteer(),
                
                // Disaster response pages - accessible to Operation Officer and Admin (NOT Volunteers)
                "/disasters" => CanAccessDisasterResponse(),
                "/evacuees" => CanAccessDisasterResponse(),
                "/shelters" => CanAccessDisasterResponse(),
                "/volunteers" => CanAccessDisasterResponse(),
                
                // Inventory management - restricted
                "/inventory" => CanViewInventory(),
                "/categories" => HasAnyRole("Admin", "Inventory Manager"),
                "/stocks" => HasAnyRole("Admin", "Inventory Manager"),
                "/suppliers" => HasAnyRole("Admin", "Inventory Manager"),
                
                // Finance
                "/finance" => IsFinanceManager() || IsAdmin(),
                
                // Reports - accessible to Admin, Finance Manager, Operation Officer, and Inventory Manager
                "/reports" => CanAccessReports(),
                
                // Admin section
                "/audit-logs" => IsAdmin(),
                "/admin/audit-logs" => IsAdmin(),
                "/manage-users" => IsAdmin(),
                
                // Settings - ADMIN ONLY
                "/settings" => IsAdmin(),
                
                _ => !IsVolunteer() // default deny for volunteers
            };
        }
    }
}
