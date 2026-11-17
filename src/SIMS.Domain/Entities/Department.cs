namespace SIMS.Domain.Entities;

public class Department
{
    public string DepartmentId { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;

    /// <summary>
    /// Aggregation: Department holds references to Student objects, but does not own their lifecycles.
    /// </summary>
    private readonly List<Student> _students = new();
    public IReadOnlyList<Student> Students => _students;

    public Department(string departmentId, string departmentName)
    {
        DepartmentId = departmentId;
        DepartmentName = departmentName;
    }

    public void AttachStudent(Student student)
    {
        if (student != null && !_students.Any(s => s.Id.Equals(student.Id, StringComparison.OrdinalIgnoreCase)))
        {
            _students.Add(student);
        }
    }

    public List<Student> DissolveDepartment()
    {
        // Students survive independently when the department is dissolved
        var survivingStudents = new List<Student>(_students);
        _students.Clear();
        return survivingStudents;
    }
}
