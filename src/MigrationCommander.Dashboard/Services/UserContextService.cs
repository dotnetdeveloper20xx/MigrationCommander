using MigrationCommander.Core.Interfaces;
using MigrationCommander.Core.Models.Security;

namespace MigrationCommander.Dashboard.Services;

/// <summary>
/// Service to manage the current user context in the dashboard.
/// </summary>
public class UserContextService
{
    private readonly IAuthorizationService _authorizationService;
    private User? _currentUser;
    private readonly object _lock = new();

    public UserContextService(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// Event raised when the current user changes.
    /// </summary>
    public event Action? OnUserChanged;

    /// <summary>
    /// Gets the current user, or a default user if not logged in.
    /// </summary>
    public User CurrentUser
    {
        get
        {
            lock (_lock)
            {
                return _currentUser ?? CreateDefaultUser();
            }
        }
    }

    /// <summary>
    /// Gets whether a user is currently logged in.
    /// </summary>
    public bool IsAuthenticated => _currentUser != null;

    /// <summary>
    /// Gets the current user's display name.
    /// </summary>
    public string DisplayName => CurrentUser.DisplayName;

    /// <summary>
    /// Gets the current user's email.
    /// </summary>
    public string Email => CurrentUser.Email;

    /// <summary>
    /// Gets the current user's ID.
    /// </summary>
    public Guid UserId => CurrentUser.Id;

    /// <summary>
    /// Gets the current user's username.
    /// </summary>
    public string Username => CurrentUser.Username;

    /// <summary>
    /// Sets the current user by username.
    /// </summary>
    public async Task SetUserAsync(string username, CancellationToken cancellationToken = default)
    {
        var user = await _authorizationService.GetUserByUsernameAsync(username, cancellationToken);
        if (user != null)
        {
            lock (_lock)
            {
                _currentUser = user;
            }
            OnUserChanged?.Invoke();
        }
    }

    /// <summary>
    /// Sets the current user by email.
    /// </summary>
    public async Task SetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _authorizationService.GetUserByEmailAsync(email, cancellationToken);
        if (user != null)
        {
            lock (_lock)
            {
                _currentUser = user;
            }
            OnUserChanged?.Invoke();
        }
    }

    /// <summary>
    /// Sets the current user by ID.
    /// </summary>
    public async Task SetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _authorizationService.GetUserByIdAsync(userId, cancellationToken);
        if (user != null)
        {
            lock (_lock)
            {
                _currentUser = user;
            }
            OnUserChanged?.Invoke();
        }
    }

    /// <summary>
    /// Logs in from an external identity provider.
    /// </summary>
    public async Task<User> LoginFromExternalAsync(
        string externalId,
        string email,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var user = await _authorizationService.GetOrCreateFromExternalAsync(
            externalId, email, displayName, cancellationToken);

        lock (_lock)
        {
            _currentUser = user;
        }

        OnUserChanged?.Invoke();
        return user;
    }

    /// <summary>
    /// Clears the current user.
    /// </summary>
    public void Logout()
    {
        lock (_lock)
        {
            _currentUser = null;
        }
        OnUserChanged?.Invoke();
    }

    /// <summary>
    /// Checks if the current user has a specific permission.
    /// </summary>
    public bool HasPermission(Permission permission)
    {
        return CurrentUser.HasPermission(permission);
    }

    /// <summary>
    /// Checks if the current user has any of the specified permissions.
    /// </summary>
    public bool HasAnyPermission(params Permission[] permissions)
    {
        return CurrentUser.HasAnyPermission(permissions);
    }

    /// <summary>
    /// Checks if the current user has all of the specified permissions.
    /// </summary>
    public bool HasAllPermissions(params Permission[] permissions)
    {
        return CurrentUser.HasAllPermissions(permissions);
    }

    /// <summary>
    /// Gets all effective permissions for the current user.
    /// </summary>
    public HashSet<Permission> GetPermissions()
    {
        return CurrentUser.EffectivePermissions;
    }

    /// <summary>
    /// Ensures the default admin user exists.
    /// </summary>
    public async Task EnsureDefaultUserExistsAsync(CancellationToken cancellationToken = default)
    {
        var existingUser = await _authorizationService.GetUserByUsernameAsync("admin", cancellationToken);
        if (existingUser != null)
        {
            return;
        }

        // Get admin role
        var adminRole = await _authorizationService.GetRoleByNameAsync("Admin", cancellationToken);
        if (adminRole == null)
        {
            return;
        }

        // Create default admin user
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            Email = "admin@localhost",
            DisplayName = "System Administrator",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            RoleIds = new List<Guid> { adminRole.Id }
        };

        try
        {
            await _authorizationService.CreateUserAsync(adminUser, cancellationToken);
        }
        catch
        {
            // User might already exist
        }
    }

    /// <summary>
    /// Initializes the service with the default user if no user is logged in.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultUserExistsAsync(cancellationToken);

        // If no user is logged in, set to admin for development
        if (_currentUser == null)
        {
            await SetUserAsync("admin", cancellationToken);
        }
    }

    private static User CreateDefaultUser()
    {
        // Return a default viewer user when not authenticated
        return new User
        {
            Id = Guid.Empty,
            Username = "anonymous",
            Email = "anonymous@localhost",
            DisplayName = "Anonymous User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Roles = new List<Role>(),
            EffectivePermissions = new HashSet<Permission>
            {
                Permission.ViewMigrations,
                Permission.ViewEnvironments,
                Permission.ViewAuditLogs
            }
        };
    }
}
