using SIMS.Application.Interfaces;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;

namespace SIMS.Application.Services;

/// <summary>
/// Service managing university Course catalog (Use Case 3: Course Management).
/// Depends on ICsvRepository&lt;Course&gt; abstraction (DIP).
/// </summary>
public class CourseService : ICourseService
{
    private readonly ICsvRepository<Course> _courseRepo;

    public CourseService(ICsvRepository<Course> courseRepo)
    {
        _courseRepo = courseRepo ?? throw new ArgumentNullException(nameof(courseRepo));
    }

    public List<Course> GetAllCourses() => _courseRepo.GetAll();

    public Course? GetCourseById(string courseId)
    {
        if (string.IsNullOrWhiteSpace(courseId)) return null;
        return _courseRepo.GetById(courseId.Trim());
    }

    public void AddCourse(Course course)
    {
        if (course == null) throw new ArgumentNullException(nameof(course));
        if (string.IsNullOrWhiteSpace(course.CourseId))
            throw new InvalidOperationException("Course ID cannot be empty.");
        if (string.IsNullOrWhiteSpace(course.CourseName))
            throw new InvalidOperationException("Course Name cannot be empty.");
        if (course.Credits <= 0 || course.Credits > 60)
            throw new InvalidOperationException("Course credits must be between 1 and 60.");

        if (_courseRepo.GetById(course.CourseId) != null)
        {
            throw new InvalidOperationException($"Course '{course.CourseId}' already exists.");
        }

        _courseRepo.Add(course);
        _courseRepo.Save();
    }

    public void UpdateCourse(Course course)
    {
        if (course == null) throw new ArgumentNullException(nameof(course));
        _courseRepo.Update(course);
        _courseRepo.Save();
    }

    public bool DeleteCourse(string courseId)
    {
        if (string.IsNullOrWhiteSpace(courseId)) return false;
        bool deleted = _courseRepo.Delete(courseId.Trim());
        if (deleted)
        {
            _courseRepo.Save();
        }
        return deleted;
    }
}
