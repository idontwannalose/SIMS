using SIMS.Domain.Entities;

namespace SIMS.Application.Interfaces;

public interface ICourseService
{
    List<Course> GetAllCourses();
    Course? GetCourseById(string courseId);
    void AddCourse(Course course);
    void UpdateCourse(Course course);
    bool DeleteCourse(string courseId);
}
