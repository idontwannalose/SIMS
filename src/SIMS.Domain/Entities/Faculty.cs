using SIMS.Domain.Enums;
using SIMS.Domain.Interfaces;

namespace SIMS.Domain.Entities;

public class Faculty : User, IGradeSubmittable
{
    public string DepartmentName { get; set; } = "Computing & IT";
    public string Specialization { get; set; } = "Applied Programming & Design Principles";

    public List<string> AssignedCourseIds { get; set; } = new();

    public Faculty() : base(string.Empty, string.Empty, string.Empty, Role.FACULTY) { }

    public Faculty(
        string id,
        string name,
        string email,
        string departmentName = "Computing & IT",
        string specialization = "Software Engineering")
        : base(id, name, email, Role.FACULTY)
    {
        DepartmentName = departmentName;
        Specialization = specialization;
    }

    public bool CanSubmitGrades(string courseId)
    {
        return !string.IsNullOrWhiteSpace(courseId);
    }

    public override string GetDisplayInfo()
    {
        return $"[FACULTY] {Id} - {Name} ({Email}) | Dept: {DepartmentName} | Specialization: {Specialization}";
    }

    public override string GetDashboard()
    {
        return "FacultyDashboard";
    }
}
