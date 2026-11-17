using SIMS.Application.Interfaces;
using SIMS.Domain.Entities;

namespace SIMS.Application.Strategies;

/// <summary>
/// Behavioral Pattern: Strategy Pattern (Report Section 4.3).
/// "For instance, the system can dynamically inject a HashLookupStrategy (achieving O(1) time complexity)
/// when searching by a unique student ID..."
/// </summary>
public class HashLookupStrategy : ISearchStrategy
{
    public string StrategyName => "HashLookupStrategy (Dictionary Hash Table)";
    public string TimeComplexity => "O(1) Constant Time";

    public IEnumerable<Student> Search(
        IEnumerable<Student> students,
        IReadOnlyDictionary<string, Student> studentDirectoryById,
        string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Enumerable.Empty<Student>();
        }

        string normalizedKey = query.Trim();
        if (studentDirectoryById != null && studentDirectoryById.TryGetValue(normalizedKey, out Student? matchedStudent))
        {
            return new[] { matchedStudent };
        }

        return Enumerable.Empty<Student>();
    }
}
