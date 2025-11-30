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

        public bool IsInRole(string roleName)
        {
            return IsAuthenticated && 
                   CurrentRole != null && 
                   CurrentRole.Equals(roleName, StringComparison.OrdinalIgnoreCase);
        }

        public bool HasAnyRole(params string[] roles)
        {
            return IsAuthenticated && 
                   CurrentRole != null && 
                   roles.Any(r => r.Equals(CurrentRole, StringComparison.OrdinalIgnoreCase));
        }
    }
}
