using FluentAssertions;
using System.Net.Http;
using Xunit;

namespace MAA.Tests.Integration;

/// <summary>
/// Integration tests for the OpenAPI YAML endpoint.
/// </summary>
[Collection("Database collection")]
[Trait("Category", "Integration")]
public class OpenApiYamlEndpointTests : IAsyncLifetime
{
    private readonly DatabaseFixture _databaseFixture;
    private TestWebApplicationFactory? _factory;
    private HttpClient? _client;

    public OpenApiYamlEndpointTests(DatabaseFixture databaseFixture)
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
    public async Task OpenApiYaml_Endpoint_Returns_Schema()
    {
        var response = await _client!.GetAsync("/openapi/v1.yaml");
        response.EnsureSuccessStatusCode();

        var yaml = await response.Content.ReadAsStringAsync();
        yaml.Should().Contain("openapi:");
        yaml.Should().Contain("3.0.1");
    }
}
