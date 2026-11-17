using SIMS.Application.Interfaces;
using SIMS.Application.Validators;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;

namespace SIMS.Application.Services;

public class StudentService : IStudentService
{
    private readonly ICsvRepository<Student> _studentRepo;
    private readonly ICsvRepository<Course> _courseRepo;
    private readonly ICsvRepository<Enrollment>? _enrollmentRepo;
    private readonly StudentValidator _validator;

    // 2.1 Meaningful Naming
    // Explicitly naming the dictionary to indicate an O(1) lookup structure by ID
    private readonly Dictionary<string, Student> _studentDirectoryById;
    private readonly HashSet<string> _registeredStudentIds;

    public StudentService(
        ICsvRepository<Student> studentRepo,
        ICsvRepository<Course> courseRepo,
        ICsvRepository<Enrollment>? enrollmentRepo = null,
        StudentValidator? validator = null)
    {
        _studentRepo = studentRepo ?? throw new ArgumentNullException(nameof(studentRepo));
        _courseRepo = courseRepo ?? throw new ArgumentNullException(nameof(courseRepo));
        _enrollmentRepo = enrollmentRepo;
        _validator = validator ?? new StudentValidator();

        _studentDirectoryById = new Dictionary<string, Student>(StringComparer.OrdinalIgnoreCase);

        // 2.3 Effective Comments
        // Using HashSet to ensure O(1) duplicate validation; 
        // List.Contains() degrades to O(n) on large CSV datasets.
        _registeredStudentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        SynchronizeInMemoryIndexes();
    }

    private void SynchronizeInMemoryIndexes()
    {
        _studentDirectoryById.Clear();
        _registeredStudentIds.Clear();

        foreach (var student in _studentRepo.GetAll())
        {
            if (!string.IsNullOrWhiteSpace(student.Id))
            {
                _studentDirectoryById[student.Id] = student;
                _registeredStudentIds.Add(student.Id);
            }
        }
    }

    public List<Student> GetAllStudents()
    {
        var students = _studentRepo.GetAll();
        SynchronizeInMemoryIndexes();
        return students;
    }

    public List<Student> getAllStudents() => GetAllStudents();

    public Student? GetStudentById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        if (_studentDirectoryById.TryGetValue(id.Trim(), out var cachedStudent))
        {
            return cachedStudent;
        }

        var fromRepo = _studentRepo.GetById(id.Trim());
        if (fromRepo != null)
        {
            _studentDirectoryById[fromRepo.Id] = fromRepo;
            _registeredStudentIds.Add(fromRepo.Id);
        }
        return fromRepo;
    }

    public Student? getStudentById(string id) => GetStudentById(id);

    public void AddStudent(Student s)
    {
        var validation = _validator.ValidateStudentForRegistration(s, _registeredStudentIds);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(validation.ErrorMessage);
        }

        _studentRepo.Add(s);
        _studentRepo.Save();

        _studentDirectoryById[s.Id] = s;
        _registeredStudentIds.Add(s.Id);
    }

    public void addStudent(Student s) => AddStudent(s);

    // Exact method from Figure 3 Class Diagram: +enrollCourse(studentId, courseId) : Enrollment
    public Enrollment EnrollCourse(string studentId, string courseId)
    {
        return EnrollCourse(studentId, courseId, enrollmentKey: null, enforceEnrollmentKey: false);
    }

    public Enrollment EnrollCourse(string studentId, string courseId, string? enrollmentKey, bool enforceEnrollmentKey = false)
    {
        var student = GetStudentById(studentId);
        var course = _courseRepo.GetById(courseId);
        var existingEnrollments = _enrollmentRepo?.GetAll() ?? new List<Enrollment>();

        var validation = _validator.ValidateEnrollment(
            student,
            course,
            existingEnrollments,
            enrollmentKey,
            enforceEnrollmentKey);

        if (!validation.IsValid)
        {
            throw new InvalidOperationException(validation.ErrorMessage);
        }

        var enrollment = new Enrollment(
            enrollmentId: $"ENR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            studentId: student!.Id,
            courseId: course!.CourseId,
            enrollmentDate: DateTime.UtcNow.Date,
            grade: null,
            status: "Enrolled");

        if (_enrollmentRepo != null)
        {
            _enrollmentRepo.Add(enrollment);
            _enrollmentRepo.Save();
        }

        return enrollment;
    }

    public Enrollment enrollCourse(string studentId, string courseId) => EnrollCourse(studentId, courseId);

    // 2.4 Consistency
    // Consistently using LINQ for filtering ensures data immutability and safe operations
    public IEnumerable<Student> GetStudentsByMajor(IEnumerable<Student> students, string major)
    {
        if (students == null) return Enumerable.Empty<Student>();
        if (string.IsNullOrWhiteSpace(major)) return students.OrderBy(s => s.FullName);

        return students
            .Where(s => s.Major.Equals(major.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.FullName)
            .Select(s => s);
    }

    public void UpdateStudent(Student student)
    {
        var validation = _validator.ValidateStudentForRegistration(student, null);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(validation.ErrorMessage);
        }

        _studentRepo.Update(student);
        _studentRepo.Save();
        _studentDirectoryById[student.Id] = student;
        _registeredStudentIds.Add(student.Id);
    }

    public bool DeleteStudent(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;

        bool removed = _studentRepo.Delete(id);
        if (removed)
        {
            _studentRepo.Save();
            _studentDirectoryById.Remove(id);
            _registeredStudentIds.Remove(id);
        }
        return removed;
    }

    public void UpdateStudentGrade(string studentId, string courseId, double grade)
    {
        var student = GetStudentById(studentId)
            ?? throw new KeyNotFoundException($"Student '{studentId}' not found.");

        if (_enrollmentRepo != null)
        {
            var allStudentEnrollments = _enrollmentRepo.GetAll()
                .Where(e => e.StudentId.Equals(studentId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var enrollment = allStudentEnrollments.FirstOrDefault(e =>
                e.CourseId.Equals(courseId, StringComparison.OrdinalIgnoreCase));

            if (enrollment != null)
            {
                enrollment.Grade = Math.Round(grade, 2);
                enrollment.Status = grade >= 2.0 ? "Completed (Pass)" : "Referral";
                _enrollmentRepo.Update(enrollment);
                _enrollmentRepo.Save();
            }

            // Synchronize all graded courses into AcademicTranscript and compute average GPA
            student.SynchronizeTranscriptAndGpa(allStudentEnrollments);
        }
        else
        {
            student.UpdateGrade(courseId, grade);
        }

        _studentRepo.Update(student);
        _studentRepo.Save();
        _studentDirectoryById[student.Id] = student;
    }

    public List<Enrollment> GetStudentEnrollments(string studentId)
    {
        if (_enrollmentRepo == null || string.IsNullOrWhiteSpace(studentId))
        {
            return new List<Enrollment>();
        }

        return _enrollmentRepo.GetAll()
            .Where(e => e.StudentId.Equals(studentId.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.EnrollmentDate)
            .ToList();
    }

    public List<Enrollment> GetAllEnrollments()
    {
        return _enrollmentRepo?.GetAll() ?? new List<Enrollment>();
    }

    public IReadOnlyDictionary<string, Student> GetStudentDirectoryById()
    {
        if (_studentDirectoryById.Count == 0)
        {
            SynchronizeInMemoryIndexes();
        }
        return _studentDirectoryById;
    }
}
