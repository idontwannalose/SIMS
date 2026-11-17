using SIMS.Domain.Enums;
namespace SIMS.Domain.Entities;

public abstract class User
{
    private string _id = string.Empty;
    private string _name = string.Empty;
    private string _email = string.Empty;
    private Role _role;
    private string _passwordHash = string.Empty;
    private bool _mustChangePassword;

    public string Id
    {
        get => _id;
        set => _id = value?.Trim() ?? string.Empty;
    }

    public string Name
    {
        get => _name;
        set => _name = value?.Trim() ?? string.Empty;
    }

    public string Email
    {
        get => _email;
        set => _email = value?.Trim() ?? string.Empty;
    }

    public Role Role
    {
        get => _role;
        protected set => _role = value;
    }

    /// <summary>
    /// Encapsulated password hash (Report Section 1.1: "encapsulate sensitive data such as passwords or grades").
    /// </summary>
    public string PasswordHash
    {
        get => _passwordHash;
        private set => _passwordHash = value ?? string.Empty;
    }

    /// <summary>
    /// Indicates whether the user must change their default password upon first login.
    /// </summary>
    public bool MustChangePassword
    {
        get => _mustChangePassword;
        private set => _mustChangePassword = value;
    }

    public void SetPasswordHash(string passwordHash, bool mustChangePassword = false)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash cannot be empty.", nameof(passwordHash));
        }

        PasswordHash = passwordHash.Trim();
        MustChangePassword = mustChangePassword;
    }

    protected User() { }

    protected User(string id, string name, string email, Role role)
    {
        Id = id;
        Name = name;
        Email = email;
        Role = role;
    }

    public string GetId() => Id;
    public string getId() => Id;

    public string GetName() => Name;
    public string getName() => Name;

    public string GetEmail() => Email;
    public string getEmail() => Email;

    public Role GetRole() => Role;
    public Role getRole() => Role;

    public abstract string GetDisplayInfo();
    public string getDisplayInfo() => GetDisplayInfo();

    public abstract string GetDashboard();
}
