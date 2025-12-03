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

        // Convenience helpers (added to satisfy existing UI calls)
        public bool IsFinanceManager() => IsInRole("Finance Manager");
        public bool IsInventoryManager() => IsInRole("Inventory Manager");
        public bool IsAdmin() => IsInRole("Admin");

        public bool CanViewInventory() =>
            HasAnyRole("Admin", "Inventory Manager", "Finance Manager");

        public bool CanAccessReports() =>
            HasAnyRole("Admin", "Finance Manager");

        public bool CanAccess(string path)
        {
            if (!IsAuthenticated) return false;
            path = path.ToLowerInvariant();

            // Normalize dynamic category routes
            if (path.StartsWith("/inventory/category/"))
                return CanViewInventory();

            return path switch
            {
                "/home" => true,
                "/inventory" => CanViewInventory(),
                "/categories" => HasAnyRole("Admin", "Inventory Manager"),
                "/stocks" => HasAnyRole("Admin", "Inventory Manager"),
                "/suppliers" => HasAnyRole("Admin", "Inventory Manager"),
                "/reports" => CanAccessReports(),
                "/audit-logs" => !IsInventoryManager() && !IsFinanceManager(), // visible to Admin / Volunteer / other general roles
                "/manage-users" => IsAdmin(),
                "/settings" => true,
                "/finance" => IsFinanceManager() || IsAdmin(),
                _ => true // default allow (extend as needed)
            };
        }
    }
}
