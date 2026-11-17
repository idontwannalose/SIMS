namespace SIMS.Domain.Entities;

public class Enrollment
{
    public string EnrollmentId { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string CourseId { get; set; } = string.Empty;
    public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow.Date;
    public double? Grade { get; set; }
    public string Status { get; set; } = "Enrolled";

    public Enrollment() { }

    public Enrollment(
        string enrollmentId,
        string studentId,
        string courseId,
        DateTime enrollmentDate,
        double? grade = null,
        string status = "Enrolled")
    {
        EnrollmentId = enrollmentId.Trim();
        StudentId = studentId.Trim();
        CourseId = courseId.Trim();
        EnrollmentDate = enrollmentDate;
        Grade = grade;
        Status = status;
    }
}
