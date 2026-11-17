using System.Globalization;
using SIMS.Application.Services;
using SIMS.Domain.Entities;
using SIMS.Domain.Enums;
using SIMS.Infrastructure.Factories;

namespace SIMS.Infrastructure.Adapters;

public class CsvDataFormatter
{
    private readonly char _delimiter;

    public CsvDataFormatter(char delimiter = ',')
    {
        _delimiter = delimiter;
    }

    public IEnumerable<Student> StreamCsvLines(string filePath)
    {
        if (!File.Exists(filePath))
        {
            yield break;
        }

        using (var reader = new StreamReader(filePath))
        {
            string? headerLine = reader.ReadLine(); // Skip header
            string? currentLine;

            while ((currentLine = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(currentLine))
                {
                    continue;
                }

                Student? mapped = null;
                try
                {
                    mapped = MapRowToStudent(currentLine);
                }
                catch (FormatException)
                {
                    continue;
                }

                if (mapped != null)
                {
                    yield return mapped;
                }
            }
        }
    }

    public Student MapRowToStudent(string csvLine)
    {
        if (string.IsNullOrWhiteSpace(csvLine))
        {
            throw new FormatException("CSV line cannot be null or whitespace.");
        }

        string[] columns = SplitCsvLine(csvLine);
        if (columns.Length < 3)
        {
            throw new FormatException($"Malformed CSV row (expected at least 3 columns, got {columns.Length}): '{csvLine}'");
        }

        string id = columns[0].Trim();
        string name = columns[1].Trim();
        string email = columns[2].Trim();

        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
        {
            throw new FormatException($"Missing mandatory Student fields (Id, Name, or Email) in CSV row: '{csvLine}'");
        }

        string major = columns.Length > 3 && !string.IsNullOrWhiteSpace(columns[3])
            ? columns[3].Trim()
            : "Computer Science";

        string program = columns.Length > 4 && !string.IsNullOrWhiteSpace(columns[4])
            ? columns[4].Trim()
            : "BTEC HND in Computing";

        double gpa = 0.0;
        if (columns.Length > 5 && !string.IsNullOrWhiteSpace(columns[5]))
        {
            if (!double.TryParse(columns[5].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out gpa))
            {
                throw new FormatException($"Invalid numeric GPA value '{columns[5]}' in CSV row.");
            }
        }

        string cohort = columns.Length > 6 && !string.IsNullOrWhiteSpace(columns[6])
            ? columns[6].Trim()
            : "SE08202";

        string studentType = columns.Length > 7 ? columns[7].Trim() : "Undergraduate";
        string researchTopic = columns.Length > 8 ? columns[8].Trim() : "Distributed Systems";
        string passwordHash = columns.Length > 9 && !string.IsNullOrWhiteSpace(columns[9])
            ? columns[9].Trim()
            : SecurityTokenHelper.HashPassword(SecurityTokenHelper.DefaultUniversityPassword);
        bool mustChangePassword = columns.Length > 10 &&
                                  bool.TryParse(columns[10].Trim(), out bool parsedMustChange) &&
                                  parsedMustChange;

        Student mappedStudent;
        if (studentType.Equals("Postgraduate", StringComparison.OrdinalIgnoreCase))
        {
            mappedStudent = new PostgraduateStudent(id, name, email, major, researchTopic, gpa)
            {
                AcademicProgram = program,
                Cohort = cohort
            };
        }
        else
        {
            mappedStudent = new Student(id, name, email, major, program, gpa, cohort);
        }

        mappedStudent.SetPasswordHash(passwordHash, mustChangePassword);
        return mappedStudent;
    }

    public string FormatStudentToCsvRow(Student student)
    {
        bool isPostgrad = student is PostgraduateStudent;
        string studentType = isPostgrad ? "Postgraduate" : "Undergraduate";
        string researchTopic = student is PostgraduateStudent pg ? SanitizeField(pg.ResearchTopic) : string.Empty;
        string passwordHash = string.IsNullOrWhiteSpace(student.PasswordHash)
            ? SecurityTokenHelper.HashPassword(SecurityTokenHelper.DefaultUniversityPassword)
            : student.PasswordHash;

        return string.Join(_delimiter,
            SanitizeField(student.Id),
            SanitizeField(student.Name),
            SanitizeField(student.Email),
            SanitizeField(student.Major),
            SanitizeField(student.AcademicProgram),
            student.GPA.ToString("F2", CultureInfo.InvariantCulture),
            SanitizeField(student.Cohort),
            studentType,
            researchTopic,
            SanitizeField(passwordHash),
            student.MustChangePassword.ToString());
    }

