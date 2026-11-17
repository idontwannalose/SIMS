using SIMS.Application.Interfaces;
using SIMS.Domain.Entities;
using SIMS.Domain.Enums;
using SIMS.Domain.Interfaces;

namespace SIMS.Application.Services;

/// <summary>
/// Authentication and Role-Based Access Control (RBAC) Service (Figure 3 UML Class Diagram).
/// Supports SHA-256 password verification, first-login forced password change, and password updates.
/// </summary>
public class AuthenticationService : IAuthService
{
    private readonly ICsvRepository<Student> _studentRepo;
    private readonly ICsvRepository<User>? _userRepo;
    private User? _currentUser;

    public AuthenticationService(
        ICsvRepository<Student> studentRepo,
        ICsvRepository<User>? userRepo = null)
    {
        _studentRepo = studentRepo ?? throw new ArgumentNullException(nameof(studentRepo));
        _userRepo = userRepo;
    }

    // Exact method from Figure 3 Class Diagram: +login(email : String) : User
    public User? Login(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        string trimmedEmail = email.Trim();

        // 1. Check staff/system users repository (Admin, Faculty, GuestLecturer)
        if (_userRepo != null)
        {
            var matchedStaff = _userRepo.GetAll().FirstOrDefault(u =>
                u.Email.Equals(trimmedEmail, StringComparison.OrdinalIgnoreCase) ||
                u.Id.Equals(trimmedEmail, StringComparison.OrdinalIgnoreCase));

            if (matchedStaff != null && matchedStaff.Role != Role.STUDENT)
            {
                _currentUser = matchedStaff;
                return _currentUser;
            }
        }

        // 2. Check student repository (Figure 3: -studentRepo : ICsvRepository<Student>)
        var matchedStudent = _studentRepo.GetAll().FirstOrDefault(s =>
            s.Email.Equals(trimmedEmail, StringComparison.OrdinalIgnoreCase) ||
            s.Id.Equals(trimmedEmail, StringComparison.OrdinalIgnoreCase));

        if (matchedStudent != null)
        {
            _currentUser = matchedStudent;
            return _currentUser;
        }

        // 3. Fallback to any user in userRepo
        if (_userRepo != null)
        {
            var matchedAny = _userRepo.GetAll().FirstOrDefault(u =>
                u.Email.Equals(trimmedEmail, StringComparison.OrdinalIgnoreCase) ||
                u.Id.Equals(trimmedEmail, StringComparison.OrdinalIgnoreCase));

            if (matchedAny != null)
            {
                _currentUser = matchedAny;
                return _currentUser;
            }
        }

        return null;
    }

    public User? login(string email) => Login(email);

    public (User? User, string ErrorMessage) Authenticate(string emailOrId, string password)
    {
        if (string.IsNullOrWhiteSpace(emailOrId))
        {
            return (null, "Please enter your University Email or ID.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return (null, "Please enter your password.");
        }

        var user = Login(emailOrId);
        if (user == null)
        {
            return (null, "Invalid account or password.");
        }

        if (!SecurityTokenHelper.VerifyPassword(password, user.PasswordHash))
        {
            return (null, "Invalid account or password.");
        }

        _currentUser = user;
        return (user, string.Empty);
    }

    public (bool Success, string Message) ChangePassword(
        string emailOrId,
        string currentPassword,
        string newPassword,
        string confirmPassword)
    {
        var user = Login(emailOrId);
        if (user == null)
        {
            return (false, "User account could not be found.");
        }

        if (!SecurityTokenHelper.VerifyPassword(currentPassword, user.PasswordHash))
        {
            return (false, "Your current password is incorrect.");
        }

        var validation = SecurityTokenHelper.ValidateNewPassword(currentPassword, newPassword, confirmPassword);
        if (!validation.IsValid)
        {
            return (false, validation.ErrorMessage);
        }

        string newHash = SecurityTokenHelper.HashPassword(newPassword);
        user.SetPasswordHash(newHash, mustChangePassword: false);

        // Persist updated password hash and MustChangePassword=false to CSV storage
        if (user is Student student)
        {
            _studentRepo.Update(student);
            _studentRepo.Save();
        }

        if (_userRepo != null)
        {
            var staffRecord = _userRepo.GetById(user.Id);
            if (staffRecord != null)
            {
                staffRecord.SetPasswordHash(newHash, mustChangePassword: false);
                _userRepo.Update(staffRecord);
                _userRepo.Save();
            }
        }

        return (true, "Your password has been updated successfully.");
    }

    // Exact method from Figure 3 Class Diagram: +logout() : void
    public void Logout()
    {
        _currentUser = null;
    }

    public void logout() => Logout();

    // Exact method from Figure 3 Class Diagram: +hasRole(user : User, role : Role) : boolean
    public bool HasRole(User? user, Role role)
    {
        if (user == null)
        {
            return false;
        }

        return user.GetRole() == role;
    }

    public bool hasRole(User? user, Role role) => HasRole(user, role);

    public User? GetCurrentUser() => _currentUser;

    public List<User> GetAllSystemUsers()
    {
        var allUsers = new List<User>();
        if (_userRepo != null)
        {
            allUsers.AddRange(_userRepo.GetAll());
        }

        foreach (var student in _studentRepo.GetAll().Take(15))
        {
            if (!allUsers.Any(u => u.Email.Equals(student.Email, StringComparison.OrdinalIgnoreCase)))
            {
                allUsers.Add(student);
            }
        }

        return allUsers;
    }
}
