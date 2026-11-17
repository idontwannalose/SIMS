using SIMS.Application.Interfaces;
using SIMS.Domain.Entities;

namespace SIMS.Application.Strategies;

/// <summary>
/// Behavioral Pattern: Strategy Pattern (Report Section 4.3).
/// "...but switch to a LinearFilterStrategy when querying partial names. This behavioral flexibility
/// ensures that the application executes the most memory-efficient algorithm at runtime..."
/// </summary>
public class LinearFilterStrategy : ISearchStrategy
{
    public string StrategyName => "LinearFilterStrategy (LINQ Stream Filter)";
    public string TimeComplexity => "O(n) Linear Scan";

    public IEnumerable<Student> Search(
        IEnumerable<Student> students,
        IReadOnlyDictionary<string, Student> studentDirectoryById,
        string query)
    {
        if (students == null)
        {
            return Enumerable.Empty<Student>();
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return students;
        }

        string term = query.Trim();
        return students
            .Where(s =>
                s.Id.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                s.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                s.Email.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                s.Major.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                s.AcademicProgram.Contains(term, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.FullName);
    }
}
