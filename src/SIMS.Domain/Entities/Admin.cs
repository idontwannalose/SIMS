using SIMS.Domain.Enums;
using SIMS.Domain.Interfaces;

namespace SIMS.Domain.Entities;

public class Admin : User, ICourseManageable
{
    public string OfficeLocation { get; set; } = "Academic Registry Office - Building A";

    public Admin() : base(string.Empty, string.Empty, string.Empty, Role.ADMIN) { }

    public Admin(string id, string name, string email, string officeLocation = "Academic Registry Office")
        : base(id, name, email, Role.ADMIN)
    {
        OfficeLocation = officeLocation;
    }

    public bool CanManageCourses() => true;

    public string GetAdministrativeScope() => "Full University SIMS Administration";

    public override string GetDisplayInfo()
    {
        return $"[ADMIN] {Id} - {Name} ({Email}) | Scope: {GetAdministrativeScope()}";
    }

    public override string GetDashboard()
    {
        return "AdminDashboard";
    }
}
