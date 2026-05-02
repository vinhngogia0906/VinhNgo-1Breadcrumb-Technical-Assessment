using LibraryApi.Domain.Repositories;
using LibraryApi.Infrastructure.Data;
using LibraryApi.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing 'Default' connection string.");

        services.AddDbContext<LibraryDbContext>(options =>
            options.UseNpgsql(connectionString, npg => npg.MigrationsAssembly(typeof(LibraryDbContext).Assembly.FullName)));

        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IBookActivityRepository, BookActivityRepository>();
        services.AddScoped<IDataSeeder, DataSeeder>();

        return services;
    }
}
