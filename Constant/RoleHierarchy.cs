namespace itpayroll.Constant
{
    public static class RoleHierarchy
    {
        private static readonly Dictionary<string, string[]> _allowedRoles = new()
        {
            { Roles.SuperAdmin, new[] { Roles.Admin, Roles.HR, Roles.Employee } },
            { Roles.Admin, new[] { Roles.HR, Roles.Employee } },
            { Roles.HR, new[] { Roles.Employee } }
        };

        public static string[] GetAllowedRoles(string creatorRole)
        {
            return _allowedRoles.TryGetValue(creatorRole, out var roles) ? roles : Array.Empty<string>();
        }

        public static bool CanAssignRole(string creatorRole, string targetRole)
        {
            return GetAllowedRoles(creatorRole).Contains(targetRole);
        }

        public static bool IsEmployeeCreation(string creatorRole)
        {
            return creatorRole == Roles.HR;
        }
    }
}
