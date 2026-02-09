namespace MAA.Domain.Sessions;

/// <summary>
/// User role enumeration for role-based access control.
/// Defines authorization levels for different system areas.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Anonymous public user (no login required).
    /// Can access: Public wizard, eligibility check, document upload.
    /// </summary>
    Anonymous = 0,

    /// <summary>
    /// Registered user (Phase 5).
    /// Can access: All anonymous features + session save/resume, account management.
    /// </summary>
    User = 1,

    /// <summary>
    /// Analyst role (internal staff).
    /// Can access: User features + read-only admin dashboard, analytics, reports.
    /// Cannot: Modify rules, approve changes, manage users.
    /// </summary>
    Analyst = 10,

    /// <summary>
    /// Reviewer role (compliance staff).
    /// Can access: Analyst features + rule approval queue, review pending changes.
    /// Cannot: Create rules, manage users.
    /// </summary>
    Reviewer = 20,

    /// <summary>
    /// Admin role (full access).
    /// Can access: All features including rule management, user management, system config.
    /// </summary>
    Admin = 100
}

/// <summary>
/// Extension methods for UserRole enum.
/// </summary>
public static class UserRoleExtensions
{
    /// <summary>
    /// Checks if role has admin privileges (Analyst, Reviewer, or Admin).
    /// </summary>
    public static bool IsAdminRole(this UserRole role)
    {
        return role >= UserRole.Analyst;
    }

    /// <summary>
    /// Checks if role can approve rules (Reviewer or Admin).
    /// </summary>
    public static bool CanApproveRules(this UserRole role)
    {
        return role >= UserRole.Reviewer;
    }

    /// <summary>
    /// Checks if role can manage users and system config (Admin only).
    /// </summary>
    public static bool CanManageSystem(this UserRole role)
    {
        return role == UserRole.Admin;
    }

    /// <summary>
    /// Parses role string to UserRole enum.
    /// </summary>
    public static UserRole ParseRole(string roleString)
    {
        if (string.IsNullOrWhiteSpace(roleString))
            return UserRole.Anonymous;

        if (Enum.TryParse<UserRole>(roleString, ignoreCase: true, out var role))
            return role;

        return UserRole.Anonymous;
    }
}
