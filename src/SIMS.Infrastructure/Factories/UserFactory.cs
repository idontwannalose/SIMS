using SIMS.Domain.Entities;
using SIMS.Domain.Enums;

namespace SIMS.Infrastructure.Factories;

public class UserFactory
{
    public static User CreateUser(Role role, string id, string name, string email, string? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("User ID is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("User Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("User Email is required.", nameof(email));

        return role switch
        {
            Role.STUDENT => new Student(id, name, email, metadata ?? "Computer Science"),
            Role.FACULTY => new Faculty(id, name, email, "Computing & IT", metadata ?? "Software Engineering"),
            Role.ADMIN => new Admin(id, name, email, metadata ?? "Academic Registry Office"),
            Role.GUEST_LECTURER => new GuestLecturer(id, name, email, metadata ?? "Pearson Partner University"),
            _ => throw new NotSupportedException($"Role '{role}' is not supported by UserFactory.")
        };
    }
}
