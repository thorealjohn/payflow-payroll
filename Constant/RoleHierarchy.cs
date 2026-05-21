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

        private static readonly Dictionary<string, int> _roleLevels = new()
        {
            { Roles.SuperAdmin, 4 },
            { Roles.Admin, 3 },
            { Roles.HR, 2 },
            { Roles.Employee, 1 }
        };

        public static bool CanApprove(string approverRole, string processorRole)
        {
            if (!_roleLevels.ContainsKey(approverRole) || !_roleLevels.ContainsKey(processorRole))
                return false;
            return _roleLevels[approverRole] > _roleLevels[processorRole];
        }
    }
}
