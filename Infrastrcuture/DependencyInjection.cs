using Application.Common.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers all Infrastructure layer services: EF Core DbContext (SQLite).
    /// Switch to SQL Server later by replacing UseSqlite with UseSqlServer here.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=ShiftManagement.db";

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));

        // Register the DbContext as its interface for the Application layer
        services.AddScoped<IApplicationDbContext>(
            provider => provider.GetRequiredService<ApplicationDbContext>());

        // Register custom dynamic authorization services
        services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider, Infrastructure.Auth.DatabasePolicyProvider>();
        services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, Infrastructure.Auth.DatabasePolicyAuthorizationHandler>();

        return services;
    }
}
