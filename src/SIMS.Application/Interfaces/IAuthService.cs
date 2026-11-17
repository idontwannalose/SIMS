using SIMS.Domain.Entities;
using SIMS.Domain.Enums;

namespace SIMS.Application.Interfaces;

/// <summary>
/// Application layer authentication contract (Figure 3 &amp; Figure 4).
/// </summary>
public interface IAuthService
{
    // Exact methods from Figure 3 UML Class Diagram
    User? Login(string email);
    void Logout();
    bool HasRole(User? user, Role role);

    // Password-verified authentication & password management
    (User? User, string ErrorMessage) Authenticate(string emailOrId, string password);
    (bool Success, string Message) ChangePassword(string emailOrId, string currentPassword, string newPassword, string confirmPassword);

    User? GetCurrentUser();
    List<User> GetAllSystemUsers();
}
