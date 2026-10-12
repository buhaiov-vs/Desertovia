using Desertod.Infrastructure.Accounts;
using Desertod.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Desertod.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContextFactory<DesertodDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<AccountService>();
        return services;
    }
}
