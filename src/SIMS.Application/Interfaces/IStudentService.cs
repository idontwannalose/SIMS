using SIMS.Domain.Entities;

namespace SIMS.Application.Interfaces;

/// <summary>
/// Application layer service contract for Student operations (Figure 3 &amp; Figure 4).
/// </summary>
public interface IStudentService
{
    // Exact methods from Figure 3 UML Class Diagram
    List<Student> GetAllStudents();
    Student? GetStudentById(string id);
    void AddStudent(Student s);
    Enrollment EnrollCourse(string studentId, string courseId);
    Enrollment EnrollCourse(string studentId, string courseId, string? enrollmentKey, bool enforceEnrollmentKey = false);

    // Additional clean coding & CRUD operations supporting all 5 Use Cases
    void UpdateStudent(Student student);
    bool DeleteStudent(string id);
    void UpdateStudentGrade(string studentId, string courseId, double grade);
    IEnumerable<Student> GetStudentsByMajor(IEnumerable<Student> students, string major);
    List<Enrollment> GetStudentEnrollments(string studentId);
    List<Enrollment> GetAllEnrollments();
    IReadOnlyDictionary<string, Student> GetStudentDirectoryById();
}
