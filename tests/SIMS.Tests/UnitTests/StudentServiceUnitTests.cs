using Moq;
using SIMS.Application.Services;
using SIMS.Application.Validators;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;

namespace SIMS.Tests.UnitTests;

/// <summary>
/// Isolated Unit Tests utilizing xUnit and Moq (Report Section 3.4 &amp; Assignment Part 2 P6, P7, D2).
/// Leverages Dependency Inversion Principle (DIP) by mocking ICsvRepository&lt;Student&gt;,
/// ICsvRepository&lt;Course&gt;, and ICsvRepository&lt;Enrollment&gt; to test business logic in milliseconds
/// without touching physical disk I/O.
/// </summary>
public class StudentServiceUnitTests
{
    private readonly Mock<ICsvRepository<Student>> _mockStudentRepo;
    private readonly Mock<ICsvRepository<Course>> _mockCourseRepo;
    private readonly Mock<ICsvRepository<Enrollment>> _mockEnrollmentRepo;
    private readonly StudentService _studentService;

    public StudentServiceUnitTests()
    {
        _mockStudentRepo = new Mock<ICsvRepository<Student>>();
        _mockCourseRepo = new Mock<ICsvRepository<Course>>();
        _mockEnrollmentRepo = new Mock<ICsvRepository<Enrollment>>();

        var initialStudents = new List<Student>
        {
            new Student("BH02191", "Tran Quang Khai", "khai.tq.bh02191@btec.edu.vn", "Software Engineering", "BTEC HND in Computing", 3.92, "SE08202"),
            new Student("BH02141", "Luong The Long", "long.lt.bh02141@btec.edu.vn", "Software Engineering", "BTEC HND in Computing", 3.88, "SE08202"),
            new Student("BH10001", "Nguyen Van An", "an.nv@btec.edu.vn", "Data Science", "BTEC HND in Computing", 3.40, "DS08102")
        };

        var initialCourses = new List<Course>
        {
            new Course("UNIT20-APDP", "Unit 20: Applied Programming and Design Principles", 15, "Computing", "Dinh Van Dong", "BTEC HND in Computing", 60, "APDP2025"),
            new Course("UNIT19-DSA", "Unit 19: Data Structures and Algorithms", 15, "Computing", "Dr. Le Thi Huong", "BTEC HND in Computing", 60, "")
        };

        _mockStudentRepo.Setup(r => r.GetAll()).Returns(initialStudents);
        _mockStudentRepo.Setup(r => r.GetById(It.IsAny<string>()))
            .Returns((string id) => initialStudents.FirstOrDefault(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase)));

        _mockCourseRepo.Setup(r => r.GetAll()).Returns(initialCourses);
        _mockCourseRepo.Setup(r => r.GetById(It.IsAny<string>()))
            .Returns((string id) => initialCourses.FirstOrDefault(c => c.CourseId.Equals(id, StringComparison.OrdinalIgnoreCase)));

        _mockEnrollmentRepo.Setup(r => r.GetAll()).Returns(new List<Enrollment>());

