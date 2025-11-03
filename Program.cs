using Labaratory.DbContext;
using Labaratory.Helpers;
using Labaratory.Models;
using Labaratory.Services;
using Labaratory.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    Log.Information("Запуск приложения");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    builder.Services.AddControllersWithViews();

    builder.Services.AddDbContext<ApplicationContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!));

    builder.Services.AddIdentity<User, IdentityRole>()
        .AddEntityFrameworkStores<ApplicationContext>();

    builder.Services.AddScoped<IRequestDbService, RequestDbService>();

    var app = builder.Build();

    // Middleware
    //app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    // Глобальный обработчик ошибок
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Account}/{action=SignIn}/{id?}");

    LicenseHelper.CheckLicense();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Приложение аварийно завершено");
}
finally
{
    Log.CloseAndFlush();
}
