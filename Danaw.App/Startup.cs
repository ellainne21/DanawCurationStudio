using System;
using System.IO;
using Danaw.Core.Abstractions;
using Danaw.Core.Services;
using Danaw.Data;
using Danaw.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Danaw.App;

public static class Startup
{
    // The app's real database lives in the user's local app-data folder,
    // not inside the project, so a published copy works on any machine.
    public static string DatabasePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DanawCurationStudio", "danaw.db");

    public static IServiceProvider BuildServices()
    {
        var services = new ServiceCollection();

        services.AddDbContext<DanawDbContext>(o => o.UseSqlite($"Data Source={DatabasePath}"));

        services.AddScoped<IContentItemRepository, ContentItemRepository>();
        services.AddScoped<IReviewerRepository, ReviewerRepository>();
        services.AddScoped<IValidationRecordRepository, ValidationRecordRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IContentCurationService, ContentCurationService>();
        services.AddScoped<IValidationWorkflowService, ValidationWorkflowService>();

        return services.BuildServiceProvider();
    }
}