        _studentService = new StudentService(
            _mockStudentRepo.Object,
            _mockCourseRepo.Object,
            _mockEnrollmentRepo.Object,
            new StudentValidator());
    }

    [Fact]
    public void GetStudentById_ExistingId_ReturnsStudentInConstantTimeFromDictionary()
    {
        // Act
        var student = _studentService.GetStudentById("BH02191");

        // Assert
        Assert.NotNull(student);
        Assert.Equal("Tran Quang Khai", student!.Name);
        Assert.Equal("Software Engineering", student.Major);
    }

    [Fact]
    public void AddStudent_ValidNewStudent_CallsRepositoryAddAndSave()
    {
        // Arrange
        var newStudent = new Student("BH09999", "Le Minh Khoa", "khoa.lm@btec.edu.vn", "Cyber Security", "BTEC HND in Computing", 3.65);

        // Act
        _studentService.AddStudent(newStudent);

        // Assert
        _mockStudentRepo.Verify(r => r.Add(It.Is<Student>(s => s.Id == "BH09999")), Times.Once);
        _mockStudentRepo.Verify(r => r.Save(), Times.Once);
        Assert.NotNull(_studentService.GetStudentById("BH09999"));
    }

    [Fact]
    public void AddStudent_DuplicateStudentId_ThrowsInvalidOperationExceptionViaHashSet()
    {
        // Arrange: BH02191 already exists in _registeredStudentIds HashSet
        var duplicateStudent = new Student("BH02191", "Duplicate Khai", "dup@btec.edu.vn", "Software Engineering");

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => _studentService.AddStudent(duplicateStudent));
        Assert.Contains("Duplicate Student ID", ex.Message);
        _mockStudentRepo.Verify(r => r.Add(It.IsAny<Student>()), Times.Never);
    }

    [Fact]
    public void LiskovSubstitutionPrinciple_PostgraduateStudent_RegistersAndEnrollsSeamlessly()
    {
        // Arrange (Report Section 1.3.2 LSP: PostgraduateStudent replaces Student without breaking behavior)
        Student postgrad = new PostgraduateStudent(
            "PG9001",
            "Pham Hoang Nam",
            "nam.ph@btec.edu.vn",
            "Software Engineering",
            "Large Dataset CSV Optimization",
            3.95);

        // Act
        _studentService.AddStudent(postgrad);
        var enrollment = _studentService.EnrollCourse("PG9001", "UNIT20-APDP");

        // Assert
        Assert.NotNull(enrollment);
        Assert.Equal("PG9001", enrollment.StudentId);
        Assert.Equal("UNIT20-APDP", enrollment.CourseId);
        _mockEnrollmentRepo.Verify(r => r.Add(It.IsAny<Enrollment>()), Times.Once);
    }

    [Fact]
    public void EnrollCourse_NonExistentCourse_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _studentService.EnrollCourse("BH02191", "INVALID-COURSE-999"));

        Assert.Contains("Target Course does not exist", ex.Message);
    }

    [Fact]
    public void EnrollCourse_ProtectedCourseWithWrongOrMissingKey_ThrowsInvalidOperationException()
    {
        // UNIT20-APDP has EnrollmentKey = "APDP2025"
        var exMissing = Assert.Throws<InvalidOperationException>(() =>
            _studentService.EnrollCourse("BH02191", "UNIT20-APDP", enrollmentKey: "", enforceEnrollmentKey: true));
        Assert.Contains("requires an enrollment password/code", exMissing.Message);

        var exWrong = Assert.Throws<InvalidOperationException>(() =>
            _studentService.EnrollCourse("BH02191", "UNIT20-APDP", enrollmentKey: "WRONG_KEY", enforceEnrollmentKey: true));
        Assert.Contains("Incorrect enrollment password/code", exWrong.Message);

        // Correct password succeeds
        var enrollment = _studentService.EnrollCourse("BH02191", "UNIT20-APDP", enrollmentKey: "APDP2025", enforceEnrollmentKey: true);
        Assert.NotNull(enrollment);
        Assert.Equal("UNIT20-APDP", enrollment.CourseId);
    }

    [Fact]
    public void EnrollCourse_OpenCourseWithoutPassword_EnrollsDirectly()
    {
        // UNIT19-DSA has no EnrollmentKey (Open course)
        var enrollment = _studentService.EnrollCourse("BH02191", "UNIT19-DSA", enrollmentKey: null, enforceEnrollmentKey: true);
        Assert.NotNull(enrollment);
        Assert.Equal("UNIT19-DSA", enrollment.CourseId);
    }

    [Fact]
    public void GetStudentsByMajor_ConsistentLinqFiltering_ReturnsSortedMatchingStudentsWithoutMutatingInput()
    {
        // Arrange (Report Section 2.4 Consistency with LINQ)
        var allStudents = _studentService.GetAllStudents();
        int originalCount = allStudents.Count;

        // Act
        var seStudents = _studentService.GetStudentsByMajor(allStudents, "Software Engineering").ToList();

        // Assert
        Assert.Equal(2, seStudents.Count);
        Assert.Equal("Luong The Long", seStudents[0].FullName); // Sorted alphabetically by FullName
        Assert.Equal("Tran Quang Khai", seStudents[1].FullName);
        Assert.Equal(originalCount, allStudents.Count); // Original collection not mutated
    }

    [Theory]
    [InlineData(-0.5)]
    [InlineData(4.5)]
    public void Encapsulation_StudentUpdateGrade_OutOfRangeGpa_ThrowsArgumentOutOfRangeException(double invalidGpa)
    {
        // Arrange (Report Section 1.1 Encapsulation)
        var student = new Student("BH02191", "Tran Quang Khai", "khai@btec.edu.vn");

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => student.UpdateGrade(invalidGpa));
    }
}
