using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace MyVirtualAcademy.API.Tests;

public class UsersTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public UsersTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users", new
        {
            nombre = "Nuevo", apellidos = "Sin Token",
            email = $"sintoken.{Guid.NewGuid():N}@example.com",
            password = "Test1234", idRol = TestDataSeeder.RolProfesorId
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_AsStudent_Returns403()
    {
        var client = await _factory.GetAuthenticatedClientAsync(
            TestDataSeeder.EstudianteEmail, TestDataSeeder.TestPassword);

        var response = await client.PostAsJsonAsync("/api/users", new
        {
            nombre = "Nuevo", apellidos = "Por Estudiante",
            email = $"porestudiante.{Guid.NewGuid():N}@example.com",
            password = "Test1234", idRol = TestDataSeeder.RolProfesorId
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_AsAdminWithValidRol_Returns201AndUserCanLoginWithThatRole()
    {
        var adminClient = await _factory.GetAuthenticatedClientAsync(
            TestDataSeeder.AdminEmail, TestDataSeeder.TestPassword);

        var nuevoEmail = $"prof.nuevo.{Guid.NewGuid():N}@example.com";
        var nuevoPassword = "Test1234";

        var createResponse = await adminClient.PostAsJsonAsync("/api/users", new
        {
            nombre = "Profesor",
            apellidos = "Recién Creado",
            email = nuevoEmail,
            password = nuevoPassword,
            idRol = TestDataSeeder.RolProfesorId
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        createBody.TryGetProperty("idUsuario", out var idProp).Should().BeTrue();
        idProp.GetInt32().Should().BeGreaterThan(0);

        // Verificación end-to-end: el usuario recién creado puede hacer login
        // (eso demuestra activo=true) y el rol devuelto coincide con el solicitado.
        var loginClient = _factory.CreateClient();
        var loginResponse = await loginClient.PostAsJsonAsync("/api/auth/login", new
        {
            email = nuevoEmail,
            password = nuevoPassword
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        loginBody.GetProperty("user").GetProperty("role").GetString()
            .Should().Be("Profesor");
    }

    [Fact]
    public async Task Create_AsAdminWithDuplicateEmail_Returns409()
    {
        var adminClient = await _factory.GetAuthenticatedClientAsync(
            TestDataSeeder.AdminEmail, TestDataSeeder.TestPassword);

        var response = await adminClient.PostAsJsonAsync("/api/users", new
        {
            nombre = "Duplicado",
            apellidos = "De Test",
            email = TestDataSeeder.ProfesorEmail, // ya seedeado
            password = "Test1234",
            idRol = TestDataSeeder.RolProfesorId
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
