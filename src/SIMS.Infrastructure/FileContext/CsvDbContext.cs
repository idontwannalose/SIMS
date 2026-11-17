using System.Text;

namespace SIMS.Infrastructure.FileContext;

public sealed class CsvDbContext
{
    private static CsvDbContext? _instance;
    private static readonly object InstanceLock = new();

    private string _basePath;
    public object FileSyncRoot { get; } = new();

    public string BasePath => _basePath;

    private CsvDbContext(string basePath)
    {
        _basePath = basePath;
        EnsureDirectoryAndSeedData();
    }

    public static CsvDbContext GetInstance(string? customBasePath = null)
    {
        lock (InstanceLock)
        {
            if (_instance == null)
            {
                string resolvedPath = string.IsNullOrWhiteSpace(customBasePath)
                    ? Path.Combine(AppContext.BaseDirectory, "CsvData")
                    : customBasePath;
                _instance = new CsvDbContext(resolvedPath);
            }
            else if (!string.IsNullOrWhiteSpace(customBasePath) &&
                     !string.Equals(_instance._basePath, customBasePath, StringComparison.OrdinalIgnoreCase))
            {
                _instance._basePath = customBasePath;
                _instance.EnsureDirectoryAndSeedData();
            }

            return _instance;
        }
    }

    public static CsvDbContext getInstance() => GetInstance();

    public static CsvDbContext CreateIsolatedContextForTesting(string testDirectoryPath, bool seedDefaultData = false)
    {
        Directory.CreateDirectory(testDirectoryPath);
        var ctx = new CsvDbContext(testDirectoryPath, seedDefaultData);
        return ctx;
    }

    private CsvDbContext(string basePath, bool seedDefaultData)
    {
        _basePath = basePath;
        Directory.CreateDirectory(_basePath);
        if (seedDefaultData)
        {
            EnsureDirectoryAndSeedData();
        }
    }

    public string GetFilePath(string entity)
    {
        if (string.IsNullOrWhiteSpace(entity))
        {
            throw new ArgumentException("Entity name cannot be empty.", nameof(entity));
        }

        string normalized = entity.Trim().ToLowerInvariant();
        if (!normalized.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            normalized += ".csv";
        }

        return Path.Combine(_basePath, normalized);
    }

    public string getFilePath(string entity) => GetFilePath(entity);

    private void EnsureDirectoryAndSeedData()
    {
        Directory.CreateDirectory(_basePath);

        string usersFile = GetFilePath("users");
        string coursesFile = GetFilePath("courses");
        string studentsFile = GetFilePath("students");
        string enrollmentsFile = GetFilePath("enrollments");

        if (!File.Exists(usersFile))
        {
            SeedUsersCsv(usersFile);
        }

        if (!File.Exists(coursesFile))
        {
            SeedCoursesCsv(coursesFile);
        }

        if (!File.Exists(studentsFile))
        {
            SeedLargeStudentsCsv(studentsFile, 1250);
        }

        if (!File.Exists(enrollmentsFile))
        {
            SeedEnrollmentsCsv(enrollmentsFile);
        }
    }

