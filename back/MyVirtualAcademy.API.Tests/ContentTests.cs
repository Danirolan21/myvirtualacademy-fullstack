using System.Net;
using FluentAssertions;

namespace MyVirtualAcademy.API.Tests;

public class ContentTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ContentTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Delete_WithNonExistentId_Returns404()
    {
        var client = await _factory.GetAuthenticatedClientAsync(
            TestDataSeeder.ProfesorEmail, TestDataSeeder.TestPassword);

        var response = await client.DeleteAsync("/api/content/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
