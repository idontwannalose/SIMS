using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SIMS.Application.Interfaces;
using SIMS.Domain.Entities;
using SIMS.Domain.Enums;
using SIMS.Presentation.Models;

namespace SIMS.Presentation.Controllers;

public class HomeController : Controller
{
    private readonly IStudentService _studentService;
    private readonly ICourseService _courseService;
    private readonly CurrentUserContext _userContext;

    public HomeController(
        IStudentService studentService,
        ICourseService courseService,
        CurrentUserContext userContext)
    {
        _studentService = studentService;
        _courseService = courseService;
        _userContext = userContext;
    }

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

        var allCourses = _courseService.GetAllCourses();

        if (currentUser.GetRole() == Role.STUDENT)
        {
            var studentRecord = _studentService.GetStudentById(currentUser.Id)
                                ?? new Student(currentUser.Id, currentUser.Name, currentUser.Email);
            var myEnrollments = _studentService.GetStudentEnrollments(studentRecord.Id);

            var vmStudent = new DashboardViewModel
            {
                CurrentUser = studentRecord,
                PolymorphicDashboardName = studentRecord.GetDashboard(),
                TotalCoursesInCsv = allCourses.Count,
                TotalEnrollmentsInCsv = myEnrollments.Count,
                Courses = allCourses,
                RecentEnrollments = myEnrollments,
                FeaturedStudents = new List<Student> { studentRecord }
            };
            return View(vmStudent);
        }

        // For ADMIN and FACULTY
        var allStudents = _studentService.GetAllStudents();
        var allEnrollments = _studentService.GetAllEnrollments();

        var vm = new DashboardViewModel
        {
            CurrentUser = currentUser,
            PolymorphicDashboardName = currentUser.GetDashboard(),
            TotalStudentsInCsv = allStudents.Count,
            TotalPostgraduateStudents = allStudents.Count(s => s is PostgraduateStudent),
            TotalCoursesInCsv = allCourses.Count,
            TotalEnrollmentsInCsv = allEnrollments.Count,
            FeaturedStudents = allStudents.Take(8).ToList(),
            Courses = allCourses,
            RecentEnrollments = allEnrollments.Take(8).ToList()
        };

        return View(vm);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
