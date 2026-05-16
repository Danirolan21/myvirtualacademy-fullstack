using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyVirtualAcademy.Data;

namespace MyVirtualAcademy.API.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Una BD InMemory única por instancia de la factory (= por clase de test que use IClassFixture).
    private readonly string _dbName = "TestDb_" + Guid.NewGuid().ToString("N");

    public CustomWebApplicationFactory()
    {
        // Inyectamos config vía variables de entorno porque Program.cs lee
        // builder.Configuration["JwtSettings:SecretKey"] EAGERLY durante el setup,
        // antes de que ConfigureAppConfiguration de la factory aplique.
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("JwtSettings__SecretKey", "TestSecretKey_MyVirtualAcademy_2026_xUnit");
        Environment.SetEnvironmentVariable("JwtSettings__Issuer", "MyVirtualAcademyTest");
        Environment.SetEnvironmentVariable("JwtSettings__Audience", "MyVirtualAcademyTestUsers");
        Environment.SetEnvironmentVariable("JwtSettings__AccessTokenExpiryMinutes", "15");
        Environment.SetEnvironmentVariable("JwtSettings__RefreshTokenExpiryDays", "7");
        Environment.SetEnvironmentVariable("ConnectionStrings__SqlMyVirtualAcademy", "InMemory");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Quitar el registro existente del DbContext (apunta a SQL Server)
            var toRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<MyVirtualAcademyContext>)
                         || d.ServiceType == typeof(DbContextOptions)
                         || d.ServiceType == typeof(MyVirtualAcademyContext))
                .ToList();
            foreach (var d in toRemove) services.Remove(d);

            // Provider EF Core aislado SOLO para InMemory. Esto evita que el provider
            // de SqlServer (registrado por la app vía UseSqlServer) y el de InMemory
            // coexistan en el mismo internal service provider.
            var inMemoryProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.AddDbContext<MyVirtualAcademyContext>(options =>
                options.UseInMemoryDatabase(_dbName)
                       .UseInternalServiceProvider(inMemoryProvider));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using (var scope = host.Services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<MyVirtualAcademyContext>();
            TestDataSeeder.Seed(ctx);
        }
        return host;
    }

    /// <summary>
    /// Crea un HttpClient autenticado vía login real contra /api/auth/login.
    /// </summary>
    public async Task<HttpClient> GetAuthenticatedClientAsync(string email, string password)
    {
        var client = CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        res.EnsureSuccessStatusCode();

        var body = await res.Content.ReadFromJsonAsync<LoginResponse>();
        if (body is null || string.IsNullOrEmpty(body.AccessToken))
            throw new InvalidOperationException("El login de seed no devolvió accessToken.");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.AccessToken);
        return client;
    }

    private sealed record LoginResponse(string AccessToken);
}
