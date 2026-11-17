using System.Diagnostics;
using SIMS.Application.Strategies;
using SIMS.Domain.Entities;
using SIMS.Infrastructure.Factories;
using SIMS.Domain.Enums;

namespace SIMS.Tests.DeveloperTestHarness;

/// <summary>
/// Supports Assignment Part 2 Criterion M4:
/// "Discuss the differences between developer-produced and vendor-provided automatic testing tools
/// for applications and software systems."
///
/// This class is a custom Developer-Produced Automated Test Harness built from scratch without
/// relying on third-party assertion/mocking libraries, allowing direct comparison against
/// Vendor-Provided tools (xUnit + Moq + Visual Studio Test Explorer).
/// </summary>
public class CustomSimsTestHarness
{
    public record HarnessCaseResult(string TestName, bool Passed, double ElapsedMicroseconds, string Message);

    public List<HarnessCaseResult> ExecuteCustomSuite()
    {
        var results = new List<HarnessCaseResult>();

        results.Add(RunCase("CustomHarness_UserFactory_RoleCheck", () =>
        {
            var user = UserFactory.CreateUser(Role.ADMIN, "ADM001", "Admin", "admin@btec.edu.vn");
            if (user.GetRole() != Role.ADMIN || user.GetDashboard() != "AdminDashboard")
            {
                throw new Exception($"Expected ADMIN / AdminDashboard, got {user.GetRole()} / {user.GetDashboard()}");
            }
        }));

        results.Add(RunCase("CustomHarness_HashLookup_O1_Benchmark", () =>
        {
            var dict = new Dictionary<string, Student>(StringComparer.OrdinalIgnoreCase);
            var list = new List<Student>();
            for (int i = 1; i <= 1000; i++)
            {
                var st = new Student($"BH{i:D5}", $"Student {i}", $"s{i}@btec.edu.vn");
                dict[st.Id] = st;
                list.Add(st);
            }

            var strategy = new HashLookupStrategy();
            var match = strategy.Search(list, dict, "BH00999").FirstOrDefault();
            if (match == null || match.Id != "BH00999")
            {
                throw new Exception("HashLookupStrategy failed to resolve BH00999 in O(1) dictionary.");
            }
        }));

        return results;
    }

    private static HarnessCaseResult RunCase(string name, Action testBody)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            testBody();
            sw.Stop();
            return new HarnessCaseResult(name, true, Math.Round(sw.Elapsed.TotalMicroseconds, 2), "PASSED");
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new HarnessCaseResult(name, false, Math.Round(sw.Elapsed.TotalMicroseconds, 2), ex.Message);
        }
    }
}

public class DeveloperHarnessVerificationTests
{
    [Fact]
    public void DeveloperProducedTestHarness_ExecutesAllCustomCasesSuccessfully()
    {
        var harness = new CustomSimsTestHarness();
        var results = harness.ExecuteCustomSuite();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.True(r.Passed, r.Message));
    }
}
