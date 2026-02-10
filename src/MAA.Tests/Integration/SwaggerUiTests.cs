using FluentAssertions;
using System.Net.Http;
using Xunit;

namespace MAA.Tests.Integration;

/// <summary>
/// Integration tests for Swagger UI behavior.
/// </summary>
[Collection("Database collection")]
[Trait("Category", "Integration")]
public class SwaggerUiTests : IAsyncLifetime
{
    private readonly DatabaseFixture _databaseFixture;
    private TestWebApplicationFactory? _factory;
    private HttpClient? _client;

    public SwaggerUiTests(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    public Task InitializeAsync()
    {
        _factory = TestWebApplicationFactory.CreateWithDatabase(_databaseFixture);
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory != null)
        {
            await _factory.DisposeAsync();
        }
    }

    [Fact]
    public async Task SwaggerUi_Includes_TryItOut_And_Execute()
    {
        var response = await _client!.GetAsync("/swagger");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();

        html.Should().Contain("Try it out");
        html.Should().Contain("Execute");
    }
}
