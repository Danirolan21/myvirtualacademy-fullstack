using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace MyVirtualAcademy.API.Tests;

public class AuthTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200AndToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = TestDataSeeder.ProfesorEmail,
            password = TestDataSeeder.TestPassword
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("accessToken", out var token).Should().BeTrue();
        token.GetString().Should().NotBeNullOrEmpty();

        body.TryGetProperty("user", out var user).Should().BeTrue();
        user.GetProperty("role").GetString().Should().Be("Profesor");
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = TestDataSeeder.ProfesorEmail,
            password = "PasswordIncorrecta"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_WithValidData_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            nombre = "Nuevo",
            apellidos = "Usuario",
            email = $"nuevo.{Guid.NewGuid():N}@example.com",
            password = "Test1234"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("message", out var msg).Should().BeTrue();
        msg.GetString().Should().Be("Cuenta creada correctamente");
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            nombre = "Profesor",
            apellidos = "Duplicado",
            email = TestDataSeeder.ProfesorEmail,
            password = "Test1234"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
