using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SIMS.Application.DTOs;
using SIMS.Application.Interfaces;
using SIMS.Application.Services;
using SIMS.Application.Strategies;
using SIMS.Domain.Entities;
using SIMS.Domain.Enums;
using SIMS.Domain.Interfaces;
using SIMS.Presentation.Models;

namespace SIMS.Presentation.Controllers;

public class StudentController : Controller
{
    private readonly IStudentService _studentService;
    private readonly ICourseService _courseService;
    private readonly IAuthService _authService;
    private readonly CurrentUserContext _userContext;

    public StudentController(
        IStudentService studentService,
        ICourseService courseService,
        IAuthService authService,
        CurrentUserContext userContext)
    {
        _studentService = studentService;
        _courseService = courseService;
        _authService = authService;
        _userContext = userContext;
    }

    [HttpGet]
    public IActionResult Index(
        string? query = null,
        string? major = null,
        int page = 1)
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

        // Students cannot browse the global student directory; redirect them to their own transcript
        if (_authService.HasRole(currentUser, Role.STUDENT))
        {
            return RedirectToAction(nameof(Details), new { id = currentUser.Id });
        }

        ViewBag.CurrentUser = currentUser;

        var allStudents = _studentService.GetAllStudents();
        var directoryById = _studentService.GetStudentDirectoryById();

        var availableMajors = allStudents
            .Select(s => s.Major)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(m => m)
            .ToList();

        IEnumerable<Student> workingSet = allStudents;

        if (!string.IsNullOrWhiteSpace(query))
        {
            string trimmed = query.Trim();
            ISearchStrategy activeStrategy = directoryById.ContainsKey(trimmed)
                ? new HashLookupStrategy()
                : new LinearFilterStrategy();

            workingSet = activeStrategy.Search(allStudents, directoryById, trimmed).ToList();
        }

        if (!string.IsNullOrWhiteSpace(major))
        {
            workingSet = _studentService.GetStudentsByMajor(workingSet, major).ToList();
        }

        var filteredList = workingSet.ToList();

        const int pageSize = 20;
        int totalPages = Math.Max(1, (int)Math.Ceiling(filteredList.Count / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        var pagedStudents = filteredList
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var vm = new StudentDirectoryViewModel
        {
            CurrentUser = currentUser,
            Students = pagedStudents,
            AvailableMajors = availableMajors,
            SearchQuery = query,
            SelectedMajor = major,
            TotalCsvRecords = filteredList.Count,
            CurrentPage = page,
            TotalPages = totalPages,
            PageSize = pageSize
        };

        return View(vm);
    }

    [HttpGet]
    public IActionResult Create()
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

        if (!_authService.HasRole(currentUser, Role.ADMIN))
        {
            TempData["ErrorMessage"] = "You do not have permission to access student admission.";
            return RedirectToAction("Index", "Home");
        }

        ViewBag.CurrentUser = currentUser;

        return View(new StudentRegistrationDto
        {
            Id = $"BH{Random.Shared.Next(20000, 99999)}",
            Cohort = "SE08202",
            AcademicProgram = "BTEC HND in Computing",
            Major = "Software Engineering",
            GPA = 0.00
        });
    }

    [HttpPost]
    public IActionResult Create(StudentRegistrationDto dto)
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

        if (!_authService.HasRole(currentUser, Role.ADMIN))
        {
            TempData["ErrorMessage"] = "Only administrators can register new students.";
            return RedirectToAction("Index", "Home");
        }

        ViewBag.CurrentUser = currentUser;

