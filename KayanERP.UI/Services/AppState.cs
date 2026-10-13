using System;
using System.Collections.Generic;

namespace KayanERP.UI.Services
{
    public class AppState
    {
        public long UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public long CompanyId { get; set; } = 1;
        public string TenantName { get; set; } = string.Empty;
        public string TenantCode { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public long? DepartmentId { get; set; }

        public bool IsLoggedIn => UserId > 0 || !string.IsNullOrEmpty(Username);

        private HashSet<string> _permissions = new(StringComparer.OrdinalIgnoreCase);

        public event Action? OnChange;

        // 1. التوقيع الجديد الخاص بشاشة تسجيل دخول المستأجر (Login.razor - 9 معاملات)
        public void SetUserSession(long userId, string username, string fullName, long companyId, string companyName, string role, string empCode, string deptName, IEnumerable<string>? permissions)
        {
            UserId = userId;
            Username = username;
            FullName = fullName;
            CompanyId = companyId;
            TenantName = companyName;
            Role = role;
            EmployeeCode = empCode;
            DepartmentName = deptName;

            _permissions = new HashSet<string>(permissions ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            NotifyStateChanged();
        }

        // 2. التوقيع القديم لشاشات MainLayout و EmergencyTasks و UsersManagement (8 معاملات)
        public void SetUserSession(long userId, string username, string fullName, string role, string empCode, string deptName, object? departmentIdOrExtra, IEnumerable<string>? permissions)
        {
            UserId = userId;
            Username = username;
            FullName = fullName;
            Role = role;
            EmployeeCode = empCode;
            DepartmentName = deptName;

            if (departmentIdOrExtra is long dId) DepartmentId = dId;
            else if (long.TryParse(departmentIdOrExtra?.ToString(), out long parsedId)) DepartmentId = parsedId;

            _permissions = new HashSet<string>(permissions ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            NotifyStateChanged();
        }

        // 3. التوقيع القديم الخاص بشاشة Home.razor (5 معاملات نصية)
        public void SetUserSession(string username, string fullName, string role, string departmentName, IEnumerable<string>? permissions)
        {
            Username = username;
            FullName = fullName;
            Role = role;
            DepartmentName = departmentName;

            _permissions = new HashSet<string>(permissions ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            NotifyStateChanged();
        }

        // 4. توقيع مبسط إضافي للشركات (5 معاملات)
        public void SetUserSession(long userId, string username, string fullName, long companyId, string companyName)
        {
            UserId = userId;
            Username = username;
            FullName = fullName;
            CompanyId = companyId;
            TenantName = companyName;
            NotifyStateChanged();
        }

        public bool HasPermission(string permission)
        {
            if (string.IsNullOrWhiteSpace(permission)) return true;
            if (_permissions.Contains("*")) return true;
            return _permissions.Contains(permission.Trim());
        }

        public bool HasAnyPermission(params string[] perms)
        {
            if (_permissions.Contains("*")) return true;
            foreach (var p in perms)
            {
                if (_permissions.Contains(p.Trim())) return true;
            }
            return false;
        }

        public void ClearSession()
        {
            UserId = 0;
            Username = string.Empty;
            FullName = string.Empty;
            CompanyId = 0;
            TenantName = string.Empty;
            Role = string.Empty;
            DepartmentId = null;
            _permissions.Clear();
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}