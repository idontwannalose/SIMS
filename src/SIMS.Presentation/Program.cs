using SIMS.Application.Interfaces;
using SIMS.Application.Services;
using SIMS.Application.Validators;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;
using SIMS.Infrastructure.Adapters;
using SIMS.Infrastructure.CsvRepositories;
using SIMS.Infrastructure.FileContext;
using SIMS.Presentation.Controllers;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

string csvDataPath = Path.Combine(builder.Environment.ContentRootPath, "CsvData");
builder.Services.AddSingleton(_ => CsvDbContext.GetInstance(csvDataPath));

builder.Services.AddSingleton<CsvDataFormatter>();

builder.Services.AddSingleton<StudentValidator>();

builder.Services.AddScoped<ICsvRepository<Student>, StudentCsvRepository>();
builder.Services.AddScoped<ICsvRepository<Course>, CourseCsvRepository>();
builder.Services.AddScoped<ICsvRepository<Enrollment>, EnrollmentCsvRepository>();
builder.Services.AddScoped<ICsvRepository<User>, UserCsvRepository>();

// Application Layer Services
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IAuthService, AuthenticationService>();
builder.Services.AddScoped<CurrentUserContext>();

var app = builder.Build();

_ = app.Services.GetRequiredService<CsvDbContext>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