        try
        {
            // New students start with 0.00 GPA; cumulative GPA is calculated strictly from course grades
            Student newStudent = dto.IsPostgraduate
                ? new PostgraduateStudent(
                    dto.Id,
                    dto.Name,
                    dto.Email,
                    dto.Major,
                    string.IsNullOrWhiteSpace(dto.ResearchTopic) ? "Graduate Computing Research" : dto.ResearchTopic,
                    0.0)
                : new Student(
                    dto.Id,
                    dto.Name,
                    dto.Email,
                    dto.Major,
                    dto.AcademicProgram,
                    0.0,
                    dto.Cohort);

            // Assign default university password (hashed) and require password change upon first login
            string defaultHash = SecurityTokenHelper.HashPassword(SecurityTokenHelper.DefaultUniversityPassword);
            newStudent.SetPasswordHash(defaultHash, mustChangePassword: true);

            _studentService.AddStudent(newStudent);
            TempData["SuccessMessage"] = $"Student {newStudent.Name} ({newStudent.Id}) has been registered with default password '{SecurityTokenHelper.DefaultUniversityPassword}' (must be changed on first login).";
            return RedirectToAction(nameof(Details), new { id = newStudent.Id });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return View(dto);
        }
    }

    [HttpGet]
    public IActionResult Details(string? id)
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

        // A Student can ONLY view their own academic record
        if (_authService.HasRole(currentUser, Role.STUDENT))
        {
            id = currentUser.Id;
        }
        else if (string.IsNullOrWhiteSpace(id))
        {
            id = "BH02191";
        }

        var student = _studentService.GetStudentById(id);
        if (student == null)
        {
            TempData["ErrorMessage"] = $"Student record '{id}' was not found.";
            return RedirectToAction("Index", "Home");
        }

        var allCourses = _courseService.GetAllCourses();
        var enrollments = _studentService.GetStudentEnrollments(student.Id);

        // Recalculate cumulative GPA strictly from the average of graded courses in the transcript
        if (enrollments.Any(e => e.Grade.HasValue))
        {
            double previousGpa = student.GPA;
            student.SynchronizeTranscriptAndGpa(enrollments);
            if (Math.Abs(previousGpa - student.GPA) > 0.001)
            {
                _studentService.UpdateStudent(student);
            }
        }

        var enrollmentItems = enrollments.Select(e => new EnrollmentDetailItem
        {
            Enrollment = e,
            Course = allCourses.FirstOrDefault(c => c.CourseId.Equals(e.CourseId, StringComparison.OrdinalIgnoreCase))
        }).ToList();

        var enrolledCourseIds = enrollments.Select(e => e.CourseId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var availableCourses = allCourses.Where(c => !enrolledCourseIds.Contains(c.CourseId)).ToList();

        var vm = new StudentProfileViewModel
        {
            CurrentUser = currentUser,
            Student = student,
            Enrollments = enrollmentItems,
            AvailableCoursesForEnrollment = availableCourses,
            CanRegisterCourses = currentUser is ICourseRegisterable || _authService.HasRole(currentUser, Role.ADMIN) || _authService.HasRole(currentUser, Role.FACULTY),
            CanSubmitGrades = currentUser is IGradeSubmittable || _authService.HasRole(currentUser, Role.ADMIN)
        };

        return View(vm);
    }

    [HttpPost]
    public IActionResult UpdateStudentInfo(
        string id,
        string name,
        string email,
        string major,
        string academicProgram,
        string cohort)
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

        if (_authService.HasRole(currentUser, Role.STUDENT))
        {
            TempData["ErrorMessage"] = "Students are not permitted to modify academic registry records.";
            return RedirectToAction(nameof(Details), new { id = currentUser.Id });
        }

        try
        {
            var existing = _studentService.GetStudentById(id);
            if (existing == null)
            {
                TempData["ErrorMessage"] = $"Student '{id}' not found.";
                return RedirectToAction(nameof(Index));
            }

            existing.Name = name;
            existing.Email = email;
            existing.Major = major;
            existing.AcademicProgram = academicProgram;
            existing.Cohort = cohort;
            // Cumulative GPA is NOT manually editable here; it is strictly derived from course grades.

            _studentService.UpdateStudent(existing);
            TempData["SuccessMessage"] = "Student profile updated successfully.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public IActionResult Delete(string id)
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

        if (!_authService.HasRole(currentUser, Role.ADMIN))
        {
            TempData["ErrorMessage"] = "Only administrators can delete student records.";
            return RedirectToAction(nameof(Index));
        }

        if (_studentService.DeleteStudent(id))
        {
            TempData["SuccessMessage"] = $"Student record {id} has been removed.";
        }
        else
        {
            TempData["ErrorMessage"] = $"Student {id} could not be found.";
        }

        return RedirectToAction(nameof(Index));
    }
}
