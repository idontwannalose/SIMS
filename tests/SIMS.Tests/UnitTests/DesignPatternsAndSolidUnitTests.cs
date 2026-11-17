using Moq;
using SIMS.Application.Services;
using SIMS.Application.Strategies;
using SIMS.Domain.Entities;
using SIMS.Domain.Enums;
using SIMS.Domain.Interfaces;
using SIMS.Infrastructure.Factories;

namespace SIMS.Tests.UnitTests;

/// <summary>
/// Unit Tests verifying Creational (Factory Method, Singleton), Behavioral (Strategy),
/// Class Relationships (Association, Aggregation, Composition), and SOLID Principles (OCP, ISP, LSP).
/// </summary>
public class DesignPatternsAndSolidUnitTests
{
    [Theory]
    [InlineData(Role.STUDENT, typeof(Student), "StudentDashboard")]
    [InlineData(Role.FACULTY, typeof(Faculty), "FacultyDashboard")]
    [InlineData(Role.ADMIN, typeof(Admin), "AdminDashboard")]
    [InlineData(Role.GUEST_LECTURER, typeof(GuestLecturer), "FacultyDashboard")]
    public void UserFactory_CreateUser_InstantiatesCorrectConcreteSubclassAndPolymorphicDashboard(
        Role role,
        Type expectedConcreteType,
        string expectedDashboard)
    {
        // Act (Creational Pattern: Factory Method - Figure 3 & Report Section 4.1)
        User createdUser = UserFactory.CreateUser(role, "USR001", "Test User", "test@btec.edu.vn");

        // Assert
        Assert.IsType(expectedConcreteType, createdUser);
        Assert.Equal(role, createdUser.GetRole());
        Assert.Equal(expectedDashboard, createdUser.GetDashboard()); // Polymorphism (Report 1.1)
    }

    [Fact]
    public void OpenClosedPrinciple_AuthenticationService_AuthenticatesNewGuestLecturerRoleWithoutCodeModification()
    {
        // Arrange (Report Section 1.3.2 & 5.1 OCP)
        var mockStudentRepo = new Mock<ICsvRepository<Student>>();
        mockStudentRepo.Setup(r => r.GetAll()).Returns(new List<Student>());

        var mockUserRepo = new Mock<ICsvRepository<User>>();
        mockUserRepo.Setup(r => r.GetAll()).Returns(new List<User>
        {
            new GuestLecturer("GST001", "Prof. Robert C. Martin", "unclebob@pearson.uk", "Pearson UK")
        });

        var authService = new AuthenticationService(mockStudentRepo.Object, mockUserRepo.Object);

        // Act
        var loggedInUser = authService.Login("unclebob@pearson.uk");

        // Assert
        Assert.NotNull(loggedInUser);
        Assert.IsType<GuestLecturer>(loggedInUser);
        Assert.True(authService.HasRole(loggedInUser, Role.GUEST_LECTURER));
    }

    [Fact]
    public void InterfaceSegregationPrinciple_RolesImplementOnlyRelevantSegregatedContracts()
    {
        // Arrange (Report Section 1.3.2 & 5.2 ISP)
        User student = new Student("BH02191", "Tran Quang Khai", "khai@btec.edu.vn");
        User faculty = new Faculty("FAC001", "Dinh Van Dong", "dong.dv@btec.edu.vn");
        User admin = new Admin("ADM001", "System Admin", "admin@btec.edu.vn");

        // Assert: Student only implements ICourseRegisterable, NOT ICourseManageable or IGradeSubmittable
        Assert.IsAssignableFrom<ICourseRegisterable>(student);
        Assert.False(student is ICourseManageable);
        Assert.False(student is IGradeSubmittable);

        // Assert: Faculty only implements IGradeSubmittable
        Assert.IsAssignableFrom<IGradeSubmittable>(faculty);
        Assert.False(faculty is ICourseManageable);

        // Assert: Admin only implements ICourseManageable
        Assert.IsAssignableFrom<ICourseManageable>(admin);
        Assert.False(admin is ICourseRegisterable);
    }

