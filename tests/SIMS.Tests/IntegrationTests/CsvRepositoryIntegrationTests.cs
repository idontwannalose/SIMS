using System.Text;
using SIMS.Domain.Entities;
using SIMS.Infrastructure.Adapters;
using SIMS.Infrastructure.CsvRepositories;
using SIMS.Infrastructure.FileContext;

namespace SIMS.Tests.IntegrationTests;

/// <summary>
/// Data-Driven Integration Tests (Report Section 3.4 &amp; Assignment Part 2 P6, P7, D2).
/// "Integration tests are targeted specifically at the CsvStudentRepository and CsvDataFormatter.
/// These tests utilize controlled, parameterized CSV edge-case files (containing malformed rows,
/// missing delimiters, and extreme file sizes) to evaluate the system's memory management."
/// </summary>
public class CsvRepositoryIntegrationTests : IDisposable
{
    private readonly string _tempTestDir;
    private readonly CsvDataFormatter _formatter;

    public CsvRepositoryIntegrationTests()
    {
        _tempTestDir = Path.Combine(Path.GetTempPath(), $"SIMS_IntegrationTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempTestDir);
        _formatter = new CsvDataFormatter();
    }

    [Theory]
    [InlineData("BH02191,Tran Quang Khai,khai.tq.bh02191@btec.edu.vn,Software Engineering,BTEC HND in Computing,3.92,SE08202,Undergraduate,", "BH02191", "Tran Quang Khai", 3.92, false)]
    [InlineData("BH02141,Luong The Long,long.lt.bh02141@btec.edu.vn,Software Engineering,BTEC HND in Computing,3.88,SE08202,Undergraduate,", "BH02141", "Luong The Long", 3.88, false)]
    [InlineData("PG00101,Nguyen Minh Anh,anh@btec.edu.vn,Software Engineering,MSc Computing,3.95,PG-2025,Postgraduate,CSV Stream Optimization", "PG00101", "Nguyen Minh Anh", 3.95, true)]
    public void CsvDataFormatter_MapRowToStudent_ParameterizedValidRows_MapsToExpectedDomainEntity(
        string rawCsvLine,
        string expectedId,
        string expectedName,
        double expectedGpa,
        bool expectPostgraduateSubclass)
    {
        // Act
        Student mapped = _formatter.MapRowToStudent(rawCsvLine);

        // Assert
        Assert.Equal(expectedId, mapped.Id);
        Assert.Equal(expectedName, mapped.Name);
        Assert.Equal(expectedGpa, mapped.GPA);
        Assert.Equal(expectPostgraduateSubclass, mapped is PostgraduateStudent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("MalformedRowWithoutCommas")]
    [InlineData("ONLY_TWO_COLUMNS,MissingEmailColumn")]
    [InlineData(",MissingId,email@btec.edu.vn")]
    [InlineData("BH001,Valid Name,valid@btec.edu.vn,CS,BTEC,NOT_A_NUMBER_GPA")]
    public void CsvDataFormatter_MapRowToStudent_ParameterizedMalformedRows_ThrowsFormatException(string malformedLine)
    {
        // Act & Assert
        Assert.Throws<FormatException>(() => _formatter.MapRowToStudent(malformedLine));
    }

    [Fact]
    public void StreamCsvLines_FileContainingMalformedRowsAndValidRows_GracefullySkipsAnomaliesAndStreamsValidRecords()
    {
        // Arrange: Create an edge-case CSV file containing malformed rows & missing delimiters
        string edgeCaseFilePath = Path.Combine(_tempTestDir, "edge_cases.csv");
        var sb = new StringBuilder();
        sb.AppendLine("Id,Name,Email,Major,AcademicProgram,GPA,Cohort,StudentType,ResearchTopic");
        sb.AppendLine("BH02191,Tran Quang Khai,khai@btec.edu.vn,Software Engineering,BTEC HND,3.92,SE08202,Undergraduate,");
        sb.AppendLine("CORRUPTED_LINE_WITHOUT_DELIMITERS");
        sb.AppendLine(",MissingStudentId,broken@btec.edu.vn,CS,BTEC,3.0");
        sb.AppendLine("BH00999,Invalid GPA Row,badgpa@btec.edu.vn,CS,BTEC,INVALID_GPA");
        sb.AppendLine("BH02141,Luong The Long,long@btec.edu.vn,Software Engineering,BTEC HND,3.88,SE08202,Undergraduate,");
        File.WriteAllText(edgeCaseFilePath, sb.ToString(), Encoding.UTF8);

        // Act: Lazy stream via yield return (Report Section 2.2 & 3.4)
        var streamedStudents = _formatter.StreamCsvLines(edgeCaseFilePath).ToList();

        // Assert: 2 valid records recovered, 3 malformed rows safely skipped without crashing
        Assert.Equal(2, streamedStudents.Count);
        Assert.Equal("BH02191", streamedStudents[0].Id);
        Assert.Equal("BH02141", streamedStudents[1].Id);
    }

    [Fact]
    public void StudentCsvRepository_LargeDatasetPersistenceAndRetrieval_Handles2000RecordsAccurately()
    {
        // Arrange: Isolated CsvDbContext for large dataset testing
        var context = CsvDbContext.CreateIsolatedContextForTesting(_tempTestDir, seedDefaultData: false);
        var repository = new StudentCsvRepository(context, _formatter);

        // Populate 2,000 student records
        for (int i = 1; i <= 2000; i++)
        {
            var s = new Student(
                $"BH{50000 + i}",
                $"Large Dataset Student {i}",
                $"student{i}@btec.edu.vn",
                i % 2 == 0 ? "Software Engineering" : "Computer Science",
                "BTEC HND in Computing",
                3.50);
            repository.Add(s);
        }

        // Act: Save to disk and reload via a brand-new repository instance
        repository.Save();

        var reloadedRepo = new StudentCsvRepository(context, _formatter);
        var allReloaded = reloadedRepo.GetAll();
        var lookupSingle = reloadedRepo.GetById("BH51500");

        // Assert
        Assert.Equal(2000, allReloaded.Count);
        Assert.NotNull(lookupSingle);
        Assert.Equal("Large Dataset Student 1500", lookupSingle!.Name);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempTestDir))
            {
                Directory.Delete(_tempTestDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup lock in temp folder
        }
    }
}
