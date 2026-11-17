using SIMS.Domain.Entities;

namespace SIMS.Application.DTOs;

public class StudentRegistrationDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Major { get; set; } = "Computer Science";
    public string AcademicProgram { get; set; } = "BTEC HND in Computing";
    public double GPA { get; set; } = 0.0;
    public string Cohort { get; set; } = "2024-2026";
    public bool IsPostgraduate { get; set; }
    public string? ResearchTopic { get; set; }
}

public class CourseDto
{
    public string CourseId { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public int Credits { get; set; } = 15;
    public string Department { get; set; } = "Computing";
    public string LecturerName { get; set; } = "Dinh Van Dong";
    public string AcademicProgram { get; set; } = "BTEC HND in Computing";
    public int MaxCapacity { get; set; } = 60;
    public string? EnrollmentKey { get; set; }
}

public class EnrollmentRequestDto
{
    public string StudentId { get; set; } = string.Empty;
    public string CourseId { get; set; } = string.Empty;
    public string? EnrollmentKey { get; set; }
    public double? Grade { get; set; }
}

public class SearchExecutionResult
{
    public List<Student> Matches { get; set; } = new();
    public string StrategyName { get; set; } = string.Empty;
    public string AlgorithmicComplexity { get; set; } = string.Empty;
    public double ExecutionTimeMicroseconds { get; set; }
    public int TotalDatasetScanned { get; set; }
    public string Query { get; set; } = string.Empty;
}
