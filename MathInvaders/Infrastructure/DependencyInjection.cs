using FluentMigrator.Runner;
using MathInvaders.Infrastructure.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Reflection;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            // Регистрация NpgsqlDataSource
            services.AddSingleton(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var connectionString = configuration.GetConnectionString("PostgresDB");
                return new NpgsqlDataSourceBuilder(connectionString).Build();
            });

            // Регистрация подключения к базе
            services.AddScoped(sp =>
            {
                var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
                return dataSource.CreateConnection();
            });

            // Настройка FluentMigrator
            services.AddFluentMigratorCore()
                .ConfigureRunner(rb => rb
                    .AddPostgres()
                    .WithGlobalConnectionString(sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("PostgresDB"))
                    .ScanIn(Assembly.GetExecutingAssembly()).For.Migrations())
                .AddLogging(lb => lb.AddFluentMigratorConsole());

            // Регистрация MigrationRunner
            services.AddScoped<MathInvaders.Infrastructure.Database.MigrationRunner>();

            return services;
        }
    }
}