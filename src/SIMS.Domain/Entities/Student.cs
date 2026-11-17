using SIMS.Domain.Enums;
using SIMS.Domain.Interfaces;

namespace SIMS.Domain.Entities;

public class Student : User, ICourseRegisterable
{
    private double _gpa;

    public string Major { get; set; } = "Computer Science";
    public string AcademicProgram { get; set; } = "BTEC HND in Computing";
    public string Cohort { get; set; } = "2024-2026";
    public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow.Date;

    public string FullName
    {
        get => Name;
        set => Name = value;
    }

    public double GPA
    {
        get => _gpa;
        private set => _gpa = Math.Clamp(Math.Round(value, 2), 0.0, 4.0);
    }


    public AcademicTranscript Transcript { get; private set; }

    public Student() : base(string.Empty, string.Empty, string.Empty, Role.STUDENT)
    {
        Transcript = new AcademicTranscript(string.Empty);
    }

    public Student(
        string id,
        string name,
        string email,
        string major = "Computer Science",
        string academicProgram = "BTEC HND in Computing",
        double gpa = 0.0,
        string cohort = "2024-2026")
        : base(id, name, email, Role.STUDENT)
    {
        Major = string.IsNullOrWhiteSpace(major) ? "Computer Science" : major.Trim();
        AcademicProgram = string.IsNullOrWhiteSpace(academicProgram) ? "BTEC HND in Computing" : academicProgram.Trim();
        Cohort = string.IsNullOrWhiteSpace(cohort) ? "2024-2026" : cohort.Trim();
        Transcript = new AcademicTranscript(id);
        UpdateGrade(gpa);
    }

    public void UpdateGrade(double newGpa)
    {
        if (newGpa < 0.0 || newGpa > 4.0)
        {
            throw new ArgumentOutOfRangeException(nameof(newGpa), "GPA must be between 0.0 and 4.0.");
        }
        GPA = newGpa;
    }

    public void UpdateGrade(string courseId, double courseGrade)
    {
        Transcript.RecordGrade(courseId, courseGrade);
        GPA = Transcript.CalculateCumulativeGpa();
    }

    public void SynchronizeTranscriptAndGpa(IEnumerable<Enrollment> enrollments)
    {
        Transcript = new AcademicTranscript(Id);
        if (enrollments != null)
        {
            foreach (var enrollment in enrollments.Where(e => e.Grade.HasValue))
            {
                Transcript.RecordGrade(enrollment.CourseId, enrollment.Grade!.Value);
            }
        }

        if (Transcript.CourseGrades.Count > 0)
        {
            GPA = Transcript.CalculateCumulativeGpa();
        }
    }

    public virtual bool CanRegisterForCourse(string courseId)
    {
        return !string.IsNullOrWhiteSpace(courseId) && !string.IsNullOrWhiteSpace(Id);
    }

    public virtual int GetMaxAllowedCredits() => 24;

    public override string GetDisplayInfo()
    {
        return $"[STUDENT] {Id} - {Name} ({Email}) | Major: {Major} | Program: {AcademicProgram} | GPA: {GPA:F2}";
    }

    public override string GetDashboard()
    {
        return "StudentDashboard";
    }
}
