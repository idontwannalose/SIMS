using System.Text;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;
using SIMS.Infrastructure.Adapters;
using SIMS.Infrastructure.FileContext;

namespace SIMS.Infrastructure.CsvRepositories;

public class EnrollmentCsvRepository : ICsvRepository<Enrollment>
{
    private readonly CsvDbContext _context;
    private readonly CsvDataFormatter _formatter;
    private readonly string _filePath;
    private readonly List<Enrollment> _cachedEnrollments = new();
    private bool _isLoaded;

    public EnrollmentCsvRepository(CsvDbContext? context = null, CsvDataFormatter? formatter = null)
    {
        _context = context ?? CsvDbContext.GetInstance();
        _formatter = formatter ?? new CsvDataFormatter();
        _filePath = _context.GetFilePath("enrollments");
    }

    private void EnsureLoaded()
    {
        if (_isLoaded) return;

        lock (_context.FileSyncRoot)
        {
            if (_isLoaded) return;
            _cachedEnrollments.Clear();

            if (File.Exists(_filePath))
            {
                using var reader = new StreamReader(_filePath);
                reader.ReadLine(); // Skip header
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    try
                    {
                        _cachedEnrollments.Add(_formatter.MapRowToEnrollment(line));
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

    public List<Enrollment> GetAll()
    {
        EnsureLoaded();
        return new List<Enrollment>(_cachedEnrollments);
    }

    public Enrollment? GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        EnsureLoaded();
        return _cachedEnrollments.FirstOrDefault(e => e.EnrollmentId.Equals(id.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public void Add(Enrollment entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        EnsureLoaded();
        _cachedEnrollments.Insert(0, entity);
    }

    public void Update(Enrollment entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        EnsureLoaded();

        int idx = _cachedEnrollments.FindIndex(e => e.EnrollmentId.Equals(entity.EnrollmentId, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0)
        {
            _cachedEnrollments[idx] = entity;
        }
    }

    public bool Delete(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        EnsureLoaded();
        return _cachedEnrollments.RemoveAll(e => e.EnrollmentId.Equals(id.Trim(), StringComparison.OrdinalIgnoreCase)) > 0;
    }

    public void Save()
    {
        EnsureLoaded();

        lock (_context.FileSyncRoot)
        {
            var sb = new StringBuilder();
            sb.AppendLine("EnrollmentId,StudentId,CourseId,EnrollmentDate,Grade,Status");
            foreach (var enrollment in _cachedEnrollments)
            {
                sb.AppendLine(_formatter.FormatEnrollmentToCsvRow(enrollment));
            }

            File.WriteAllText(_filePath, sb.ToString(), Encoding.UTF8);
        }
    }
}