    private static void SeedUsersCsv(string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,Name,Email,Role,Metadata");
        sb.AppendLine("ADM001,Dr. Nguyen Quan Tri (System Admin),admin@btec.edu.vn,ADMIN,Academic Registry Office");
        sb.AppendLine("FAC001,Dinh Van Dong (Unit 20 Assessor),dong.dv@btec.edu.vn,FACULTY,Applied Programming and Design Principles");
        sb.AppendLine("FAC002,Dr. Le Thi Huong (Senior Faculty),huong.lt@btec.edu.vn,FACULTY,Data Structures & Algorithms");
        sb.AppendLine("GST001,Prof. Robert C. Martin (Guest Lecturer),unclebob@pearson.uk,GUEST_LECTURER,Pearson UK Visiting Scholar");
        sb.AppendLine("BH02191,Tran Quang Khai,khai.tq.bh02191@btec.edu.vn,STUDENT,Software Engineering");
        sb.AppendLine("BH02141,Luong The Long,long.lt.bh02141@btec.edu.vn,STUDENT,Software Engineering");
        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    private static void SeedCoursesCsv(string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("CourseId,CourseName,Credits,Department,LecturerName,AcademicProgram,MaxCapacity");
        sb.AppendLine("UNIT20-APDP,Unit 20: Applied Programming and Design Principles,15,Computing,Dinh Van Dong,BTEC HND in Computing,60");
        sb.AppendLine("UNIT19-DSA,Unit 19: Data Structures and Algorithms,15,Computing,Dr. Le Thi Huong,BTEC HND in Computing,60");
        sb.AppendLine("UNIT01-PROG,Unit 1: Programming,15,Computing,Dinh Van Dong,BTEC HND in Computing,60");
        sb.AppendLine("UNIT04-DB,Unit 4: Database Design & Development,15,Computing,Dr. Le Thi Huong,BTEC HND in Computing,60");
        sb.AppendLine("UNIT09-SDLC,Unit 9: Software Development Lifecycles,15,Software Engineering,Dinh Van Dong,BTEC HND in Computing,60");
        sb.AppendLine("UNIT13-WEB,Unit 13: Website Design & Development,15,Computing,Dr. Le Thi Huong,BTEC HND in Computing,60");
        sb.AppendLine("UNIT34-ARCH,Unit 34: Systems Architecture,15,Software Engineering,Dinh Van Dong,BTEC HND in Computing,50");
        sb.AppendLine("UNIT47-AI,Unit 47: Emerging Technologies & AI,15,Artificial Intelligence,Dr. Le Thi Huong,BTEC HND in Computing,45");
        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    private static void SeedLargeStudentsCsv(string filePath, int totalRecords)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,Name,Email,Major,AcademicProgram,GPA,Cohort,StudentType,ResearchTopic");

        // Primary authors from the Assignment Report
        sb.AppendLine("BH02191,Tran Quang Khai,khai.tq.bh02191@btec.edu.vn,Software Engineering,BTEC HND in Computing,3.92,SE08202,Undergraduate,");
        sb.AppendLine("BH02141,Luong The Long,long.lt.bh02141@btec.edu.vn,Software Engineering,BTEC HND in Computing,3.88,SE08202,Undergraduate,");
        sb.AppendLine("PG00101,Nguyen Minh Anh,anh.nm.pg00101@btec.edu.vn,Software Engineering,MSc Computing & Software Engineering,3.95,PG-2025,Postgraduate,High-Throughput CSV Stream Processing in .NET");
        sb.AppendLine("PG00102,Pham Hoang Nam,nam.ph.pg00102@btec.edu.vn,Data Science,MSc Computing & Software Engineering,3.82,PG-2025,Postgraduate,Clean Architecture & SOLID Metrics");

        string[] lastNames = { "Nguyen", "Tran", "Le", "Pham", "Hoang", "Huynh", "Phan", "Vu", "Vo", "Dang", "Bui", "Do", "Ho", "Ngo", "Duong", "Ly" };
        string[] middleNames = { "Van", "Thi", "Quang", "Minh", "The", "Hoang", "Duc", "Ngoc", "Tuan", "Gia", "Bao", "Thanh", "Khanh", "Phuong" };
        string[] firstNames = { "An", "Binh", "Cuong", "Dung", "Giang", "Hai", "Hieu", "Hung", "Huy", "Khoa", "Linh", "Mai", "Nam", "Phong", "Phuc", "Quan", "Son", "Thao", "Trang", "Tuan", "Vy", "Yen" };
        string[] majors = { "Software Engineering", "Computer Science", "Data Science", "Cyber Security", "Artificial Intelligence", "Information Systems" };
        string[] cohorts = { "SE08201", "SE08202", "SE08203", "CS08101", "DS08102", "AI08201" };

        var rng = new Random(202520);
        for (int i = 1; i <= totalRecords - 4; i++)
        {
            string id = $"BH{10000 + i:D5}";
            string last = lastNames[i % lastNames.Length];
            string mid = middleNames[(i * 3) % middleNames.Length];
            string first = firstNames[(i * 7) % firstNames.Length];
            string fullName = $"{last} {mid} {first}";
            string email = $"{first.ToLowerInvariant()}.{last.ToLowerInvariant()}.{id.ToLowerInvariant()}@btec.edu.vn";
            string major = majors[i % majors.Length];
            string cohort = cohorts[i % cohorts.Length];
            double gpa = Math.Round(2.10 + (rng.NextDouble() * 1.90), 2);
            bool isPostgrad = i % 15 == 0;
            string program = isPostgrad ? "MSc Computing & Software Engineering" : "BTEC HND in Computing";
            string studentType = isPostgrad ? "Postgraduate" : "Undergraduate";
            string researchTopic = isPostgrad ? "Scalable Enterprise Architecture" : "";

            sb.AppendLine($"{id},{fullName},{email},{major},{program},{gpa.ToString(System.Globalization.CultureInfo.InvariantCulture)},{cohort},{studentType},{researchTopic}");
        }

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    private static void SeedEnrollmentsCsv(string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("EnrollmentId,StudentId,CourseId,EnrollmentDate,Grade,Status");
        sb.AppendLine("ENR-20250501-0001,BH02191,UNIT20-APDP,2025-05-01,3.95,Completed (Distinction)");
        sb.AppendLine("ENR-20250501-0002,BH02191,UNIT19-DSA,2025-05-01,3.90,Completed (Distinction)");
        sb.AppendLine("ENR-20250501-0003,BH02191,UNIT34-ARCH,2025-05-02,,Enrolled");
        sb.AppendLine("ENR-20250501-0004,BH02141,UNIT20-APDP,2025-05-01,3.90,Completed (Distinction)");
        sb.AppendLine("ENR-20250501-0005,BH02141,UNIT19-DSA,2025-05-01,3.85,Completed (Distinction)");
        sb.AppendLine("ENR-20250501-0006,BH02141,UNIT13-WEB,2025-05-02,,Enrolled");
        sb.AppendLine("ENR-20250501-0007,PG00101,UNIT20-APDP,2025-05-01,4.00,Completed (Distinction)");
        sb.AppendLine("ENR-20250501-0008,BH10001,UNIT20-APDP,2025-05-03,3.40,Completed (Merit)");
        sb.AppendLine("ENR-20250501-0009,BH10002,UNIT20-APDP,2025-05-03,3.10,Completed (Pass)");
        sb.AppendLine("ENR-20250501-0010,BH10003,UNIT19-DSA,2025-05-03,,Enrolled");
        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }
}
