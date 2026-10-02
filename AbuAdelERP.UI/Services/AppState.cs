using System;
using System.Collections.Generic;

namespace AbuAdelERP.UI.Services
{
    public class AppState
    {
        public bool IsLoggedIn { get; private set; } = false;
        public long UserId { get; set; } = 0;
        public string EmployeeCode { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public long? DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public HashSet<string> Permissions { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public event Action? OnChange;

        public bool HasPermission(string permissionCode)
        {
            if (string.IsNullOrWhiteSpace(permissionCode)) return true;
            if (!IsLoggedIn) return false;

            // استخدام string.Equals لضمان الأمان التام ضد أي قيمة فارغة
            if (string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase)) return true;
            if (Permissions != null && Permissions.Contains("ALL")) return true;

            return Permissions != null && Permissions.Contains(permissionCode);
        }

        public void SetUserSession(long id, string empCode, string user, string name, string role, long? deptId, string dept, List<string> perms)
        {
            IsLoggedIn = true;
            UserId = id;
            EmployeeCode = !string.IsNullOrWhiteSpace(empCode) ? empCode : (!string.IsNullOrWhiteSpace(user) ? user : "EMP");
            Username = user ?? string.Empty;
            FullName = name ?? user ?? string.Empty;
            Role = role ?? "User";
            DepartmentId = deptId;
            DepartmentName = dept ?? string.Empty;
            Permissions = new HashSet<string>(perms ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            NotifyStateChanged();
        }

        public void SetUserSession(string user, string name, string role, string dept, List<string> perms)
        {
            SetUserSession(0, user, user, name, role, null, dept, perms);
        }

        public void ClearSession()
        {
            IsLoggedIn = false;
            UserId = 0;
            EmployeeCode = string.Empty;
            Username = string.Empty;
            FullName = string.Empty;
            Role = string.Empty;
            DepartmentId = null;
            DepartmentName = string.Empty;
            Permissions?.Clear();
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}