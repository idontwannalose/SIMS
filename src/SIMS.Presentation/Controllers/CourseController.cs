using Microsoft.AspNetCore.Mvc;
using SIMS.Application.DTOs;
using SIMS.Application.Interfaces;
using SIMS.Domain.Entities;
using SIMS.Domain.Enums;
using SIMS.Domain.Interfaces;
using SIMS.Presentation.Models;

namespace SIMS.Presentation.Controllers;

public class CourseController : Controller
{
    private readonly ICourseService _courseService;
    private readonly IStudentService _studentService;
    private readonly IAuthService _authService;
    private readonly CurrentUserContext _userContext;

    public CourseController(
        ICourseService courseService,
        IStudentService studentService,
        IAuthService authService,
        CurrentUserContext userContext)
    {
        _courseService = courseService;
        _studentService = studentService;
        _authService = authService;
        _userContext = userContext;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var currentUser = _userContext.GetCurrentUser();
        if (currentUser == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        if (currentUser.MustChangePassword)
        {
            return RedirectToAction("ChangePassword", "Auth");
        }

        ViewBag.CurrentUser = currentUser;

        var courses = _courseService.GetAllCourses();
        var studentsById = _studentService.GetStudentDirectoryById();

        var enrollments = _authService.HasRole(currentUser, Role.STUDENT)
            ? _studentService.GetStudentEnrollments(currentUser.Id)
            : _studentService.GetAllEnrollments();

        var enrollmentDetails = enrollments.Select(e => new EnrollmentDetailViewModel
        {
            Enrollment = e,
            Student = studentsById.TryGetValue(e.StudentId, out var st) ? st : null,
            Course = courses.FirstOrDefault(c => c.CourseId.Equals(e.CourseId, StringComparison.OrdinalIgnoreCase))
        }).ToList();

        var vm = new CourseManagementViewModel
        {
            CurrentUser = currentUser,
            Courses = courses,
            Enrollments = enrollmentDetails,
            HighlightedStudents = _authService.HasRole(currentUser, Role.STUDENT)
                ? new List<Student>()
                : _studentService.GetAllStudents().Take(30).ToList(),
            CanManageCourses = currentUser is ICourseManageable cm && cm.CanManageCourses(),
            CanSubmitGrades = (currentUser is IGradeSubmittable gs && gs.CanSubmitGrades("UNIT20-APDP")) || _authService.HasRole(currentUser, Role.ADMIN)
        };

        return View(vm);
    }

    [HttpPost]
    public IActionResult CreateCourse(CourseDto dto)
    {
        var currentUser = _userContext.GetCurrentUser();
        if (currentUser == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        if (currentUser.MustChangePassword)
        {
            return RedirectToAction("ChangePassword", "Auth");
        }

        if (currentUser is not ICourseManageable manageable || !manageable.CanManageCourses())
        {
            TempData["ErrorMessage"] = "Only administrators can add or modify university courses.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var course = new Course(
                dto.CourseId,
                dto.CourseName,
                dto.Credits,
                dto.Department,
                dto.LecturerName,
                dto.AcademicProgram,
                dto.MaxCapacity,
                dto.EnrollmentKey);

            _courseService.AddCourse(course);
            string keyInfo = course.RequiresEnrollmentKey
                ? $" (Protected with access code: {course.EnrollmentKey})"
                : " (Open enrollment)";
            TempData["SuccessMessage"] = $"Course '{course.CourseName}' ({course.CourseId}) has been created{keyInfo}.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult DeleteCourse(string courseId)
    {
        var currentUser = _userContext.GetCurrentUser();
        if (currentUser == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        if (currentUser.MustChangePassword)
        {
            return RedirectToAction("ChangePassword", "Auth");
        }

        if (currentUser is not ICourseManageable manageable || !manageable.CanManageCourses())
        {
            TempData["ErrorMessage"] = "Only administrators can remove courses.";
            return RedirectToAction(nameof(Index));
        }

        if (_courseService.DeleteCourse(courseId))
        {
            TempData["SuccessMessage"] = $"Course {courseId} has been removed.";
        }
        else
        {
            TempData["ErrorMessage"] = $"Course {courseId} was not found.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult AssignStudent(
        string studentId,
        string courseId,
        string? enrollmentKey = null,
        string? returnToStudentId = null)
    {
        var currentUser = _userContext.GetCurrentUser();
        if (currentUser == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        if (currentUser.MustChangePassword)
        {
            return RedirectToAction("ChangePassword", "Auth");
        }

        bool isStudentSelfEnrolling = _authService.HasRole(currentUser, Role.STUDENT);
        if (isStudentSelfEnrolling)
        {
            studentId = currentUser.Id;
        }

        try
        {
            bool enforceKey = isStudentSelfEnrolling || !string.IsNullOrWhiteSpace(enrollmentKey);
            _studentService.EnrollCourse(studentId, courseId, enrollmentKey, enforceEnrollmentKey: enforceKey);
            TempData["SuccessMessage"] = $"Successfully enrolled in course {courseId}.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        if (!string.IsNullOrWhiteSpace(returnToStudentId))
        {
            return RedirectToAction("Details", "Student", new { id = returnToStudentId });
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult SubmitGrade(string studentId, string courseId, double grade, string? returnToStudentId = null)
    {
        var currentUser = _userContext.GetCurrentUser();
        if (currentUser == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        if (currentUser.MustChangePassword)
        {
            return RedirectToAction("ChangePassword", "Auth");
        }

        bool allowed = (currentUser is IGradeSubmittable gs && gs.CanSubmitGrades(courseId))
                       || _authService.HasRole(currentUser, Role.ADMIN);

        if (!allowed)
        {
            TempData["ErrorMessage"] = "You do not have permission to record grades.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            _studentService.UpdateStudentGrade(studentId, courseId, grade);
            TempData["SuccessMessage"] = $"Grade {grade:F2} saved for student {studentId} in {courseId}.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        if (!string.IsNullOrWhiteSpace(returnToStudentId))
        {
            return RedirectToAction("Details", "Student", new { id = returnToStudentId });
        }

        return RedirectToAction(nameof(Index));
    }
}
