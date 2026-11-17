using System.Text.RegularExpressions;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;

namespace SIMS.Application.Validators;

public class StudentValidator
{
    private static readonly Regex EmailPattern = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public (bool IsValid, string ErrorMessage) ValidateStudentForRegistration(
        Student? student,
        ISet<string>? existingStudentIds = null)
    {
        if (student == null)
        {
            return (false, "Student entity cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(student.Id))
        {
            return (false, "Student ID is required.");
        }

        if (string.IsNullOrWhiteSpace(student.Name))
        {
            return (false, "Student Full Name is required.");
        }

        if (string.IsNullOrWhiteSpace(student.Email) || !EmailPattern.IsMatch(student.Email))
        {
            return (false, "A valid university email address is required.");
        }

        if (student.GPA < 0.0 || student.GPA > 4.0)
        {
            return (false, "Student GPA must be between 0.0 and 4.0.");
        }

        if (existingStudentIds != null && existingStudentIds.Contains(student.Id))
        {
            return (false, $"Duplicate Student ID '{student.Id}' detected.");
        }

        return (true, string.Empty);
    }

    public (bool IsValid, string ErrorMessage) ValidateEnrollment(
        Student? student,
        Course? course,
        IEnumerable<Enrollment> existingEnrollments,
        string? providedEnrollmentKey = null,
        bool enforceEnrollmentKey = false)
    {
        if (student == null)
        {
            return (false, "Student does not exist in SIMS.");
        }

        if (course == null)
        {
            return (false, "Target Course does not exist in SIMS.");
        }

        if (student is ICourseRegisterable registerable && !registerable.CanRegisterForCourse(course.CourseId))
        {
            return (false, $"Student '{student.Id}' is not eligible to register for course '{course.CourseId}'.");
        }

        bool alreadyEnrolled = existingEnrollments.Any(e =>
            e.StudentId.Equals(student.Id, StringComparison.OrdinalIgnoreCase) &&
            e.CourseId.Equals(course.CourseId, StringComparison.OrdinalIgnoreCase));

        if (alreadyEnrolled)
        {
            return (false, $"Student '{student.Id}' is already enrolled in course '{course.CourseId}'.");
        }

        if (enforceEnrollmentKey && course.RequiresEnrollmentKey)
        {
            string actualKey = course.EnrollmentKey.Trim();
            string inputKey = providedEnrollmentKey?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(inputKey))
            {
                return (false, $"Course '{course.CourseId}' requires an enrollment password/code.");
            }

            if (!string.Equals(actualKey, inputKey, StringComparison.Ordinal))
            {
                return (false, $"Incorrect enrollment password/code for course '{course.CourseId}'.");
            }
        }

        return (true, string.Empty);
    }
}
