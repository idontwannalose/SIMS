using SIMS.Application.DTOs;
using SIMS.Domain.Entities;

namespace SIMS.Presentation.Models;

public class DashboardViewModel
{
    public User CurrentUser { get; set; } = null!;
    public string PolymorphicDashboardName { get; set; } = string.Empty;
    public string PolymorphicDisplayInfo { get; set; } = string.Empty;
    public int TotalStudentsInCsv { get; set; }
    public int TotalPostgraduateStudents { get; set; }
    public int TotalCoursesInCsv { get; set; }
    public int TotalEnrollmentsInCsv { get; set; }
    public string CsvStoragePath { get; set; } = string.Empty;
    public double CsvLoadTimeMilliseconds { get; set; }
    public List<Student> FeaturedStudents { get; set; } = new();
    public List<Course> Courses { get; set; } = new();
    public List<Enrollment> RecentEnrollments { get; set; } = new();
    public List<string> ActiveUserCapabilities { get; set; } = new();
}

public class StudentDirectoryViewModel
{
    public User CurrentUser { get; set; } = null!;
    public List<Student> Students { get; set; } = new();
    public List<string> AvailableMajors { get; set; } = new();
    public string? SearchQuery { get; set; }
    public string SelectedStrategy { get; set; } = "LinearFilter";
    public string? SelectedMajor { get; set; }
    public SearchExecutionResult? SearchMetrics { get; set; }
    public int TotalCsvRecords { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class StudentProfileViewModel
{
    public User CurrentUser { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public List<EnrollmentDetailItem> Enrollments { get; set; } = new();
    public List<Course> AvailableCoursesForEnrollment { get; set; } = new();
    public bool CanRegisterCourses { get; set; }
    public bool CanSubmitGrades { get; set; }
}

public class EnrollmentDetailItem
{
    public Enrollment Enrollment { get; set; } = null!;
    public Course? Course { get; set; }
}

public class CourseManagementViewModel
{
    public User CurrentUser { get; set; } = null!;
    public List<Course> Courses { get; set; } = new();
    public List<EnrollmentDetailViewModel> Enrollments { get; set; } = new();
    public List<Student> HighlightedStudents { get; set; } = new();
    public bool CanManageCourses { get; set; }
    public bool CanSubmitGrades { get; set; }
    public CourseDto NewCourse { get; set; } = new();
    public EnrollmentRequestDto NewEnrollment { get; set; } = new();
}

public class EnrollmentDetailViewModel
{
    public Enrollment Enrollment { get; set; } = null!;
    public Student? Student { get; set; }
    public Course? Course { get; set; }
}
