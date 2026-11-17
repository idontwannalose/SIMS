using System.Text;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;
using SIMS.Infrastructure.Adapters;
using SIMS.Infrastructure.FileContext;

namespace SIMS.Infrastructure.CsvRepositories;

public class CourseCsvRepository : ICsvRepository<Course>
{
    private readonly CsvDbContext _context;
    private readonly CsvDataFormatter _formatter;
    private readonly string _filePath;
    private readonly List<Course> _cachedCourses = new();
    private bool _isLoaded;

    public CourseCsvRepository(CsvDbContext? context = null, CsvDataFormatter? formatter = null)
    {
        _context = context ?? CsvDbContext.GetInstance();
        _formatter = formatter ?? new CsvDataFormatter();
        _filePath = _context.GetFilePath("courses");
    }

    private void EnsureLoaded()
    {
        if (_isLoaded) return;

        lock (_context.FileSyncRoot)
        {
            if (_isLoaded) return;
            _cachedCourses.Clear();

            if (File.Exists(_filePath))
            {
                using var reader = new StreamReader(_filePath);
                reader.ReadLine(); // Skip CSV header
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    try
                    {
                        _cachedCourses.Add(_formatter.MapRowToCourse(line));
                    }
                    catch (FormatException)
                    {
                        continue;
                    }
                }
            }

            _isLoaded = true;
        }
    }

    public List<Course> GetAll()
    {
        EnsureLoaded();
        return new List<Course>(_cachedCourses);
    }

    public Course? GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        EnsureLoaded();
        return _cachedCourses.FirstOrDefault(c => c.CourseId.Equals(id.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public void Add(Course entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        EnsureLoaded();

        if (_cachedCourses.Any(c => c.CourseId.Equals(entity.CourseId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Course '{entity.CourseId}' already exists.");
        }

        _cachedCourses.Add(entity);
    }

    public void Update(Course entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        EnsureLoaded();

        int idx = _cachedCourses.FindIndex(c => c.CourseId.Equals(entity.CourseId, StringComparison.OrdinalIgnoreCase));
        if (idx < 0)
        {
            throw new KeyNotFoundException($"Course '{entity.CourseId}' not found.");
        }

        _cachedCourses[idx] = entity;
    }

    public bool Delete(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        EnsureLoaded();
        return _cachedCourses.RemoveAll(c => c.CourseId.Equals(id.Trim(), StringComparison.OrdinalIgnoreCase)) > 0;
    }

    public void Save()
    {
        EnsureLoaded();

        lock (_context.FileSyncRoot)
        {
            var sb = new StringBuilder();
            sb.AppendLine("CourseId,CourseName,Credits,Department,LecturerName,AcademicProgram,MaxCapacity,EnrollmentKey");
            foreach (var course in _cachedCourses)
            {
                sb.AppendLine(_formatter.FormatCourseToCsvRow(course));
            }

            File.WriteAllText(_filePath, sb.ToString(), Encoding.UTF8);
        }
    }
}
