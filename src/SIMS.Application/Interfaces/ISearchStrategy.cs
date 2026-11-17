using SIMS.Domain.Entities;

namespace SIMS.Application.Interfaces;

/// <summary>
/// Behavioral Design Pattern: Strategy Pattern (Report Section 4.3).
/// "Applying the Strategy Pattern allows the system to encapsulate different search algorithms
/// into interchangeable classes that implement an ISearchStrategy interface."
/// </summary>
public interface ISearchStrategy
{
    string StrategyName { get; }
    string TimeComplexity { get; }
    IEnumerable<Student> Search(
        IEnumerable<Student> students,
        IReadOnlyDictionary<string, Student> studentDirectoryById,
        string query);
}
