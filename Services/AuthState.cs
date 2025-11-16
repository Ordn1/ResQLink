using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResQLink.Services
{
    public class AuthState
    {
        public bool IsAuthenticated { get; private set; }

        public event Action? AuthenticationStateChanged;

        public void SetAuthenticated(bool value)
        {
            IsAuthenticated = value;
            AuthenticationStateChanged?.Invoke();
        }
    }
}
