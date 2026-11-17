using System.Text;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;
using SIMS.Infrastructure.Adapters;
using SIMS.Infrastructure.FileContext;

namespace SIMS.Infrastructure.CsvRepositories;

public class UserCsvRepository : ICsvRepository<User>
{
    private readonly CsvDbContext _context;
    private readonly CsvDataFormatter _formatter;
    private readonly string _filePath;
    private readonly List<User> _cachedUsers = new();
    private bool _isLoaded;

    public UserCsvRepository(CsvDbContext? context = null, CsvDataFormatter? formatter = null)
    {
        _context = context ?? CsvDbContext.GetInstance();
        _formatter = formatter ?? new CsvDataFormatter();
        _filePath = _context.GetFilePath("users");
    }

    private void EnsureLoaded()
    {
        if (_isLoaded) return;

        lock (_context.FileSyncRoot)
        {
            if (_isLoaded) return;
            _cachedUsers.Clear();

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
                        _cachedUsers.Add(_formatter.MapRowToUser(line));
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
            }

            _isLoaded = true;
        }
    }

    public List<User> GetAll()
    {
        EnsureLoaded();
        return new List<User>(_cachedUsers);
    }

    public User? GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        EnsureLoaded();
        return _cachedUsers.FirstOrDefault(u => u.Id.Equals(id.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public void Add(User entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        EnsureLoaded();
        _cachedUsers.Add(entity);
    }

    public void Update(User entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        EnsureLoaded();
        int idx = _cachedUsers.FindIndex(u => u.Id.Equals(entity.Id, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0) _cachedUsers[idx] = entity;
    }

    public bool Delete(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        EnsureLoaded();
        return _cachedUsers.RemoveAll(u => u.Id.Equals(id.Trim(), StringComparison.OrdinalIgnoreCase)) > 0;
    }

    public void Save()
    {
        EnsureLoaded();
        lock (_context.FileSyncRoot)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Id,Name,Email,Role,Metadata,PasswordHash,MustChangePassword");
            foreach (var user in _cachedUsers)
            {
                sb.AppendLine(_formatter.FormatUserToCsvRow(user));
            }
            File.WriteAllText(_filePath, sb.ToString(), Encoding.UTF8);
        }
    }
}