    [Fact]
    public void ClassRelationships_AggregationAndComposition_BehaveAccordingToLifecycleRules()
    {
        // 1. Aggregation (Has-a): Department & Student (Report Section 1.2)
        var itDept = new Department("DEPT-IT", "IT Department");
        var khai = new Student("BH02191", "Tran Quang Khai", "khai@btec.edu.vn");
        var longStudent = new Student("BH02141", "Luong The Long", "long@btec.edu.vn");

        itDept.AttachStudent(khai);
        itDept.AttachStudent(longStudent);
        Assert.Equal(2, itDept.Students.Count);

        // Dissolving the department does NOT destroy the Student objects
        var reassignedStudents = itDept.DissolveDepartment();
        Assert.Empty(itDept.Students);
        Assert.Equal(2, reassignedStudents.Count);
        Assert.Equal("BH02191", reassignedStudents[0].Id);

        // 2. Composition (Part-of): Student & AcademicTranscript (Report Section 1.2)
        khai.UpdateGrade("UNIT20-APDP", 4.0);
        khai.UpdateGrade("UNIT19-DSA", 3.8);
        Assert.NotNull(khai.Transcript);
        Assert.Equal(2, khai.Transcript.CourseGrades.Count);
        Assert.Equal(3.9, khai.GPA);
    }

    [Fact]
    public void StrategyPattern_HashLookupVsLinearFilter_ReturnsAccurateMatches()
    {
        // Arrange (Report Section 4.3 Behavioral Strategy Pattern)
        var s1 = new Student("BH02191", "Tran Quang Khai", "khai@btec.edu.vn", "Software Engineering");
        var s2 = new Student("BH02141", "Luong The Long", "long@btec.edu.vn", "Software Engineering");
        var s3 = new Student("BH10005", "Nguyen Minh Anh", "anh@btec.edu.vn", "Data Science");
        var list = new List<Student> { s1, s2, s3 };
        var dict = list.ToDictionary(s => s.Id, s => s, StringComparer.OrdinalIgnoreCase);

        var hashStrategy = new HashLookupStrategy();
        var linearStrategy = new LinearFilterStrategy();

        // Act
        var exactHit = hashStrategy.Search(list, dict, "BH02191").ToList();
        var partialHits = linearStrategy.Search(list, dict, "Software").ToList();

        // Assert
        Assert.Single(exactHit);
        Assert.Equal("Tran Quang Khai", exactHit[0].Name);
        Assert.Equal(2, partialHits.Count);
    }

    [Fact]
    public void JwtAndPasswordSecurity_HashVerifyJwtAndForceFirstLoginPasswordChange_WorksEndToEnd()
    {
        // Arrange: Create a new student with default password and MustChangePassword = true
        var newStudent = new Student("BH20999", "New Admission Student", "new.student@btec.edu.vn");
        string defaultHash = SecurityTokenHelper.HashPassword(SecurityTokenHelper.DefaultUniversityPassword);
        newStudent.SetPasswordHash(defaultHash, mustChangePassword: true);

        var mockStudentRepo = new Mock<ICsvRepository<Student>>();
        mockStudentRepo.Setup(r => r.GetAll()).Returns(new List<Student> { newStudent });
        var authService = new AuthenticationService(mockStudentRepo.Object);

        // Act 1: Authenticate with default password
        var (authenticatedUser, error) = authService.Authenticate("BH20999", "Btec@123");
        Assert.NotNull(authenticatedUser);
        Assert.Empty(error);
        Assert.True(authenticatedUser!.MustChangePassword);

        // Act 2: Generate and validate signed HMAC-SHA256 JWT token
        string jwt = SecurityTokenHelper.GenerateJwtToken(authenticatedUser);
        var decodedPayload = SecurityTokenHelper.ValidateAndDecodeJwtToken(jwt);
        Assert.NotNull(decodedPayload);
        Assert.Equal("BH20999", decodedPayload!.Sub);
        Assert.True(decodedPayload.MustChangePassword);

        // Act 3: Weak password should fail validation
        var (weakSuccess, weakMsg) = authService.ChangePassword("BH20999", "Btec@123", "weak", "weak");
        Assert.False(weakSuccess);
        Assert.Contains("at least 8 characters", weakMsg);

        // Act 4: Valid strong password change succeeds and clears MustChangePassword
        var (changeSuccess, changeMsg) = authService.ChangePassword("BH20999", "Btec@123", "NewStrong@2026", "NewStrong@2026");
        Assert.True(changeSuccess, changeMsg);
        Assert.False(newStudent.MustChangePassword);
        Assert.True(SecurityTokenHelper.VerifyPassword("NewStrong@2026", newStudent.PasswordHash));
        mockStudentRepo.Verify(r => r.Update(It.Is<Student>(s => s.Id == "BH20999" && !s.MustChangePassword)), Times.Once);
        mockStudentRepo.Verify(r => r.Save(), Times.Once);
    }
}
