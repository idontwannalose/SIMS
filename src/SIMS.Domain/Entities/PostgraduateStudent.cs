namespace SIMS.Domain.Entities;

public class PostgraduateStudent : Student
{
    public string ResearchTopic { get; set; } = "Distributed Data Systems";
    public string SupervisorId { get; set; } = "FAC001";

    public PostgraduateStudent() : base()
    {
        AcademicProgram = "MSc Computing & Software Engineering";
    }

    public PostgraduateStudent(
        string id,
        string name,
        string email,
        string major = "Software Engineering",
        string researchTopic = "Distributed Data Systems",
        double gpa = 3.5)
        : base(id, name, email, major, "MSc Computing & Software Engineering", gpa, "PG-2025")
    {
        ResearchTopic = researchTopic;
    }

    public override int GetMaxAllowedCredits() => 30;

    public override string GetDisplayInfo()
    {
        return $"[POSTGRADUATE] {Id} - {Name} ({Email}) | Major: {Major} | Research: {ResearchTopic} | GPA: {GPA:F2}";
    }
}
