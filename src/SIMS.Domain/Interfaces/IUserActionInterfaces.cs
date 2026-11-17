namespace SIMS.Domain.Interfaces;

public interface ICourseRegisterable
{
    bool CanRegisterForCourse(string courseId);
    int GetMaxAllowedCredits();
}

public interface ICourseManageable
{
    bool CanManageCourses();
    string GetAdministrativeScope();
}

public interface IGradeSubmittable
{
    bool CanSubmitGrades(string courseId);
}
