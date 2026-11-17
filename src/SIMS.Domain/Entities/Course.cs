namespace SIMS.Domain.Entities;

public class Course
{
    public string CourseId { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public int Credits { get; set; }
    public string Department { get; set; } = "Computing";
    public string LecturerName { get; set; } = "Dinh Van Dong";
    public string AcademicProgram { get; set; } = "BTEC HND in Computing";
    public int MaxCapacity { get; set; } = 60;
    public string EnrollmentKey { get; set; } = string.Empty;

    public bool RequiresEnrollmentKey => !string.IsNullOrWhiteSpace(EnrollmentKey);

    public Course() { }

    public Course(
        string courseId,
        string courseName,
        int credits,
        string department = "Computing",
        string lecturerName = "Dinh Van Dong",
        string academicProgram = "BTEC HND in Computing",
        int maxCapacity = 60,
        string? enrollmentKey = null)
    {
        CourseId = courseId.Trim();
        CourseName = courseName.Trim();
        Credits = credits;
        Department = department.Trim();
        LecturerName = lecturerName.Trim();
        AcademicProgram = academicProgram.Trim();
        MaxCapacity = maxCapacity;
        EnrollmentKey = enrollmentKey?.Trim() ?? string.Empty;
    }
}
