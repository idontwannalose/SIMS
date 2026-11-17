using System.Text;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;
using SIMS.Infrastructure.Adapters;
using SIMS.Infrastructure.FileContext;
namespace SIMS.Infrastructure.CsvRepositories;

public class StudentCsvRepository : ICsvRepository<Student>
{
    private readonly CsvDbContext _context;
    private readonly CsvDataFormatter _formatter;
    private readonly string _filePath;

    // 2.1 Meaningful Naming
    // Explicitly naming the dictionary to indicate an O(1) lookup structure by ID
    private readonly Dictionary<string, Student> _studentDirectoryById;
    private readonly HashSet<string> _registeredStudentIds;
    private readonly List<Student> _cachedStudents;
    private bool _isLoaded;

    public StudentCsvRepository(CsvDbContext? context = null, CsvDataFormatter? formatter = null)
    {
        _context = context ?? CsvDbContext.GetInstance();
        _formatter = formatter ?? new CsvDataFormatter();
        _filePath = _context.GetFilePath("students");

        _studentDirectoryById = new Dictionary<string, Student>(StringComparer.OrdinalIgnoreCase);

        _registeredStudentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _cachedStudents = new List<Student>();
    }

    private void EnsureLoaded()
    {
        if (_isLoaded) return;

        lock (_context.FileSyncRoot)
        {
            if (_isLoaded) return;

            _cachedStudents.Clear();
            _studentDirectoryById.Clear();
            _registeredStudentIds.Clear();

            // Uses lazy streaming from CsvDataFormatter (Report Section 2.2)
            foreach (var student in _formatter.StreamCsvLines(_filePath))
            {
                if (_registeredStudentIds.Add(student.Id))
                {
                    _cachedStudents.Add(student);
                    _studentDirectoryById[student.Id] = student;
                }
            }

            _isLoaded = true;
        }
    }

    public List<Student> GetAll()
    {
        EnsureLoaded();
        return new List<Student>(_cachedStudents);
    }

    // Exact method from Figure 3 UML Class Diagram: +GetById(id : String) : Student
    public Student? GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        EnsureLoaded();

        return _studentDirectoryById.TryGetValue(id.Trim(), out var student) ? student : null;
    }

    public void Add(Student entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        EnsureLoaded();

        if (_registeredStudentIds.Contains(entity.Id))
        {
            throw new InvalidOperationException($"Student with ID '{entity.Id}' already exists in the CSV repository.");
        }

        _cachedStudents.Insert(0, entity);
        _studentDirectoryById[entity.Id] = entity;
        _registeredStudentIds.Add(entity.Id);
    }

    public void Update(Student entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        EnsureLoaded();

        int index = _cachedStudents.FindIndex(s => s.Id.Equals(entity.Id, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            throw new KeyNotFoundException($"Student with ID '{entity.Id}' was not found for update.");
        }

        _cachedStudents[index] = entity;
        _studentDirectoryById[entity.Id] = entity;
    }

    public bool Delete(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        EnsureLoaded();

        int removedCount = _cachedStudents.RemoveAll(s => s.Id.Equals(id.Trim(), StringComparison.OrdinalIgnoreCase));
        if (removedCount > 0)
        {
            _studentDirectoryById.Remove(id.Trim());
            _registeredStudentIds.Remove(id.Trim());
            return true;
        }

        return false;
    }

    public void Save()
    {
        EnsureLoaded();

        lock (_context.FileSyncRoot)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Id,Name,Email,Major,AcademicProgram,GPA,Cohort,StudentType,ResearchTopic,PasswordHash,MustChangePassword");
            foreach (var student in _cachedStudents)
            {
                sb.AppendLine(_formatter.FormatStudentToCsvRow(student));
            }

            File.WriteAllText(_filePath, sb.ToString(), Encoding.UTF8);
        }
    }
}