    public Course MapRowToCourse(string csvLine)
    {
        string[] columns = SplitCsvLine(csvLine);
        if (columns.Length < 3)
        {
            throw new FormatException($"Malformed Course CSV row: '{csvLine}'");
        }

        string courseId = columns[0].Trim();
        string courseName = columns[1].Trim();
        if (!int.TryParse(columns[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int credits))
        {
            credits = 15;
        }

        string department = columns.Length > 3 ? columns[3].Trim() : "Computing";
        string lecturer = columns.Length > 4 ? columns[4].Trim() : "Dinh Van Dong";
        string program = columns.Length > 5 ? columns[5].Trim() : "BTEC HND in Computing";
        int capacity = columns.Length > 6 && int.TryParse(columns[6].Trim(), out int cap) ? cap : 60;
        string enrollmentKey = columns.Length > 7 ? columns[7].Trim() : string.Empty;

        return new Course(courseId, courseName, credits, department, lecturer, program, capacity, enrollmentKey);
    }

    public string FormatCourseToCsvRow(Course course)
    {
        return string.Join(_delimiter,
            SanitizeField(course.CourseId),
            SanitizeField(course.CourseName),
            course.Credits.ToString(CultureInfo.InvariantCulture),
            SanitizeField(course.Department),
            SanitizeField(course.LecturerName),
            SanitizeField(course.AcademicProgram),
            course.MaxCapacity.ToString(CultureInfo.InvariantCulture),
            SanitizeField(course.EnrollmentKey));
    }

    public Enrollment MapRowToEnrollment(string csvLine)
    {
        string[] columns = SplitCsvLine(csvLine);
        if (columns.Length < 4)
        {
            throw new FormatException($"Malformed Enrollment CSV row: '{csvLine}'");
        }

        string enrollmentId = columns[0].Trim();
        string studentId = columns[1].Trim();
        string courseId = columns[2].Trim();

        if (!DateTime.TryParse(columns[3].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
        {
            date = DateTime.UtcNow.Date;
        }

        double? grade = null;
        if (columns.Length > 4 && !string.IsNullOrWhiteSpace(columns[4]) &&
            double.TryParse(columns[4].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedGrade))
        {
            grade = parsedGrade;
        }

        string status = columns.Length > 5 && !string.IsNullOrWhiteSpace(columns[5])
            ? columns[5].Trim()
            : "Enrolled";

        return new Enrollment(enrollmentId, studentId, courseId, date, grade, status);
    }

    public string FormatEnrollmentToCsvRow(Enrollment enrollment)
    {
        string gradeText = enrollment.Grade.HasValue
            ? enrollment.Grade.Value.ToString("F2", CultureInfo.InvariantCulture)
            : string.Empty;

        return string.Join(_delimiter,
            SanitizeField(enrollment.EnrollmentId),
            SanitizeField(enrollment.StudentId),
            SanitizeField(enrollment.CourseId),
            enrollment.EnrollmentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            gradeText,
            SanitizeField(enrollment.Status));
    }

    public User MapRowToUser(string csvLine)
    {
        string[] columns = SplitCsvLine(csvLine);
        if (columns.Length < 4)
        {
            throw new FormatException($"Malformed User CSV row: '{csvLine}'");
        }

        string id = columns[0].Trim();
        string name = columns[1].Trim();
        string email = columns[2].Trim();
        if (!Enum.TryParse<Role>(columns[3].Trim(), ignoreCase: true, out Role role))
        {
            role = Role.STUDENT;
        }

        string? metadata = columns.Length > 4 ? columns[4].Trim() : null;
        string passwordHash = columns.Length > 5 && !string.IsNullOrWhiteSpace(columns[5])
            ? columns[5].Trim()
            : SecurityTokenHelper.HashPassword(SecurityTokenHelper.DefaultUniversityPassword);
        bool mustChangePassword = columns.Length > 6 &&
                                  bool.TryParse(columns[6].Trim(), out bool parsedMustChange) &&
                                  parsedMustChange;

        var user = UserFactory.CreateUser(role, id, name, email, metadata);
        user.SetPasswordHash(passwordHash, mustChangePassword);
        return user;
    }

    public string FormatUserToCsvRow(User user)
    {
        string metadata = user switch
        {
            Admin a => a.OfficeLocation,
            Faculty f => f.Specialization,
            GuestLecturer g => g.HomeInstitution,
            Student s => s.Major,
            _ => string.Empty
        };

        string passwordHash = string.IsNullOrWhiteSpace(user.PasswordHash)
            ? SecurityTokenHelper.HashPassword(SecurityTokenHelper.DefaultUniversityPassword)
            : user.PasswordHash;

        return string.Join(_delimiter,
            SanitizeField(user.Id),
            SanitizeField(user.Name),
            SanitizeField(user.Email),
            user.Role.ToString(),
            SanitizeField(metadata),
            SanitizeField(passwordHash),
            user.MustChangePassword.ToString());
    }

    private string[] SplitCsvLine(string line)
    {
        return line.Split(_delimiter);
    }

    private string SanitizeField(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        return input.Replace(_delimiter, ' ').Replace("\r", "").Replace("\n", "").Trim();
    }
}
