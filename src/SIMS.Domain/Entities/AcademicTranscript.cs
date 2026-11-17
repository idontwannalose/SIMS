namespace SIMS.Domain.Entities;

public class AcademicTranscript
{
    public string StudentId { get; }
    private readonly Dictionary<string, double> _courseGrades = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, double> CourseGrades => _courseGrades;

    public AcademicTranscript(string studentId)
    {
        StudentId = studentId;
    }

    public void RecordGrade(string courseId, double grade)
    {
        if (grade < 0.0 || grade > 4.0)
        {
            throw new ArgumentOutOfRangeException(nameof(grade), "Grade on 4.0 scale must be between 0.0 and 4.0.");
        }
        _courseGrades[courseId] = Math.Round(grade, 2);
    }

    public double CalculateCumulativeGpa()
    {
        if (_courseGrades.Count == 0) return 0.0;
        return Math.Round(_courseGrades.Values.Average(), 2);
    }
}
