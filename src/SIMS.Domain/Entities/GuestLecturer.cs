using SIMS.Domain.Enums;
namespace SIMS.Domain.Entities;

public class GuestLecturer : User
{
    public string HomeInstitution { get; set; } = "Pearson Partner University";

    public GuestLecturer() : base(string.Empty, string.Empty, string.Empty, Role.GUEST_LECTURER) { }

    public GuestLecturer(string id, string name, string email, string homeInstitution = "Pearson Partner University")
        : base(id, name, email, Role.GUEST_LECTURER)
    {
        HomeInstitution = homeInstitution;
    }

    public override string GetDisplayInfo()
    {
        return $"[GUEST LECTURER] {Id} - {Name} ({Email}) | Institution: {HomeInstitution}";
    }

    public override string GetDashboard()
    {
        return "FacultyDashboard";
    }
}
